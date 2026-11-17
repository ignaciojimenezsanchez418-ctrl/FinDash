using System.Net;
using System.Text.Json;
using FinDash.Core.Interfaces;
using FinDash.Core.Models;

namespace FinDash.Api.Integrations;

/// <summary>
/// Cliente de CoinGecko (plan Demo). La llave va en el encabezado x-cg-demo-api-key y vive
/// solo en la configuración del servidor (D13). Todas las cripto se piden en UNA llamada.
/// Si falla lanza HttpRequestException (contrato §3) o RateLimitedException si se cuidó el límite.
/// </summary>
public sealed class CoinGeckoProvider(HttpClient http, CallBudget budget, TimeProvider time, ILogger<CoinGeckoProvider> log)
    : IMarketDataProvider
{
    const string Api = "CoinGecko";

    public async Task<IReadOnlyList<MarketQuote>> GetQuotesAsync(IReadOnlyCollection<string> assetIds, CancellationToken ct = default)
    {
        if (assetIds.Count == 0) return [];
        var ids = Uri.EscapeDataString(string.Join(',', assetIds));
        var json = await GetAsync($"simple/price?ids={ids}&vs_currencies=usd&include_24hr_change=true&include_last_updated_at=true", ct);
        return ParseSimplePrice(json, time.GetUtcNow());
    }

    public async Task<IReadOnlyList<HistoryPoint>> GetHistoryAsync(string assetId, ChartRange range, CancellationToken ct = default)
    {
        var days = range switch { ChartRange.Day => 1, ChartRange.Week => 7, ChartRange.Month => 30, _ => 365 };
        var json = await GetAsync($"coins/{Uri.EscapeDataString(assetId)}/market_chart?vs_currency=usd&days={days}", ct);
        return ParseMarketChart(json);
    }

    async Task<string> GetAsync(string path, CancellationToken ct)
    {
        budget.Take(Api);
        using var response = await http.GetAsync(path, ct);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var wait = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(60);
            budget.BlockFor(wait);
            log.LogWarning("CoinGecko respondió 429; no se llamará por {Seconds} s", wait.TotalSeconds);
            throw new RateLimitedException("CoinGecko respondió 429.");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    /// <summary>{"bitcoin":{"usd":95205,"usd_24h_change":2.1,"last_updated_at":1731715200}, ...}</summary>
    internal static List<MarketQuote> ParseSimplePrice(string json, DateTimeOffset now)
    {
        using var doc = JsonDocument.Parse(json);
        var result = new List<MarketQuote>();
        foreach (var coin in doc.RootElement.EnumerateObject())
        {
            if (!coin.Value.TryGetProperty("usd", out var usd) || usd.ValueKind != JsonValueKind.Number) continue;
            var change = coin.Value.TryGetProperty("usd_24h_change", out var c) && c.ValueKind == JsonValueKind.Number
                ? ToDecimal(c)
                : 0m;
            var at = coin.Value.TryGetProperty("last_updated_at", out var u) && u.ValueKind == JsonValueKind.Number
                ? DateTimeOffset.FromUnixTimeSeconds(u.GetInt64())
                : now;
            result.Add(new MarketQuote(coin.Name, ToDecimal(usd), Math.Round(change, 4), at));
        }
        return result;
    }

    /// <summary>{"prices":[[1731715200000, 91234.5], ...], ...}</summary>
    internal static List<HistoryPoint> ParseMarketChart(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var points = new List<HistoryPoint>();
        if (!doc.RootElement.TryGetProperty("prices", out var prices)) return points;
        foreach (var p in prices.EnumerateArray())
            points.Add(new HistoryPoint(DateTimeOffset.FromUnixTimeMilliseconds((long)p[0].GetDouble()), ToDecimal(p[1])));
        return points;
    }

    /// <summary>CoinGecko a veces manda notación científica (1.2e-05).</summary>
    static decimal ToDecimal(JsonElement e) => e.TryGetDecimal(out var d) ? d : (decimal)e.GetDouble();
}
