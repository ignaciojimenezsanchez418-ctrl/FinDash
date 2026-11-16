namespace FinDashFrontEnd.Models;

public enum TxType { Buy, Sell }

/// <summary>Una compra o venta registrada por el usuario (RF-06).</summary>
public sealed class Transaction
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string AssetId { get; init; }
    public TxType Type { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public Currency PriceCurrency { get; init; }
    public decimal Fee { get; init; }
    public DateTime Date { get; init; }

    /// <summary>Tipo de cambio usado para pasar a colones el costo de esta transacción.</summary>
    public decimal FxRate { get; init; }
}

/// <summary>Posición de un activo, ya valorada (RF-08). Montos en colones.</summary>
public sealed record Holding(
    Asset Asset,
    decimal Quantity,
    decimal AvgCostCrc,
    decimal InvestedCrc,
    decimal ValueCrc,
    decimal PnlCrc,
    decimal PnlPct,
    decimal Change24hPct);

public sealed record PortfolioSummary(
    decimal ValueCrc,
    decimal InvestedCrc,
    decimal PnlCrc,
    decimal PnlPct,
    decimal DayChangeCrc,
    decimal DayChangePct,
    decimal FxRate,
    IReadOnlyList<Holding> Holdings);

public sealed record OperationResult(bool Ok, string? Error = null)
{
    public static OperationResult Success { get; } = new(true);
    public static OperationResult Fail(string error) => new(false, error);
}
