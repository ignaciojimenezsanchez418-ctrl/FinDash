namespace FinDash.Core.Models;

public sealed record Asset(string Id, string Symbol, string Name, AssetKind Kind)
{
    public const string UsdId = "usd";

    public bool IsFx => Kind == AssetKind.Fiat;
}

public sealed record MarketQuote(string AssetId, decimal PriceUsd, decimal Change24hPct, DateTimeOffset At);

public sealed record FxQuote(decimal Buy, decimal Sell, DateTimeOffset At);

public sealed record HistoryPoint(DateTimeOffset Time, decimal PriceUsd);
