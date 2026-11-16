namespace FinDash.Core.Models;

public sealed record PriceAlert
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public required string AssetId { get; init; }
    public AlertCondition Condition { get; init; }
    public decimal Threshold { get; init; }
    public Currency Currency { get; init; }
    public bool IsActive { get; init; } = true;
    public AlertState State { get; init; } = AlertState.Active;
    public decimal? LastPrice { get; init; }
    public DateTimeOffset? LastTriggeredAt { get; init; }
}

public sealed record AlertEvent(
    Guid Id,
    Guid AlertId,
    Guid UserId,
    string AssetId,
    AlertCondition Condition,
    decimal Threshold,
    Currency Currency,
    decimal PriceAtTrigger,
    DateTimeOffset TriggeredAt,
    DateTimeOffset? DeliveredAt);
