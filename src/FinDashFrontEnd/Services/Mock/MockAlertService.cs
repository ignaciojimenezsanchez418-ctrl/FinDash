using FinDashFrontEnd.Models;

namespace FinDashFrontEnd.Services.Mock;

public sealed class MockAlertService(MockBackend db) : IAlertService
{
    public async Task<IReadOnlyList<PriceAlert>> GetAlertsAsync()
    {
        await MockBackend.Latency(150);
        return db.Alerts.ToList();
    }

    public async Task<IReadOnlyList<AlertEvent>> GetFiredAsync()
    {
        await MockBackend.Latency(100);
        return db.Events.OrderByDescending(e => e.TriggeredAt).ToList();
    }

    public Task<PriceAlert?> GetAsync(Guid id) =>
        Task.FromResult(db.Alerts.FirstOrDefault(a => a.Id == id));

    public async Task<OperationResult> SaveAsync(PriceAlert alert)
    {
        await MockBackend.Latency(400);
        if (db.IsOffline) return OperationResult.Fail("Sin conexión. Intente de nuevo cuando vuelva el internet.");
        if (alert.Threshold <= 0) return OperationResult.Fail("El monto debe ser mayor a cero.");

        var index = db.Alerts.FindIndex(a => a.Id == alert.Id);
        if (index >= 0) db.Alerts[index] = alert;
        else db.Alerts.Add(alert);
        return OperationResult.Success;
    }

    public async Task DeleteAsync(Guid id)
    {
        await MockBackend.Latency(300);
        db.Alerts.RemoveAll(a => a.Id == id);
    }

    public async Task SetActiveAsync(Guid id, bool active)
    {
        await MockBackend.Latency(100);
        var alert = db.Alerts.FirstOrDefault(a => a.Id == id);
        if (alert is not null) alert.IsActive = active;
    }
}
