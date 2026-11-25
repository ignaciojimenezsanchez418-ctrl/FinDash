using FinDash.Core.Models;

namespace FinDash.Api.Data;

// Las 7 tablas del documento (sección 6) con los ajustes del contrato (sección 4).
// Las entidades son clases propias de la API; los modelos de FinDash.Core no saben de EF.

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public Currency BaseCurrency { get; set; } = Currency.CRC;
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Id = id externo ("bitcoin" de CoinGecko, "usd" para el tipo de cambio).</summary>
public sealed class AssetEntity
{
    public required string Id { get; set; }
    public required string Symbol { get; set; }
    public required string Name { get; set; }
    public AssetKind Kind { get; set; }

    public required string Source { get; set; }

    public Asset ToModel() => new(Id, Symbol, Name, Kind);
}

public sealed class WatchlistItem
{
    public Guid UserId { get; set; }
    public required string AssetId { get; set; }
    public DateTimeOffset AddedAt { get; set; }
}

public sealed class TransactionEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string AssetId { get; set; }
    public TxType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public Currency PriceCurrency { get; set; }
    public decimal Fee { get; set; }
    public DateTime Date { get; set; }

    /// <summary>Colones por dólar en la fecha de la transacción (contrato §5).</summary>
    public decimal FxRate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Transaction ToModel() => new(Id, AssetId, Type, Quantity, UnitPrice, PriceCurrency, Fee, Date, FxRate);
}

public sealed class PriceAlertEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string AssetId { get; set; }
    public AlertCondition Condition { get; set; }
    public decimal Threshold { get; set; }
    public Currency Currency { get; set; }
    public bool IsActive { get; set; } = true;
    public AlertState State { get; set; } = AlertState.Active;
    public decimal? LastPrice { get; set; }
    public DateTimeOffset? LastTriggeredAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public PriceAlert ToModel() => new()
    {
        Id = Id,
        UserId = UserId,
        AssetId = AssetId,
        Condition = Condition,
        Threshold = Threshold,
        Currency = Currency,
        IsActive = IsActive,
        State = State,
        LastPrice = LastPrice,
        LastTriggeredAt = LastTriggeredAt,
    };

    /// <summary>Copia el estado que calcula AlertRuleEngine.</summary>
    public void Apply(PriceAlert a)
    {
        IsActive = a.IsActive;
        State = a.State;
        LastPrice = a.LastPrice;
        LastTriggeredAt = a.LastTriggeredAt;
    }
}

/// <summary>
/// Copia activo, condición y umbral para que el historial no cambie si se edita la alerta.
/// No tiene llave foránea a PriceAlerts: al borrar una alerta, sus eventos se conservan.
/// </summary>
public sealed class AlertEventEntity
{
    public Guid Id { get; set; }
    public Guid AlertId { get; set; }
    public Guid UserId { get; set; }
    public required string AssetId { get; set; }
    public AlertCondition Condition { get; set; }
    public decimal Threshold { get; set; }
    public Currency Currency { get; set; }
    public decimal PriceAtTrigger { get; set; }
    public DateTimeOffset TriggeredAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }

    public AlertEvent ToModel() =>
        new(Id, AlertId, UserId, AssetId, Condition, Threshold, Currency, PriceAtTrigger, TriggeredAt, DeliveredAt);

    public static AlertEventEntity From(AlertEvent e) => new()
    {
        Id = e.Id,
        AlertId = e.AlertId,
        UserId = e.UserId,
        AssetId = e.AssetId,
        Condition = e.Condition,
        Threshold = e.Threshold,
        Currency = e.Currency,
        PriceAtTrigger = e.PriceAtTrigger,
        TriggeredAt = e.TriggeredAt,
        DeliveredAt = e.DeliveredAt,
    };
}

/// <summary>Una fila por activo y ciclo de PriceService (contrato §4).</summary>
public sealed class PriceSnapshot
{
    public long Id { get; set; }
    public required string AssetId { get; set; }

    /// <summary>1 para "usd".</summary>
    public decimal PriceUsd { get; set; }

    public decimal PriceCrc { get; set; }

    public decimal? SellCrc { get; set; }

    public decimal Change24hPct { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
}
