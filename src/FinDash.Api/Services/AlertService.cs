using FinDash.Api.Data;
using FinDash.Core.Interfaces;
using FinDash.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FinDash.Api.Services;

/// <summary>Alertas con EF Core (RF-10, RF-11).</summary>
public sealed class AlertService(FinDashDbContext db, TimeProvider time) : IAlertService
{
    public async Task<IReadOnlyList<PriceAlert>> GetAlertsAsync(Guid userId, CancellationToken ct = default)
    {
        var list = await db.PriceAlerts.AsNoTracking().Where(a => a.UserId == userId).ToListAsync(ct);
        return list.OrderBy(a => a.CreatedAt).Select(a => a.ToModel()).ToList();
    }

    public async Task<PriceAlert?> GetAsync(Guid userId, Guid alertId, CancellationToken ct = default) =>
        (await Find(userId, alertId, ct))?.ToModel();

    /// <summary>
    /// Crea o edita (el id lo genera la app). Si cambia el activo, la condición, el umbral o la
    /// moneda, la alerta vuelve a Active sin precio anterior: si ya se cumple, avisa una vez.
    /// </summary>
    public async Task<OperationResult> SaveAsync(Guid userId, PriceAlert alert, CancellationToken ct = default)
    {
        if (!await db.Assets.AnyAsync(a => a.Id == alert.AssetId, ct)) return OperationResult.Fail(ServiceErrors.AssetNotFound);
        if (alert.Threshold <= 0) return OperationResult.Fail("El monto debe ser mayor a cero.");
        if (alert.AssetId == Asset.UsdId && alert.Currency != Currency.CRC)
            return OperationResult.Fail("Las alertas del dólar se definen en colones.");

        var id = alert.Id == Guid.Empty ? Guid.NewGuid() : alert.Id;
        var entity = await db.PriceAlerts.FirstOrDefaultAsync(a => a.Id == id, ct);

        if (entity is null)
        {
            db.PriceAlerts.Add(new PriceAlertEntity
            {
                Id = id,
                UserId = userId,
                AssetId = alert.AssetId,
                Condition = alert.Condition,
                Threshold = alert.Threshold,
                Currency = alert.Currency,
                IsActive = alert.IsActive,
                CreatedAt = time.GetUtcNow(),
            });
        }
        else
        {
            if (entity.UserId != userId) return OperationResult.Fail(ServiceErrors.AlertNotFound);

            var ruleChanged = entity.AssetId != alert.AssetId || entity.Condition != alert.Condition
                || entity.Threshold != alert.Threshold || entity.Currency != alert.Currency;
            entity.AssetId = alert.AssetId;
            entity.Condition = alert.Condition;
            entity.Threshold = alert.Threshold;
            entity.Currency = alert.Currency;
            entity.IsActive = alert.IsActive;
            if (ruleChanged)
            {
                entity.State = AlertState.Active;
                entity.LastPrice = null;
            }
        }

        await db.SaveChangesAsync(ct);
        return OperationResult.Success;
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid alertId, CancellationToken ct = default) =>
        await db.PriceAlerts.Where(a => a.Id == alertId && a.UserId == userId).ExecuteDeleteAsync(ct) > 0;

    public async Task<bool> SetActiveAsync(Guid userId, Guid alertId, bool active, CancellationToken ct = default)
    {
        var entity = await Find(userId, alertId, ct);
        if (entity is null) return false;
        entity.IsActive = active;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<AlertEvent>> GetEventsAsync(Guid userId, CancellationToken ct = default)
    {
        var list = await db.AlertEvents.AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.TriggeredAt)
            .Take(100)
            .ToListAsync(ct);
        return list.Select(e => e.ToModel()).ToList();
    }

    public async Task MarkDeliveredAsync(Guid userId, IReadOnlyCollection<Guid> eventIds, CancellationToken ct = default)
    {
        var ids = eventIds.ToList();
        var events = await db.AlertEvents
            .Where(e => e.UserId == userId && ids.Contains(e.Id) && e.DeliveredAt == null)
            .ToListAsync(ct);
        var now = time.GetUtcNow();
        foreach (var e in events) e.DeliveredAt = now;
        await db.SaveChangesAsync(ct);
    }

    Task<PriceAlertEntity?> Find(Guid userId, Guid alertId, CancellationToken ct) =>
        db.PriceAlerts.FirstOrDefaultAsync(a => a.Id == alertId && a.UserId == userId, ct);
}
