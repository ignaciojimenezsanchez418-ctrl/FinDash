using FinDash.Core.Interfaces;
using FinDash.Core.Models;

namespace FinDash.Api.Integrations;

/// <summary>
/// Precios de ejemplo para pruebas, desarrollo sin llave y plan B de la demo (riesgo de la
/// sección 14). Arranca con los mismos valores que MockBackend de la app.
/// </summary>
public sealed class FakeMarketDataProvider(TimeProvider time) : IMarketDataProvider
{
    static readonly Dictionary<string, (decimal Price, decimal Change)> Base = new()
    {
        ["bitcoin"] = (95_205m, 2.1m), ["ethereum"] = (3_459.60m, -1.3m), ["tether"] = (1.00m, 0m),
        ["usd-coin"] = (1.00m, 0m), ["binancecoin"] = (640.25m, 0.8m), ["solana"] = (100.60m, 3.6m),
        ["ripple"] = (2.15m, -2.4m), ["cardano"] = (0.7210m, 1.1m), ["dogecoin"] = (0.1840m, -3.2m),
        ["tron"] = (0.2410m, 0.4m), ["avalanche-2"] = (36.80m, -0.9m), ["chainlink"] = (19.45m, 1.7m),
        ["polkadot"] = (6.92m, -1.1m), ["litecoin"] = (92.30m, 0.5m), ["bitcoin-cash"] = (455.10m, 0.2m),
        ["stellar"] = (0.3920m, -0.6m),
    };

    readonly Random _rng = new(17);
    readonly Lock _lock = new();
    readonly Dictionary<string, decimal> _drift = [];

    public Task<IReadOnlyList<MarketQuote>> GetQuotesAsync(IReadOnlyCollection<string> assetIds, CancellationToken ct = default)
    {
        var now = time.GetUtcNow();
        var result = new List<MarketQuote>();
        lock (_lock)
        {
            foreach (var id in assetIds)
            {
                if (!Base.TryGetValue(id, out var b)) continue;
                var drift = _drift.GetValueOrDefault(id, 1m);
                if (b.Change != 0) drift *= 1 + (decimal)(_rng.NextDouble() - 0.5) * 0.003m;
                _drift[id] = drift;
                result.Add(new MarketQuote(id, Math.Round(b.Price * drift, b.Price < 10 ? 4 : 2), b.Change, now));
            }
        }
        return Task.FromResult<IReadOnlyList<MarketQuote>>(result);
    }

    public Task<IReadOnlyList<HistoryPoint>> GetHistoryAsync(string assetId, ChartRange range, CancellationToken ct = default)
    {
        var (count, step) = range switch
        {
            ChartRange.Day => (48, TimeSpan.FromMinutes(30)),
            ChartRange.Week => (84, TimeSpan.FromHours(2)),
            ChartRange.Month => (60, TimeSpan.FromHours(12)),
            _ => (52, TimeSpan.FromDays(7)),
        };
        var rng = new Random(StableSeed(assetId + range));
        var prices = new decimal[count];
        prices[^1] = Base.TryGetValue(assetId, out var b) ? b.Price : 1m;
        for (var i = count - 1; i > 0; i--)
            prices[i - 1] = prices[i] / (1 + (decimal)(rng.NextDouble() - 0.48) * 0.024m);

        var now = time.GetUtcNow();
        IReadOnlyList<HistoryPoint> points = prices
            .Select((p, i) => new HistoryPoint(now - step * (count - 1 - i), Math.Round(p, 6)))
            .ToList();
        return Task.FromResult(points);
    }

    static int StableSeed(string s)
    {
        unchecked
        {
            var h = 23;
            foreach (var ch in s) h = h * 31 + ch;
            return h;
        }
    }
}

public sealed class FakeExchangeRateProvider(TimeProvider time) : IExchangeRateProvider
{
    public async Task<FxQuote> GetLatestAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        return (await GetHistoryAsync(today, today, ct))[^1];
    }

    public Task<IReadOnlyList<FxQuote>> GetHistoryAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var rates = new List<FxQuote>();
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            // Movimiento suave y repetible: entre ~₡498 y ~₡508.
            var buy = Math.Round(503.12m + (decimal)Math.Sin(d.DayNumber / 9.0) * 5m, 2);
            rates.Add(new FxQuote(buy, buy + 6.36m, new DateTimeOffset(d.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(-6))));
        }
        return Task.FromResult<IReadOnlyList<FxQuote>>(rates);
    }
}
