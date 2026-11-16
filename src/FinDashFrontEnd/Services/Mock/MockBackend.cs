using FinDashFrontEnd.Models;

namespace FinDashFrontEnd.Services.Mock;

/// <summary>
/// Estado en memoria que imita al backend mientras la API no está lista.
/// Todos los servicios Mock* comparten esta instancia (singleton).
/// </summary>
public sealed class MockBackend(ISettingsService settings)
{
    readonly Random _rng = new(17);

    public static readonly Asset Usd = new(Asset.UsdId, "USD", "Dólar", AssetKind.Fiat);

    public IReadOnlyList<Asset> Assets { get; } =
    [
        Usd,
        new("bitcoin", "BTC", "Bitcoin", AssetKind.Crypto),
        new("ethereum", "ETH", "Ethereum", AssetKind.Crypto),
        new("solana", "SOL", "Solana", AssetKind.Crypto),
        new("tether", "USDT", "Tether", AssetKind.Crypto),
        new("binancecoin", "BNB", "BNB", AssetKind.Crypto),
        new("ripple", "XRP", "XRP", AssetKind.Crypto),
        new("cardano", "ADA", "Cardano", AssetKind.Crypto),
        new("dogecoin", "DOGE", "Dogecoin", AssetKind.Crypto),
        new("litecoin", "LTC", "Litecoin", AssetKind.Crypto),
    ];

    // Precios de ejemplo en USD y variación 24 h en %.
    public Dictionary<string, decimal> PriceUsd { get; } = new()
    {
        ["bitcoin"] = 95_205m, ["ethereum"] = 3_459.60m, ["solana"] = 100.60m, ["tether"] = 1.00m,
        ["binancecoin"] = 640.25m, ["ripple"] = 2.15m, ["cardano"] = 0.7210m, ["dogecoin"] = 0.1840m,
        ["litecoin"] = 92.30m,
    };

    public Dictionary<string, decimal> Change24h { get; } = new()
    {
        ["usd"] = -0.4m, ["bitcoin"] = 2.1m, ["ethereum"] = -1.3m, ["solana"] = 3.6m, ["tether"] = 0.0m,
        ["binancecoin"] = 0.8m, ["ripple"] = -2.4m, ["cardano"] = 1.1m, ["dogecoin"] = -3.2m,
        ["litecoin"] = 0.5m,
    };

    public decimal FxBuy { get; private set; } = 503.12m;
    public decimal FxSell { get; private set; } = 509.48m;

    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.Now.AddMinutes(-2);

    public bool IsOffline => settings.SimulateOffline;

    public List<string> Watchlist { get; } = ["usd", "bitcoin", "ethereum", "solana"];

    public List<Transaction> Transactions { get; } =
    [
        new() { AssetId = "ethereum", Type = TxType.Buy, Quantity = 1.0m, UnitPrice = 3_650m, PriceCurrency = Currency.USD, Fee = 5m, Date = new(2026, 6, 20), FxRate = 518.40m },
        new() { AssetId = "bitcoin", Type = TxType.Buy, Quantity = 0.015m, UnitPrice = 86_500m, PriceCurrency = Currency.USD, Fee = 4m, Date = new(2026, 7, 15), FxRate = 514.10m },
        new() { AssetId = "usd", Type = TxType.Buy, Quantity = 3_200m, UnitPrice = 512.40m, PriceCurrency = Currency.CRC, Fee = 0m, Date = new(2026, 8, 3), FxRate = 1m },
        new() { AssetId = "ethereum", Type = TxType.Sell, Quantity = 0.15m, UnitPrice = 3_900m, PriceCurrency = Currency.USD, Fee = 2m, Date = new(2026, 9, 12), FxRate = 508.90m },
        new() { AssetId = "solana", Type = TxType.Buy, Quantity = 12.5m, UnitPrice = 92.40m, PriceCurrency = Currency.USD, Fee = 1.5m, Date = new(2026, 9, 28), FxRate = 506.30m },
        new() { AssetId = "bitcoin", Type = TxType.Buy, Quantity = 0.0065m, UnitPrice = 91_200m, PriceCurrency = Currency.USD, Fee = 2m, Date = new(2026, 10, 2), FxRate = 505.70m },
    ];

    public List<PriceAlert> Alerts { get; } =
    [
        new() { AssetId = "usd", Condition = AlertCondition.Below, Threshold = 500m, Currency = Currency.CRC },
        new() { AssetId = "bitcoin", Condition = AlertCondition.Above, Threshold = 100_000m, Currency = Currency.USD },
        new() { AssetId = "ethereum", Condition = AlertCondition.Below, Threshold = 3_200m, Currency = Currency.USD, IsActive = false },
    ];

    public List<AlertEvent> Events { get; } =
    [
        new(Guid.NewGuid(), Guid.Empty, "usd", AlertCondition.Below, 505m, Currency.CRC, 504.80m, DateTimeOffset.Now.AddHours(-2)),
        new(Guid.NewGuid(), Guid.Empty, "solana", AlertCondition.Above, 100m, Currency.USD, 100.40m, DateTimeOffset.Now.AddDays(-1)),
    ];

    /// <summary>Simula una consulta nueva: mueve un poco los precios.</summary>
    public void Tick()
    {
        if (IsOffline) return;
        foreach (var id in PriceUsd.Keys.ToList())
        {
            if (id == "tether") continue;
            var drift = (decimal)(_rng.NextDouble() - 0.5) * 0.003m;
            PriceUsd[id] = Math.Round(PriceUsd[id] * (1 + drift), PriceUsd[id] < 10 ? 4 : 2);
            Change24h[id] = Math.Round(Change24h[id] + drift * 100, 1);
        }
        UpdatedAt = DateTimeOffset.Now;
    }

    /// <summary>Precio en colones usado para valorar (tipo de cambio de compra).</summary>
    public decimal PriceCrc(string id) => id == Usd.Id ? FxBuy : PriceUsd.GetValueOrDefault(id) * FxBuy;

    public decimal PriceUsdOf(string id) => id == Usd.Id ? 1m : PriceUsd.GetValueOrDefault(id);

    public Asset? Find(string id) => Assets.FirstOrDefault(a => a.Id == id);

    public static Task Latency(int ms = 300) => Task.Delay(ms);
}
