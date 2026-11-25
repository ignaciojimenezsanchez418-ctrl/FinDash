using FinDash.Api.Data;
using FinDash.Api.Integrations;
using FinDash.Api.Services;
using FinDash.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinDash.Api.Controllers;

/// <summary>Forma de ExchangeRate de la app.</summary>
public sealed record ExchangeRateDto(decimal Buy, decimal Sell, decimal Change24hPct);

/// <summary>Forma de Quote de la app.</summary>
public sealed record QuoteDto(string AssetId, decimal PriceUsd, decimal PriceCrc, decimal Change24hPct);

/// <summary>
/// Forma de MarketSnapshot de la app. IsOffline siempre es false (contrato §2); IsStale dice
/// si el último refresco del servidor falló y son datos de la caché.
/// </summary>
public sealed record MarketSnapshotDto(
    ExchangeRateDto Fx,
    IReadOnlyDictionary<string, QuoteDto> Quotes,
    bool IsOffline,
    DateTimeOffset UpdatedAt,
    bool IsStale);

/// <summary>Catálogo de activos (RF-02).</summary>
[Route("api/assets")]
public sealed class AssetsController(FinDashDbContext db) : ApiControllerBase
{
    /// <summary>Todos los activos. La app los guarda al iniciar.</summary>
    [HttpGet]
    public async Task<IReadOnlyList<Asset>> GetAll(CancellationToken ct) =>
        (await db.Assets.AsNoTracking().ToListAsync(ct))
            .OrderBy(a => a.Kind).ThenBy(a => a.Name)
            .Select(a => a.ToModel())
            .ToList();

    /// <summary>Busca por nombre, símbolo o id en la tabla Assets, sin llamar a CoinGecko (US-204).</summary>
    [HttpGet("search")]
    public async Task<IReadOnlyList<Asset>> Search([FromQuery] string? q, CancellationToken ct)
    {
        var query = (q ?? "").Trim();
        var all = await GetAll(ct);
        return query.Length == 0
            ? all
            : all.Where(a => a.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                          || a.Symbol.Contains(query, StringComparison.OrdinalIgnoreCase)
                          || a.Id.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}

/// <summary>Precios y gráficas (RF-03, RF-04, RF-09).</summary>
[Route("api/market")]
public sealed class MarketController(QuoteStore store, HistoryService history, FinDashDbContext db) : ApiControllerBase
{
    /// <summary>
    /// Precios y tipo de cambio de la caché del servidor. No llama a CoinGecko: los refresca
    /// PriceService (RNF-02). 503 mientras el servidor no tenga el primer tipo de cambio.
    /// </summary>
    [HttpGet("snapshot")]
    [ProducesResponseType<MarketSnapshotDto>(StatusCodes.Status200OK)]
    public IActionResult Snapshot()
    {
        var s = store.Current;
        if (s.Fx is null) return Error("El servidor todavía no tiene precios. Intente de nuevo en un minuto.", 503);

        var quotes = s.AllQuotes().ToDictionary(q => q.AssetId, q => new QuoteDto(q.AssetId, q.PriceUsd, q.PriceCrc, q.Change24hPct));
        return Ok(new MarketSnapshotDto(
            new ExchangeRateDto(s.Fx.Buy, s.Fx.Sell, s.Fx.Change24hPct),
            quotes,
            IsOffline: false,
            UpdatedAt: s.UpdatedAt ?? s.Fx.At,
            IsStale: s.LastRefreshFailed));
    }

    /// <summary>Historial para la gráfica. range = Day, Week, Month o Year.</summary>
    [HttpGet("history/{assetId}")]
    [ProducesResponseType<IReadOnlyList<PricePointDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> History(string assetId, [FromQuery] ChartRange range = ChartRange.Day, CancellationToken ct = default)
    {
        if (!await db.Assets.AnyAsync(a => a.Id == assetId, ct)) return Error(ServiceErrors.AssetNotFound);
        try
        {
            var points = await history.GetAsync(assetId, range, ct);
            return points is null
                ? Error("Todavía no hay tipo de cambio para calcular la gráfica.", 503)
                : Ok(points);
        }
        catch (Exception ex) when (ex is HttpRequestException or RateLimitedException)
        {
            return Error("La fuente de precios no respondió. Intente de nuevo en un minuto.", 503);
        }
    }
}

/// <summary>Activos que sigue el usuario.</summary>
[Route("api/watchlist")]
public sealed class WatchlistController(FinDashDbContext db, TimeProvider time) : ApiControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<string>> Get(CancellationToken ct)
    {
        var userId = UserId;
        var items = await db.WatchlistItems.AsNoTracking().Where(w => w.UserId == userId).ToListAsync(ct);
        return items.OrderBy(w => w.AddedAt).Select(w => w.AssetId).ToList();
    }

    [HttpPut("{assetId}")]
    public async Task<IActionResult> Add(string assetId, CancellationToken ct)
    {
        var userId = UserId;
        if (!await db.Assets.AnyAsync(a => a.Id == assetId, ct)) return Error(ServiceErrors.AssetNotFound);
        if (!await db.WatchlistItems.AnyAsync(w => w.UserId == userId && w.AssetId == assetId, ct))
        {
            db.WatchlistItems.Add(new WatchlistItem { UserId = userId, AssetId = assetId, AddedAt = time.GetUtcNow() });
            await db.SaveChangesAsync(ct);
        }
        return NoContent();
    }

    [HttpDelete("{assetId}")]
    public async Task<IActionResult> Remove(string assetId, CancellationToken ct)
    {
        var userId = UserId;
        await db.WatchlistItems.Where(w => w.UserId == userId && w.AssetId == assetId).ExecuteDeleteAsync(ct);
        return NoContent();
    }
}
