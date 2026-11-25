using FinDash.Api.Data;
using FinDash.Core.Models;
using FinDash.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FinDash.Api.Services;

public sealed class AlertOptions
{
    public int EvaluateEverySeconds { get; set; } = 60;
    public int CooldownMinutes { get; set; } = 60;
}

/// <summary>
/// Pasa todas las alertas por AlertRuleEngine con los precios de la caché y guarda el
/// resultado (contrato §3: "AlertWorker solo lee y guarda"). Las apagadas también se
/// evalúan para guardar el último precio y así detectar el cruce cuando se enciendan.
/// </summary>
public sealed class AlertEvaluator(FinDashDbContext db, QuoteStore store, TimeProvider time, IOptions<AlertOptions> options)
{
    /// <summary>Devuelve cuántas alertas se dispararon.</summary>
    public async Task<int> EvaluateAsync(CancellationToken ct)
    {
        var market = store.Current;
        var now = time.GetUtcNow();
        var cooldown = TimeSpan.FromMinutes(Math.Max(0, options.Value.CooldownMinutes));
        var fired = 0;

        foreach (var entity in await db.PriceAlerts.ToListAsync(ct))
        {
            // Contrato §5: el dólar en CRC con el tipo de compra; la cripto en la moneda de la alerta.
            // Una alerta en USD no necesita tipo de cambio, así que se evalúa aunque falte.
            var price = entity.Currency == Currency.USD
                ? market.Crypto.GetValueOrDefault(entity.AssetId)?.PriceUsd ?? 0
                : market.Quote(entity.AssetId)?.PriceCrc ?? 0;
            if (price <= 0) continue;

            var result = AlertRuleEngine.Evaluate(entity.ToModel(), price, now, cooldown);

            entity.Apply(result.Alert);
            if (result.Event is { } ev)
            {
                db.AlertEvents.Add(AlertEventEntity.From(ev));
                fired++;
            }
        }

        await db.SaveChangesAsync(ct);
        return fired;
    }
}
