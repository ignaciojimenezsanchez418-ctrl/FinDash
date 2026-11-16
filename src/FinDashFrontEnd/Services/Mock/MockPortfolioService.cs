using FinDashFrontEnd.Models;

namespace FinDashFrontEnd.Services.Mock;

/// <summary>
/// Cálculo provisional del portafolio con costo promedio ponderado (D10).
/// En la versión final este cálculo vive en FinDash.Core (PortfolioCalculator) y lo hace la API.
/// </summary>
public sealed class MockPortfolioService(MockBackend db) : IPortfolioService
{
    public async Task<PortfolioSummary> GetSummaryAsync()
    {
        await MockBackend.Latency(200);

        var holdings = new List<Holding>();
        foreach (var group in db.Transactions.GroupBy(t => t.AssetId))
        {
            var asset = db.Find(group.Key);
            if (asset is null) continue;

            decimal bought = 0, sold = 0, costCrc = 0;
            foreach (var t in group)
            {
                if (t.Type == TxType.Buy)
                {
                    bought += t.Quantity;
                    costCrc += ToCrc(t.Quantity * t.UnitPrice + t.Fee, t);
                }
                else
                {
                    sold += t.Quantity;
                }
            }

            var qty = bought - sold;
            if (qty <= 0) continue;

            var avgCost = bought == 0 ? 0 : costCrc / bought;
            var invested = qty * avgCost;
            var value = qty * db.PriceCrc(asset.Id);
            var pnl = value - invested;
            holdings.Add(new Holding(
                asset, qty, avgCost, invested, value, pnl,
                invested == 0 ? 0 : pnl / invested * 100,
                db.Change24h.GetValueOrDefault(asset.Id)));
        }

        var totalValue = holdings.Sum(h => h.ValueCrc);
        var totalInvested = holdings.Sum(h => h.InvestedCrc);
        var dayChange = holdings.Sum(h => h.ValueCrc - h.ValueCrc / (1 + h.Change24hPct / 100));
        var previous = totalValue - dayChange;

        return new PortfolioSummary(
            totalValue,
            totalInvested,
            totalValue - totalInvested,
            totalInvested == 0 ? 0 : (totalValue - totalInvested) / totalInvested * 100,
            dayChange,
            previous == 0 ? 0 : dayChange / previous * 100,
            db.FxBuy,
            holdings.OrderByDescending(h => h.ValueCrc).ToList());
    }

    public async Task<IReadOnlyList<Transaction>> GetTransactionsAsync()
    {
        await MockBackend.Latency(150);
        return db.Transactions.OrderByDescending(t => t.Date).ToList();
    }

    public async Task<decimal> GetQuantityAsync(string assetId)
    {
        await MockBackend.Latency(50);
        return Quantity(assetId);
    }

    public async Task<OperationResult> AddTransactionAsync(Transaction tx)
    {
        await MockBackend.Latency(500);
        if (db.IsOffline) return OperationResult.Fail("Sin conexión. Intente de nuevo cuando vuelva el internet.");
        if (tx.Quantity <= 0) return OperationResult.Fail("La cantidad debe ser mayor a cero.");
        if (tx.UnitPrice <= 0) return OperationResult.Fail("El precio debe ser mayor a cero.");
        if (tx.Type == TxType.Sell && tx.Quantity > Quantity(tx.AssetId))
            return OperationResult.Fail("No se puede vender más de lo que se tiene."); // RF-07

        db.Transactions.Add(tx);
        if (!db.Watchlist.Contains(tx.AssetId)) db.Watchlist.Add(tx.AssetId);
        return OperationResult.Success;
    }

    decimal Quantity(string assetId) =>
        db.Transactions.Where(t => t.AssetId == assetId)
            .Sum(t => t.Type == TxType.Buy ? t.Quantity : -t.Quantity);

    static decimal ToCrc(decimal amount, Transaction t) =>
        t.PriceCurrency == Currency.CRC ? amount : amount * t.FxRate;
}
