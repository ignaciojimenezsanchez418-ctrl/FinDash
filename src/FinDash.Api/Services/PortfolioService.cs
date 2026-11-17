using FinDash.Api.Data;
using FinDash.Core.Interfaces;
using FinDash.Core.Models;
using FinDash.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace FinDash.Api.Services;

/// <summary>Portafolio con EF Core y PortfolioCalculator (RF-06 a RF-08).</summary>
public sealed class PortfolioService(FinDashDbContext db, QuoteStore store, FxRateLookup fxLookup, TimeProvider time)
    : IPortfolioService
{
    /// <summary>Requiere tipo de cambio en la caché; el controller lo revisa antes.</summary>
    public async Task<PortfolioSummary> GetSummaryAsync(Guid userId, CancellationToken ct = default)
    {
        var market = store.Current;
        if (market.Fx is null) throw new InvalidOperationException("Todavía no hay tipo de cambio.");
        return PortfolioCalculator.Calculate(await LoadAsync(userId, ct), market.PricesCrc(), market.Fx.Buy);
    }

    public async Task<IReadOnlyList<Transaction>> GetTransactionsAsync(Guid userId, CancellationToken ct = default)
    {
        var list = await db.Transactions.AsNoTracking().Where(t => t.UserId == userId).ToListAsync(ct);
        return list.OrderByDescending(t => t.Date).ThenByDescending(t => t.CreatedAt).Select(t => t.ToModel()).ToList();
    }

    public async Task<decimal> GetQuantityAsync(Guid userId, string assetId, CancellationToken ct = default) =>
        PortfolioCalculator.QuantityOf(await LoadAsync(userId, ct), assetId);

    public async Task<OperationResult> AddTransactionAsync(Guid userId, Transaction tx, CancellationToken ct = default)
    {
        if (tx.Id == Guid.Empty) tx = tx with { Id = Guid.NewGuid() };

        // El id lo genera la app: si llega repetido se responde igual (contrato §1).
        var existing = await db.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tx.Id, ct);
        if (existing is not null)
            return existing.UserId == userId ? OperationResult.Success : OperationResult.Fail(ServiceErrors.IdInUse);

        if (!await db.Assets.AnyAsync(a => a.Id == tx.AssetId, ct)) return OperationResult.Fail(ServiceErrors.AssetNotFound);

        if (tx.PriceCurrency == Currency.USD && tx.FxRate <= 0)
        {
            var rate = await fxLookup.GetBuyRateAsync(tx.Date, ct);
            if (rate is not > 0) return OperationResult.Fail(ServiceErrors.FxUnavailable);
            tx = tx with { FxRate = rate.Value };
        }
        if (tx.PriceCurrency == Currency.CRC) tx = tx with { FxRate = 1m };

        // Un día de margen por la diferencia de zona horaria entre el teléfono y el servidor.
        if (PortfolioCalculator.Validate(tx, time.GetUtcNow().UtcDateTime.AddDays(1)) is { } error)
            return OperationResult.Fail(error);

        // RF-07: con la nueva transacción ninguna venta puede dejar saldo negativo en su fecha.
        var all = await LoadAsync(userId, ct);
        if (PortfolioCalculator.FindOversell(all.Append(tx)) is not null)
            return OperationResult.Fail(ServiceErrors.Oversell);

        var now = time.GetUtcNow();
        db.Transactions.Add(new TransactionEntity
        {
            Id = tx.Id,
            UserId = userId,
            AssetId = tx.AssetId,
            Type = tx.Type,
            Quantity = tx.Quantity,
            UnitPrice = tx.UnitPrice,
            PriceCurrency = tx.PriceCurrency,
            Fee = tx.Fee,
            Date = tx.Date.Date,
            FxRate = tx.FxRate,
            CreatedAt = now,
        });

        // Igual que el mock de la app: registrar un movimiento agrega el activo a la watchlist.
        if (!await db.WatchlistItems.AnyAsync(w => w.UserId == userId && w.AssetId == tx.AssetId, ct))
            db.WatchlistItems.Add(new WatchlistItem { UserId = userId, AssetId = tx.AssetId, AddedAt = now });

        await db.SaveChangesAsync(ct);
        return OperationResult.Success;
    }

    /// <summary>Transacciones del usuario en el orden en que se registraron.</summary>
    async Task<List<Transaction>> LoadAsync(Guid userId, CancellationToken ct) =>
        (await db.Transactions.AsNoTracking().Where(t => t.UserId == userId).ToListAsync(ct))
            .OrderBy(t => t.CreatedAt)
            .Select(t => t.ToModel())
            .ToList();
}
