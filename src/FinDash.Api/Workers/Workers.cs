using FinDash.Api.Services;
using Microsoft.Extensions.Options;

namespace FinDash.Api.Workers;

/// <summary>Workers:Enabled = false apaga los dos servicios (las pruebas los llaman a mano).</summary>
public sealed class WorkerOptions
{
    public bool Enabled { get; set; } = true;
}

public sealed class PriceOptions
{
    /// <summary>
    /// Cada cuánto se piden precios cripto. 5 min: el plan Demo de CoinGecko tiene además un tope
    /// mensual de llamadas que cada 30–60 s se pasaría.
    /// </summary>
    public int CryptoRefreshMinutes { get; set; } = 5;

    public int FxRefreshHours { get; set; } = 1;
}

/// <summary>
/// Mantiene la caché de precios al día para todos los usuarios, sin importar cuántos haya
/// (D3, RNF-02).
/// </summary>
public sealed class PriceService(
    IServiceScopeFactory scopes,
    IOptions<PriceOptions> options,
    IOptions<WorkerOptions> workers,
    TimeProvider time,
    ILogger<PriceService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!workers.Value.Enabled) return;

        await using (var scope = scopes.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<PriceRefresher>().LoadFromDatabaseAsync(ct);

        DateTimeOffset nextCrypto = DateTimeOffset.MinValue, nextFx = DateTimeOffset.MinValue;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30), time);

        do
        {
            try
            {
                var now = time.GetUtcNow();
                await using var scope = scopes.CreateAsyncScope();
                var refresher = scope.ServiceProvider.GetRequiredService<PriceRefresher>();

                if (now >= nextFx)
                {
                    var ok = await refresher.RefreshFxAsync(ct);
                    // Si falló, se reintenta en 10 min en vez de esperar horas.
                    nextFx = now + (ok ? TimeSpan.FromHours(options.Value.FxRefreshHours) : TimeSpan.FromMinutes(10));
                }
                if (now >= nextCrypto)
                {
                    var ok = await refresher.RefreshCryptoAsync(ct);
                    nextCrypto = now + (ok ? TimeSpan.FromMinutes(options.Value.CryptoRefreshMinutes) : TimeSpan.FromMinutes(1));
                }
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                log.LogError(ex, "Error inesperado al refrescar precios");
            }
        }
        while (await timer.WaitForNextTickAsync(ct));
    }
}

/// <summary>Evalúa las alertas cada minuto (US-403).</summary>
public sealed class AlertWorker(
    IServiceScopeFactory scopes,
    IOptions<AlertOptions> options,
    IOptions<WorkerOptions> workers,
    TimeProvider time,
    ILogger<AlertWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!workers.Value.Enabled) return;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, options.Value.EvaluateEverySeconds)), time);
        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var fired = await scope.ServiceProvider.GetRequiredService<AlertEvaluator>().EvaluateAsync(ct);
                if (fired > 0) log.LogInformation("Se dispararon {Count} alertas", fired);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                log.LogError(ex, "Error al evaluar alertas");
            }
        }
    }
}
