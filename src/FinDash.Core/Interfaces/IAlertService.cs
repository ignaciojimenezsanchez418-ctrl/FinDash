using FinDash.Core.Models;

namespace FinDash.Core.Interfaces;

public interface IAlertService
{
    Task<IReadOnlyList<PriceAlert>> GetAlertsAsync(Guid userId, CancellationToken ct = default);

    Task<PriceAlert?> GetAsync(Guid userId, Guid alertId, CancellationToken ct = default);

    Task<OperationResult> SaveAsync(Guid userId, PriceAlert alert, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid userId, Guid alertId, CancellationToken ct = default);

    Task<bool> SetActiveAsync(Guid userId, Guid alertId, bool active, CancellationToken ct = default);

    Task<IReadOnlyList<AlertEvent>> GetEventsAsync(Guid userId, CancellationToken ct = default);

    Task MarkDeliveredAsync(Guid userId, IReadOnlyCollection<Guid> eventIds, CancellationToken ct = default);
}
