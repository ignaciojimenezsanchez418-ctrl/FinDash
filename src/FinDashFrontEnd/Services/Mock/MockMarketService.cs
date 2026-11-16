using FinDashFrontEnd.Models;

namespace FinDashFrontEnd.Services.Mock;

public sealed class MockMarketService(MockBackend db) : IMarketService
{
    public IReadOnlyList<Asset> Assets => db.Assets;

    public Asset? GetAsset(string id) => db.Find(id);

    public async Task<MarketSnapshot> GetSnapshotAsync(bool forceRefresh = false)
    {
        await MockBackend.Latency(forceRefresh ? 700 : 250);
        if (forceRefresh) db.Tick();

        var quotes = db.Assets.ToDictionary(
            a => a.Id,
            a => new Quote(a.Id, db.PriceUsdOf(a.Id), db.PriceCrc(a.Id), db.Change24h.GetValueOrDefault(a.Id)));

        var fx = new ExchangeRate(db.FxBuy, db.FxSell, db.Change24h["usd"]);
        return new MarketSnapshot(fx, quotes, db.IsOffline, db.UpdatedAt);
    }

    public async Task<IReadOnlyList<Asset>> SearchAsync(string query)
    {
        await MockBackend.Latency(150);
        var q = query.Trim();
        if (q.Length == 0) return db.Assets;
        return db.Assets
            .Where(a => a.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                     || a.Symbol.Contains(q, StringComparison.OrdinalIgnoreCase)
                     || a.Id.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<IReadOnlyList<PricePoint>> GetHistoryAsync(string assetId, ChartRange range)
    {
        await MockBackend.Latency(250);

        var (count, step) = range switch
        {
            ChartRange.Day => (48, TimeSpan.FromMinutes(30)),
            ChartRange.Week => (84, TimeSpan.FromHours(2)),
            ChartRange.Month => (60, TimeSpan.FromHours(12)),
            _ => (52, TimeSpan.FromDays(7)),
        };
        var volatility = assetId == MockBackend.Usd.Id ? 0.0015 : 0.012;
        var rng = new Random(StableSeed(assetId + range));

        // Camino aleatorio hacia atrás que termina en el precio actual.
        var prices = new double[count];
        prices[count - 1] = (double)db.PriceCrc(assetId);
        for (var i = count - 1; i > 0; i--)
            prices[i - 1] = prices[i] / (1 + (rng.NextDouble() - 0.48) * 2 * volatility);

        // En 24 h la gráfica debe empezar donde indica la variación del día.
        if (range == ChartRange.Day)
        {
            var start = prices[count - 1] / (1 + (double)db.Change24h.GetValueOrDefault(assetId) / 100);
            var factor = start / prices[0];
            for (var i = 0; i < count; i++)
                prices[i] *= Math.Pow(factor, (double)(count - 1 - i) / (count - 1));
        }

        var now = DateTimeOffset.Now;
        var fx = (double)db.FxBuy;
        return prices
            .Select((p, i) => new PricePoint(now - step * (count - 1 - i), (decimal)p, assetId == MockBackend.Usd.Id ? 1m : (decimal)(p / fx)))
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetWatchlistAsync()
    {
        await MockBackend.Latency(50);
        return db.Watchlist.ToList();
    }

    public async Task AddToWatchlistAsync(string assetId)
    {
        await MockBackend.Latency(150);
        if (!db.Watchlist.Contains(assetId)) db.Watchlist.Add(assetId);
    }

    public async Task RemoveFromWatchlistAsync(string assetId)
    {
        await MockBackend.Latency(150);
        db.Watchlist.Remove(assetId);
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
