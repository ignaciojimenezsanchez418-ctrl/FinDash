using FinDash.Api.Services;
using FinDash.Core.Interfaces;
using FinDash.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinDash.Api.Controllers;

/// <summary>Lo que manda la app al guardar (forma de su PriceAlert).</summary>
public sealed record AlertInput(Guid Id, string AssetId, AlertCondition Condition, decimal Threshold, Currency Currency, bool IsActive = true);

public sealed record AlertDto(
    Guid Id, string AssetId, AlertCondition Condition, decimal Threshold, Currency Currency,
    bool IsActive, AlertState State, DateTimeOffset? LastTriggeredAt);

public sealed record ActiveInput(bool IsActive);

public sealed record AlertEventDto(
    Guid Id, Guid AlertId, string AssetId, AlertCondition Condition, decimal Threshold, Currency Currency,
    decimal PriceAtTrigger, DateTimeOffset TriggeredAt, DateTimeOffset? DeliveredAt);

public sealed record DeliveredInput(IReadOnlyList<Guid> Ids);

/// <summary>Alertas de precio y de tipo de cambio (RF-10, RF-11).</summary>
[Route("api/alerts")]
public sealed class AlertsController(IAlertService alerts) : ApiControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<AlertDto>> GetAll(CancellationToken ct) =>
        (await alerts.GetAlertsAsync(UserId, ct)).Select(ToDto);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AlertDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        await alerts.GetAsync(UserId, id, ct) is { } alert ? Ok(ToDto(alert)) : Error(ServiceErrors.AlertNotFound);

    /// <summary>Crea o edita (el id lo genera la app). 200 · 400 "El monto debe ser mayor a cero."</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<AlertDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Save(Guid id, AlertInput body, CancellationToken ct)
    {
        var alert = new PriceAlert
        {
            Id = id,
            AssetId = body.AssetId,
            Condition = body.Condition,
            Threshold = body.Threshold,
            Currency = body.Currency,
            IsActive = body.IsActive,
        };
        var result = await alerts.SaveAsync(UserId, alert, ct);
        if (!result.Ok) return Error(result.Error!);
        return Ok(ToDto((await alerts.GetAsync(UserId, id, ct))!));
    }

    /// <summary>204 aunque no exista (la app no distingue).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await alerts.DeleteAsync(UserId, id, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, ActiveInput body, CancellationToken ct) =>
        await alerts.SetActiveAsync(UserId, id, body.IsActive, ct) ? NoContent() : Error(ServiceErrors.AlertNotFound);

    /// <summary>Alertas disparadas, más reciente primero. deliveredAt null = la app no la ha notificado.</summary>
    [HttpGet("events")]
    public async Task<IEnumerable<AlertEventDto>> Events(CancellationToken ct) =>
        (await alerts.GetEventsAsync(UserId, ct)).Select(e =>
            new AlertEventDto(e.Id, e.AlertId, e.AssetId, e.Condition, e.Threshold, e.Currency, e.PriceAtTrigger, e.TriggeredAt, e.DeliveredAt));

    /// <summary>Marca eventos como notificados para no repetirlos (RF-11).</summary>
    [HttpPost("events/delivered")]
    public async Task<IActionResult> Delivered(DeliveredInput body, CancellationToken ct)
    {
        await alerts.MarkDeliveredAsync(UserId, body.Ids ?? [], ct);
        return NoContent();
    }

    static AlertDto ToDto(PriceAlert a) =>
        new(a.Id, a.AssetId, a.Condition, a.Threshold, a.Currency, a.IsActive, a.State, a.LastTriggeredAt);
}
