namespace FinDashFrontEnd.Models;

public enum AlertCondition { Above, Below }

/// <summary>Alerta de precio o de tipo de cambio (RF-10).</summary>
public sealed class PriceAlert
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string AssetId { get; set; }
    public AlertCondition Condition { get; set; }
    public decimal Threshold { get; set; }
    public Currency Currency { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Una alerta que el servidor marcó como disparada (RF-11).</summary>
public sealed record AlertEvent(
    Guid Id,
    Guid AlertId,
    string AssetId,
    AlertCondition Condition,
    decimal Threshold,
    Currency Currency,
    decimal PriceAtTrigger,
    DateTimeOffset TriggeredAt);
