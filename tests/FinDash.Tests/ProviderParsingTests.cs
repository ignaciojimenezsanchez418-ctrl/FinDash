using FinDash.Api.Integrations;
using FinDash.Core.Models;

namespace FinDash.Tests;

/// <summary>Mapeo de respuestas de CoinGecko y del BCCR, con respuestas guardadas (sin red).</summary>
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
    public void CoinGecko_TipoDeCambioDeRespaldo()
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
    public void Bccr_RespuestaConXmlEscapadoAdentro()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <string xmlns="http://ws.sdde.bccr.fi.cr">&lt;Datos_de_INGC011_CAT_INDICADORECONOMIC&gt;
              &lt;INGC011_CAT_INDICADORECONOMIC&gt;
                &lt;COD_INDICADORINTERNO&gt;317&lt;/COD_INDICADORINTERNO&gt;
                &lt;DES_FECHA&gt;2026-11-13T00:00:00-06:00&lt;/DES_FECHA&gt;
                &lt;NUM_VALOR&gt;502.95000000&lt;/NUM_VALOR&gt;
              &lt;/INGC011_CAT_INDICADORECONOMIC&gt;
              &lt;INGC011_CAT_INDICADORECONOMIC&gt;
                &lt;COD_INDICADORINTERNO&gt;317&lt;/COD_INDICADORINTERNO&gt;
                &lt;DES_FECHA&gt;2026-11-16T00:00:00-06:00&lt;/DES_FECHA&gt;
                &lt;NUM_VALOR&gt;503.12000000&lt;/NUM_VALOR&gt;
              &lt;/INGC011_CAT_INDICADORECONOMIC&gt;
            &lt;/Datos_de_INGC011_CAT_INDICADORECONOMIC&gt;</string>
            """;

        var values = BccrProvider.ParseIndicator(xml);

        Assert.Equal(2, values.Count);
        Assert.Equal(503.12m, values[new DateOnly(2026, 11, 16)]);
        Assert.Equal(502.95m, values[new DateOnly(2026, 11, 13)]);
    }

    [Fact]
    public void Bccr_XmlSinEnvolver()
    {
        const string xml = """
            <Datos_de_INGC011_CAT_INDICADORECONOMIC>
              <INGC011_CAT_INDICADORECONOMIC>
                <DES_FECHA>2026-11-16T00:00:00-06:00</DES_FECHA>
                <NUM_VALOR>509.48</NUM_VALOR>
              </INGC011_CAT_INDICADORECONOMIC>
            </Datos_de_INGC011_CAT_INDICADORECONOMIC>
            """;

        Assert.Equal(509.48m, BccrProvider.ParseIndicator(xml)[new DateOnly(2026, 11, 16)]);
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
}
