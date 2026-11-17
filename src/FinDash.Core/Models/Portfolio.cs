namespace FinDash.Core.Models;

public sealed record Transaction(
    Guid Id,
    string AssetId,
    TxType Type,
    decimal Quantity,
    decimal UnitPrice,
    Currency PriceCurrency,
    decimal Fee,
    DateTime Date,
    decimal FxRate);

public sealed record Holding(
    string AssetId,
    decimal Quantity,
    decimal AvgCostCrc,
    decimal InvestedCrc,
    decimal ValueCrc,
    decimal ValueUsd,
    decimal PnlCrc,
    decimal PnlUsd,
    decimal PnlPct,
    decimal Change24hPct);

public sealed record PortfolioSummary(
    decimal ValueCrc,
    decimal ValueUsd,
    decimal InvestedCrc,
    decimal PnlCrc,
    decimal PnlUsd,
    decimal PnlPct,
    decimal DayChangeCrc,
    decimal DayChangePct,
    decimal FxRate,
    IReadOnlyList<Holding> Holdings,
    decimal RealizedPnlCrc = 0);

/// <summary>Precio actual de un activo en colones y su variación de 24 h.</summary>
public sealed record AssetPrice(decimal PriceCrc, decimal Change24hPct);

public sealed record OperationResult(bool Ok, string? Error = null)
{
    public static OperationResult Success { get; } = new(true);

    public static OperationResult Fail(string error) => new(false, error);
}
