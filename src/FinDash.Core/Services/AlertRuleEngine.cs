using FinDash.Core.Models;

namespace FinDash.Core.Services;

/// <summary>Resultado de evaluar una alerta: la alerta actualizada y el evento si se disparó.</summary>
public sealed record AlertEvaluation(PriceAlert Alert, AlertEvent? Event);

/// <summary>
/// Reglas de las alertas (D11), como función pura (contrato §3): recibe la alerta, el precio
/// nuevo y la hora, y devuelve la alerta actualizada y, si se disparó, el evento.
///
/// - "Sube de" se cumple con precio ≥ umbral; "baja de" con precio ≤ umbral.
/// - Se dispara cuando el precio CRUZA el umbral: el precio anterior (LastPrice) no cumplía y
///   el nuevo sí. Si no hay precio anterior (alerta nueva o editada) y ya se cumple, avisa una vez.
/// - Después de dispararse queda Triggered: aunque el precio siga del mismo lado, no repite.
/// - Cuando el precio vuelve al otro lado pasa a Rearmed y puede volver a dispararse, pero no
///   antes del cooldown contado desde el último disparo.
/// - Una alerta apagada (IsActive = false) no se dispara, pero sí guarda el último precio.
/// </summary>
public static class AlertRuleEngine
{
    public static readonly TimeSpan DefaultCooldown = TimeSpan.FromMinutes(60);

    public static bool IsMet(AlertCondition condition, decimal threshold, decimal price) =>
        condition == AlertCondition.Above ? price >= threshold : price <= threshold;

    public static AlertEvaluation Evaluate(PriceAlert alert, decimal price, DateTimeOffset now, TimeSpan cooldown)
    {
        // Sin cotización no se decide nada ni se toca la alerta.
        if (price <= 0) return new AlertEvaluation(alert, null);

        var met = IsMet(alert.Condition, alert.Threshold, price);
        var updated = alert with { LastPrice = price };

        if (!alert.IsActive) return new AlertEvaluation(updated, null);

        if (!met)
        {
            return alert.State == AlertState.Triggered
                ? new AlertEvaluation(updated with { State = AlertState.Rearmed }, null)
                : new AlertEvaluation(updated, null);
        }

        if (alert.State == AlertState.Triggered) return new AlertEvaluation(updated, null);

        // Rearmed ya implica que el precio volvió al otro lado: cuenta como cruce aunque el
        // disparo haya tenido que esperar el cooldown.
        var crossed = alert.State == AlertState.Rearmed
            || alert.LastPrice is null
            || !IsMet(alert.Condition, alert.Threshold, alert.LastPrice.Value);
        var cooldownOver = alert.LastTriggeredAt is null || now - alert.LastTriggeredAt.Value >= cooldown;

        if (!crossed || !cooldownOver) return new AlertEvaluation(updated, null);

        var fired = updated with { State = AlertState.Triggered, LastTriggeredAt = now };
        var ev = new AlertEvent(
            Guid.NewGuid(), alert.Id, alert.UserId, alert.AssetId, alert.Condition,
            alert.Threshold, alert.Currency, price, now, DeliveredAt: null);
        return new AlertEvaluation(fired, ev);
    }
}
