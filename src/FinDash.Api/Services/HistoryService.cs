using FinDash.Core.Interfaces;
using FinDash.Core.Models;
using Microsoft.Extensions.Caching.Memory;

namespace FinDash.Api.Services;

/// <summary>Un punto de la gráfica con la forma de PricePoint de la app.</summary>
public sealed record PricePointDto(DateTimeOffset Time, decimal PriceCrc, decimal PriceUsd);

/// <summary>
/// Gráfica histórica (RF-09). PriceSnapshots solo tiene datos desde que arrancó el servidor,
/// así que el historial se pide a la fuente cuando alguien abre la gráfica y se guarda en
/// memoria un rato (10 min para 24h, 1 h para los demás) para no gastar llamadas.
/// </summary>
public sealed class HistoryService(
    IMarketDataProvider market,
    IExchangeRateProvider fx,
    QuoteStore store,
    IMemoryCache cache,
    TimeProvider time)
{
    static readonly TimeSpan CrOffset = TimeSpan.FromHours(-6);

    /// <summary>Null si todavía no hay tipo de cambio para pasar a colones.</summary>
    public async Task<IReadOnlyList<PricePointDto>?> GetAsync(string assetId, ChartRange range, CancellationToken ct)
    {
        var key = $"history:{assetId}:{range}";
        if (cache.TryGetValue(key, out IReadOnlyList<PricePointDto>? cached)) return cached;

        var points = assetId == Asset.UsdId
            ? await FxHistoryAsync(range, ct)
            : await CryptoHistoryAsync(assetId, range, ct);
        if (points is null) return null;

        cache.Set(key, points, range == ChartRange.Day ? TimeSpan.FromMinutes(10) : TimeSpan.FromHours(1));
        return points;
    }

    async Task<IReadOnlyList<PricePointDto>?> CryptoHistoryAsync(string assetId, ChartRange range, CancellationToken ct)
    {
        // Simplificación: toda la serie se pasa a colones con el tipo de compra de hoy.
        var fxBuy = store.Current.Fx?.Buy;
        if (fxBuy is null) return null;

        var history = await market.GetHistoryAsync(assetId, range, ct);
        return history.Select(p => new PricePointDto(p.Time, p.PriceUsd * fxBuy.Value, p.PriceUsd)).ToList();
    }

    async Task<IReadOnlyList<PricePointDto>> FxHistoryAsync(ChartRange range, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().ToOffset(CrOffset).DateTime);
        var days = range switch { ChartRange.Day or ChartRange.Week => 7, ChartRange.Month => 30, _ => 365 };
        var rates = await fx.GetHistoryAsync(today.AddDays(-days), today, ct);
        return rates.Select(r => new PricePointDto(r.At, r.Buy, 1m)).ToList();
    }
}
