using FinDash.Core.Models;

namespace FinDash.Api.Services;

/// <summary>Tipo de cambio vigente y su variación contra el día hábil anterior.</summary>
public sealed record CurrentFx(decimal Buy, decimal Sell, decimal Change24hPct, DateTimeOffset At);

/// <summary>Precio de un activo listo para la app: en USD y en colones (con el tipo de compra).</summary>
public sealed record CurrentQuote(string AssetId, decimal PriceUsd, decimal PriceCrc, decimal Change24hPct);

/// <summary>Foto de los precios en memoria. Inmutable: se reemplaza entera en cada refresco.</summary>
public sealed record MarketState(
    CurrentFx? Fx,
    IReadOnlyDictionary<string, MarketQuote> Crypto,
    DateTimeOffset? UpdatedAt,
    bool LastRefreshFailed)
{
    public static readonly MarketState Empty = new(null, new Dictionary<string, MarketQuote>(), null, false);

    /// <summary>Precio del activo en USD y CRC, o null si todavía no hay datos.</summary>
    public CurrentQuote? Quote(string assetId)
    {
        if (Fx is null) return null;
        if (assetId == Asset.UsdId) return new CurrentQuote(assetId, 1m, Fx.Buy, Fx.Change24hPct);
        return Crypto.TryGetValue(assetId, out var q)
            ? new CurrentQuote(assetId, q.PriceUsd, q.PriceUsd * Fx.Buy, q.Change24hPct)
            : null;
    }

    public IEnumerable<CurrentQuote> AllQuotes() =>
        Fx is null ? [] : new[] { Asset.UsdId }.Concat(Crypto.Keys).Select(id => Quote(id)!);

    /// <summary>Precios en colones para PortfolioCalculator.</summary>
    public Dictionary<string, AssetPrice> PricesCrc() =>
        AllQuotes().ToDictionary(q => q.AssetId, q => new AssetPrice(q.PriceCrc, q.Change24hPct));
}

/// <summary>
/// Caché de precios compartida por todos los usuarios (sección 4 del documento). Solo
/// PriceRefresher la escribe; los controllers y AlertWorker la leen.
/// </summary>
public sealed class QuoteStore
{
    MarketState _state = MarketState.Empty;

    public MarketState Current => Volatile.Read(ref _state);

    public void Update(Func<MarketState, MarketState> change)
    {
        MarketState before, after;
        do
        {
            before = Current;
            after = change(before);
        }
        while (Interlocked.CompareExchange(ref _state, after, before) != before);
    }
}
