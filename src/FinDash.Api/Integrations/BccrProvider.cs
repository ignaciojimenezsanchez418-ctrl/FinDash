using System.Globalization;
using System.Xml.Linq;
using FinDash.Core.Interfaces;
using FinDash.Core.Models;
using Microsoft.Extensions.Options;

namespace FinDash.Api.Integrations;

public sealed class BccrOptions
{
    public string BaseUrl { get; set; } = "https://gee.bccr.fi.cr/Indicadores/Suscripciones/WS/wsindicadoreseconomicos.asmx/";
    public string Name { get; set; } = "FinDash";
    public string? Email { get; set; }
    public string? Token { get; set; }
}

/// <summary>
/// Tipo de cambio oficial del servicio web de Indicadores Económicos del BCCR: indicador 317
/// (compra) y 318 (venta). Pide correo y token de suscripción (D13).
///
/// PENDIENTE (US-206): probar contra el servicio real cuando el equipo tenga el token. El
/// formato que se lee está en ParseIndicator y tiene pruebas con una respuesta guardada.
/// </summary>
public sealed class BccrProvider(HttpClient http, IOptions<BccrOptions> options, TimeProvider time) : IExchangeRateProvider
{
    const int BuyIndicator = 317;
    const int SellIndicator = 318;

    /// <summary>Hora de Costa Rica: el BCCR publica por fecha local.</summary>
    static readonly TimeSpan CrOffset = TimeSpan.FromHours(-6);

    public async Task<FxQuote> GetLatestAsync(CancellationToken ct = default)
    {
        // Una semana hacia atrás cubre fines de semana y feriados sin publicación.
        var today = DateOnly.FromDateTime(time.GetUtcNow().ToOffset(CrOffset).DateTime);
        var history = await GetHistoryAsync(today.AddDays(-7), today, ct);
        return history.Count > 0
            ? history[^1]
            : throw new HttpRequestException("El BCCR no devolvió tipo de cambio para los últimos 7 días.");
    }

    public async Task<IReadOnlyList<FxQuote>> GetHistoryAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var buy = await GetIndicatorAsync(BuyIndicator, from, to, ct);
        var sell = await GetIndicatorAsync(SellIndicator, from, to, ct);

        return buy
            .Where(b => sell.ContainsKey(b.Key))
            .OrderBy(b => b.Key)
            .Select(b => new FxQuote(b.Value, sell[b.Key], new DateTimeOffset(b.Key.ToDateTime(TimeOnly.MinValue), CrOffset)))
            .ToList();
    }

    async Task<Dictionary<DateOnly, decimal>> GetIndicatorAsync(int indicator, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var o = options.Value;
        var url = "ObtenerIndicadoresEconomicosXML"
            + $"?Indicador={indicator}"
            + $"&FechaInicio={from:dd'/'MM'/'yyyy}"
            + $"&FechaFinal={to:dd'/'MM'/'yyyy}"
            + $"&Nombre={Uri.EscapeDataString(o.Name)}"
            + "&SubNiveles=N"
            + $"&CorreoElectronico={Uri.EscapeDataString(o.Email ?? "")}"
            + $"&Token={Uri.EscapeDataString(o.Token ?? "")}";

        var xml = await http.GetStringAsync(url, ct);
        try
        {
            return ParseIndicator(xml);
        }
        catch (System.Xml.XmlException ex)
        {
            throw new HttpRequestException("El BCCR devolvió una respuesta que no se pudo leer.", ex);
        }
    }

    /// <summary>
    /// El servicio devuelve &lt;string&gt; con el XML de los datos escapado adentro:
    /// Datos_de_INGC011_CAT_INDICADORECONOMIC / INGC011_CAT_INDICADORECONOMIC con DES_FECHA y NUM_VALOR.
    /// También acepta el XML de datos sin envolver.
    /// </summary>
    internal static Dictionary<DateOnly, decimal> ParseIndicator(string xml)
    {
        var doc = XDocument.Parse(xml);
        if (doc.Root is { } root && root.Name.LocalName == "string" && !root.HasElements)
            doc = XDocument.Parse(root.Value);

        var result = new Dictionary<DateOnly, decimal>();
        foreach (var row in doc.Descendants().Where(e => e.Name.LocalName == "INGC011_CAT_INDICADORECONOMIC"))
        {
            var date = row.Elements().FirstOrDefault(e => e.Name.LocalName == "DES_FECHA")?.Value;
            var value = row.Elements().FirstOrDefault(e => e.Name.LocalName == "NUM_VALOR")?.Value;
            if (date is null || value is null) continue;
            if (!DateTimeOffset.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) continue;
            if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) || v <= 0) continue;
            result[DateOnly.FromDateTime(d.DateTime)] = v;
        }
        return result;
    }
}
