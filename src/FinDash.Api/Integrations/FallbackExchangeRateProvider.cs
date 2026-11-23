using FinDash.Core.Interfaces;
using FinDash.Core.Models;

namespace FinDash.Api.Integrations;

/// <summary>
/// Tipo de cambio con respaldo (riesgo de la sección 14): pide primero a la fuente oficial (BCCR)
/// y, si no responde, a la de respaldo (CoinGecko). Si las dos fallan, el error sube y
/// PriceRefresher conserva el último valor guardado.
/// </summary>
public sealed class FallbackExchangeRateProvider(
    IExchangeRateProvider primary,
    IExchangeRateProvider backup,
    ILogger<FallbackExchangeRateProvider> log) : IExchangeRateProvider
{
    public Task<FxQuote> GetLatestAsync(CancellationToken ct = default) =>
        WithBackupAsync(p => p.GetLatestAsync(ct), ct);

    public Task<IReadOnlyList<FxQuote>> GetHistoryAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        WithBackupAsync(p => p.GetHistoryAsync(from, to, ct), ct);

    async Task<T> WithBackupAsync<T>(Func<IExchangeRateProvider, Task<T>> call, CancellationToken ct)
    {
        try
        {
            return await call(primary);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            log.LogWarning("La fuente oficial del tipo de cambio no respondió ({Message}); se usa la de respaldo.", ex.Message);
            return await call(backup);
        }
    }
}
