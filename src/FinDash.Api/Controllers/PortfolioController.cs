using FinDash.Api.Data;
using FinDash.Api.Services;
using FinDash.Core.Interfaces;
using FinDash.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinDash.Api.Controllers;

/// <summary>Forma de Holding de la app: trae el activo completo (contrato §2).</summary>
public sealed record HoldingDto(
    Asset Asset,
    decimal Quantity,
    decimal AvgCostCrc,
    decimal InvestedCrc,
    decimal ValueCrc,
    decimal ValueUsd,
    decimal PnlCrc,
    decimal PnlUsd,
    decimal PnlPct,
    decimal Change24hPct);

/// <summary>Forma de PortfolioSummary de la app, más los montos en USD y la ganancia realizada.</summary>
public sealed record PortfolioSummaryDto(
    decimal ValueCrc,
    decimal ValueUsd,
    decimal InvestedCrc,
    decimal PnlCrc,
    decimal PnlUsd,
    decimal PnlPct,
    decimal RealizedPnlCrc,
    decimal DayChangeCrc,
    decimal DayChangePct,
    decimal FxRate,
    IReadOnlyList<HoldingDto> Holdings);

/// <summary>Portafolio y transacciones (RF-06 a RF-08).</summary>
[Route("api/portfolio")]
public sealed class PortfolioController(IPortfolioService portfolio, QuoteStore store, FinDashDbContext db) : ApiControllerBase
{
    /// <summary>Patrimonio, costo promedio y ganancia o pérdida en CRC y USD.</summary>
    [HttpGet("summary")]
    [ProducesResponseType<PortfolioSummaryDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        if (store.Current.Fx is null) return Error("El servidor todavía no tiene tipo de cambio para valorar el portafolio.", 503);

        var s = await portfolio.GetSummaryAsync(UserId, ct);
        var assets = (await db.Assets.AsNoTracking().ToListAsync(ct)).ToDictionary(a => a.Id, a => a.ToModel());
        var holdings = s.Holdings
            .Where(h => assets.ContainsKey(h.AssetId))
            .Select(h => new HoldingDto(assets[h.AssetId], h.Quantity, h.AvgCostCrc, h.InvestedCrc, h.ValueCrc,
                h.ValueUsd, h.PnlCrc, h.PnlUsd, h.PnlPct, h.Change24hPct))
            .ToList();

        return Ok(new PortfolioSummaryDto(s.ValueCrc, s.ValueUsd, s.InvestedCrc, s.PnlCrc, s.PnlUsd, s.PnlPct,
            s.RealizedPnlCrc, s.DayChangeCrc, s.DayChangePct, s.FxRate, holdings));
    }

    /// <summary>Movimientos del usuario, del más reciente al más viejo.</summary>
    [HttpGet("transactions")]
    public Task<IReadOnlyList<Transaction>> Transactions(CancellationToken ct) => portfolio.GetTransactionsAsync(UserId, ct);

    /// <summary>Cantidad en poder de un activo (para el formulario de venta).</summary>
    [HttpGet("quantity/{assetId}")]
    public Task<decimal> Quantity(string assetId, CancellationToken ct) => portfolio.GetQuantityAsync(UserId, assetId, ct);

    /// <summary>
    /// Registra una compra o venta. El id lo genera la app; si llega repetido responde igual.
    /// Si una compra en USD llega sin fxRate (0), el servidor usa el tipo de compra oficial de esa fecha.
    /// 201 · 422 si se vende más de lo que se tiene · 400 si un campo no es válido.
    /// </summary>
    [HttpPost("transactions")]
    [ProducesResponseType<Transaction>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Add(Transaction body, CancellationToken ct)
    {
        var tx = body.Id == Guid.Empty ? body with { Id = Guid.NewGuid() } : body;
        var result = await portfolio.AddTransactionAsync(UserId, tx, ct);
        if (!result.Ok) return Error(result.Error!);

        var saved = (await portfolio.GetTransactionsAsync(UserId, ct)).First(t => t.Id == tx.Id);
        return StatusCode(StatusCodes.Status201Created, saved);
    }
}
