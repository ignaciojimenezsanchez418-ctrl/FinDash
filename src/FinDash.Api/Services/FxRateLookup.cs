using FinDash.Api.Data;
using FinDash.Api.Integrations;
using FinDash.Core.Interfaces;
using FinDash.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FinDash.Api.Services;

/// <summary>
/// Tipo de cambio de compra de una fecha, para una transacción en USD que llega sin FxRate.
/// Busca en PriceSnapshots; si no hay (fechas antes de que arrancara el servidor) lo pide a la
/// fuente; como último recurso usa el de hoy.
/// </summary>
public sealed class FxRateLookup(FinDashDbContext db, IExchangeRateProvider fx, QuoteStore store, ILogger<FxRateLookup> log)
{
    public async Task<decimal?> GetBuyRateAsync(DateTime date, CancellationToken ct)
    {
        var day = DateOnly.FromDateTime(date);
        var from = new DateTimeOffset(day.AddDays(-6).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var to = new DateTimeOffset(day.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var saved = await db.PriceSnapshots
            .Where(s => s.AssetId == Asset.UsdId && s.CapturedAt >= from && s.CapturedAt < to)
            .OrderByDescending(s => s.CapturedAt)
            .Select(s => (decimal?)s.PriceCrc)
            .FirstOrDefaultAsync(ct);
        if (saved is not null) return saved;

        try
        {
            var rates = await fx.GetHistoryAsync(day.AddDays(-7), day, ct);
            if (rates.Count > 0) return rates[^1].Buy;
        }
        catch (Exception ex) when (ex is HttpRequestException or RateLimitedException)
        {
            log.LogWarning("No se pudo obtener el tipo de cambio del {Date}: {Message}", day, ex.Message);
        }

        return store.Current.Fx?.Buy;
    }
}
