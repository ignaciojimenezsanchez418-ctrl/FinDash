using FinDash.Api.Data;
using FinDash.Api.Integrations;
using FinDash.Core.Interfaces;
using FinDash.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FinDash.Api.Services;

/// <summary>
/// Trae precios y tipo de cambio, los guarda en PriceSnapshots y actualiza la caché. Es lo
/// ÚNICO que llama a CoinGecko y al BCCR por precios actuales (sección 9 del documento).
/// Si una fuente falla, se registra en los logs y se conserva el último dato (contrato §3).
/// </summary>
public sealed class PriceRefresher(
    FinDashDbContext db,
    QuoteStore store,
    IMarketDataProvider market,
    IExchangeRateProvider fx,
    TimeProvider time,
    ILogger<PriceRefresher> log)
{
    static readonly TimeSpan CrOffset = TimeSpan.FromHours(-6);

    /// <summary>Al arrancar: carga la última foto guardada para no empezar sin datos.</summary>
    public async Task LoadFromDatabaseAsync(CancellationToken ct)
    {
        var latest = await db.PriceSnapshots
            .GroupBy(s => s.AssetId)
            .Select(g => g.OrderByDescending(s => s.CapturedAt).First())
            .ToListAsync(ct);
        if (latest.Count == 0) return;

        var usd = latest.FirstOrDefault(s => s.AssetId == Asset.UsdId);
        var crypto = latest.Where(s => s.AssetId != Asset.UsdId)
            .ToDictionary(s => s.AssetId, s => new MarketQuote(s.AssetId, s.PriceUsd, s.Change24hPct, s.CapturedAt));

        store.Update(_ => new MarketState(
            usd is null ? null : new CurrentFx(usd.PriceCrc, usd.SellCrc ?? usd.PriceCrc, usd.Change24hPct, usd.CapturedAt),
            crypto,
            latest.Max(s => s.CapturedAt),
            LastRefreshFailed: false));
        log.LogInformation("Precios cargados de la base: {Count} activos", latest.Count);
    }

    /// <summary>Tipo de cambio de la última semana; la variación es contra el día anterior publicado.</summary>
    public async Task<bool> RefreshFxAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().ToOffset(CrOffset).DateTime);
        try
        {
            var rates = await fx.GetHistoryAsync(today.AddDays(-7), today, ct);
            if (rates.Count == 0) throw new HttpRequestException("La fuente del tipo de cambio no devolvió datos.");

            var last = rates[^1];
            var previous = rates.Count > 1 ? rates[^2] : last;
            var change = previous.Buy == 0 ? 0 : Math.Round((last.Buy - previous.Buy) / previous.Buy * 100, 4);
            var now = time.GetUtcNow();

            db.PriceSnapshots.Add(new PriceSnapshot
            {
                AssetId = Asset.UsdId,
                PriceUsd = 1m,
                PriceCrc = last.Buy,
                SellCrc = last.Sell,
                Change24hPct = change,
                CapturedAt = now,
            });
            await db.SaveChangesAsync(ct);

            store.Update(s => s with { Fx = new CurrentFx(last.Buy, last.Sell, change, last.At), UpdatedAt = now, LastRefreshFailed = false });
            return true;
        }
        catch (HttpRequestException ex)
        {
            log.LogWarning("No se pudo actualizar el tipo de cambio: {Message}. Se usa el último guardado.", ex.Message);
            store.Update(s => s with { LastRefreshFailed = true });
            return false;
        }
    }

    /// <summary>Un solo pedido con todas las cripto del catálogo (RNF-02).</summary>
    public async Task<bool> RefreshCryptoAsync(CancellationToken ct)
    {
        var ids = await db.Assets.Where(a => a.Kind == AssetKind.Crypto).Select(a => a.Id).ToListAsync(ct);
        try
        {
            var quotes = await market.GetQuotesAsync(ids, ct);
            var now = time.GetUtcNow();
            var fxBuy = store.Current.Fx?.Buy ?? 0;

            db.PriceSnapshots.AddRange(quotes.Select(q => new PriceSnapshot
            {
                AssetId = q.AssetId,
                PriceUsd = q.PriceUsd,
                PriceCrc = q.PriceUsd * fxBuy,
                Change24hPct = q.Change24hPct,
                CapturedAt = now,
            }));
            await db.SaveChangesAsync(ct);

            store.Update(s =>
            {
                // Se conservan los activos que esta vez no vinieron.
                var merged = new Dictionary<string, MarketQuote>(s.Crypto);
                foreach (var q in quotes) merged[q.AssetId] = q;
                return s with { Crypto = merged, UpdatedAt = now, LastRefreshFailed = false };
            });
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or RateLimitedException
                                   || ex is TaskCanceledException && !ct.IsCancellationRequested)
        {
            log.LogWarning("No se pudieron actualizar los precios cripto: {Message}. Se sirve la caché.", ex.Message);
            store.Update(s => s with { LastRefreshFailed = true });
            return false;
        }
    }
}
