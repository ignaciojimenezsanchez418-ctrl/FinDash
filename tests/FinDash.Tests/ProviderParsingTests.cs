using FinDash.Api;
using FinDash.Api.Integrations;
using FinDash.Core.Models;
using Microsoft.Extensions.Configuration;

namespace FinDash.Tests;

/// <summary>Mapeo de respuestas de CoinGecko, con respuestas guardadas (sin red).</summary>
public class ProviderParsingTests
{
    [Fact]
    public void CoinGecko_PrecioSimple()
    {
        const string json = """
            {
              "bitcoin": { "usd": 95205, "usd_24h_change": 2.1034567, "last_updated_at": 1731715200 },
              "shiba-inu": { "usd": 1.234e-05, "usd_24h_change": -3.5 },
              "sin-precio": { }
            }
            """;
        var now = new DateTimeOffset(2026, 11, 17, 0, 0, 0, TimeSpan.Zero);

        var quotes = CoinGeckoProvider.ParseSimplePrice(json, now).ToDictionary(q => q.AssetId);

        Assert.Equal(2, quotes.Count);
        Assert.Equal(95_205m, quotes["bitcoin"].PriceUsd);
        Assert.Equal(2.1035m, quotes["bitcoin"].Change24hPct);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1731715200), quotes["bitcoin"].At);
        Assert.Equal(0.00001234m, quotes["shiba-inu"].PriceUsd);
        Assert.Equal(now, quotes["shiba-inu"].At);
    }

    [Fact]
    public void CoinGecko_Historial()
    {
        const string json = """{ "prices": [[1731715200000, 91234.5], [1731718800000, 91500]], "market_caps": [] }""";

        var points = CoinGeckoProvider.ParseMarketChart(json);

        Assert.Equal(2, points.Count);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1731715200000), points[0].Time);
        Assert.Equal(91_500m, points[1].PriceUsd);
    }

    [Fact]
    public void CoinGecko_TipoDeCambioUsdCrc()
    {
        const string json = """{ "tether": { "usd": 0.998, "crc": 446.0, "crc_24h_change": 1.0 } }""";
        var now = new DateTimeOffset(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

        var rates = CoinGeckoProvider.ParseUsdCrc(json, now);

        // 446 ÷ 0.998 = 446.89; ayer = 446.89 ÷ 1.01 = 442.47
        Assert.Equal(2, rates.Count);
        Assert.Equal(new FxQuote(446.89m, 446.89m, now), rates[1]);
        Assert.Equal(442.47m, rates[0].Buy);
        Assert.Equal(now.AddDays(-1), rates[0].At);
    }

    [Fact]
    public void CoinGecko_TipoDeCambioSinColones_LanzaHttpRequestException()
    {
        Assert.Throws<HttpRequestException>(() =>
            CoinGeckoProvider.ParseUsdCrc("""{ "tether": { "usd": 1.0 } }""", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CallBudget_NoPasaDelMaximoPorMinuto()
    {
        var budget = new CallBudget(2, TimeProvider.System);
        budget.Take("X");
        budget.Take("X");
        Assert.Throws<RateLimitedException>(() => budget.Take("X"));
        Assert.Equal(2, budget.CallsInLastMinute);
    }

    [Fact]
    public void CallBudget_Respeta429()
    {
        var budget = new CallBudget(30, TimeProvider.System);
        budget.BlockFor(TimeSpan.FromMinutes(1));
        Assert.Throws<RateLimitedException>(() => budget.Take("X"));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("Real", false)]
    [InlineData("Auto", false)]
    [InlineData("fake", true)]
    public void ProviderMode_SoloUsaPreciosFalsosSiSePidenAPropósito(string? mode, bool fake)
    {
        var values = new Dictionary<string, string?>();
        if (mode is not null) values["Providers:Mode"] = mode;
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        Assert.Equal(fake, ProviderMode.UseFake(config));
    }
}
