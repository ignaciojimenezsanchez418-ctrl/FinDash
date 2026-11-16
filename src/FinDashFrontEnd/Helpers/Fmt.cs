using System.Globalization;
using FinDashFrontEnd.Models;

namespace FinDashFrontEnd.Helpers;

/// <summary>
/// Formato de Costa Rica: ₡1 234 567,89 · +2,3 % · 14 nov 2026.
/// Se arma a mano (en vez de CultureInfo "es-CR") para que se vea igual en Android y Windows.
/// </summary>
public static class Fmt
{
    public static readonly NumberFormatInfo Nfi = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = " ",
        NumberGroupSizes = [3],
    };

    const string Minus = "−";

    static readonly string[] Months = ["ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "set", "oct", "nov", "dic"];

    static string Num(decimal v, int decimals) => Math.Abs(v).ToString("N" + decimals, Nfi);

    static string Sign(decimal v, bool signed) => v < 0 ? Minus : signed ? "+" : "";

    static string Symbol(Currency c) => c == Currency.CRC ? "₡" : "$";

    /// <summary>Decimales según el tamaño: 0 desde 10 000, 2 desde 1, 4 debajo de 1.</summary>
    static int AutoDecimals(decimal v)
    {
        var a = Math.Abs(v);
        return a >= 10_000 ? 0 : a >= 1 || a == 0 ? 2 : 4;
    }

    /// <summary>Monto en una moneda: ₡4 812 350 o $9 446,12.</summary>
    public static string Amount(decimal v, Currency c, bool signed = false, int? decimals = null)
        => Sign(v, signed) + Symbol(c) + Num(v, decimals ?? AutoDecimals(v));

    /// <summary>Monto guardado en colones, mostrado en la moneda base del usuario.</summary>
    public static string Money(decimal crc, Currency baseCurrency, decimal fx, bool signed = false, int? decimals = null)
        => baseCurrency == Currency.CRC || fx == 0
            ? Amount(crc, Currency.CRC, signed, decimals)
            : Amount(crc / fx, Currency.USD, signed, decimals);

    /// <summary>Precio unitario: siempre con decimales cuando es menor a 10 000.</summary>
    public static string Price(decimal v, Currency c) => Amount(v, c);

    public static string Pct(decimal pct) => Sign(pct, true) + Num(pct, 1) + " %";

    public static string Arrow(decimal v) => v >= 0 ? "▲" : "▼";

    /// <summary>"▲ +2,1 %"</summary>
    public static string Change(decimal pct) => $"{Arrow(pct)} {Pct(pct)}";

    /// <summary>"0,0215 BTC" (hasta 8 decimales) o "$3 200,00" para dólares.</summary>
    public static string Qty(decimal q, Asset asset)
        => asset.IsFx ? Amount(q, Currency.USD, decimals: 2) : $"{q.ToString("#,0.########", Nfi)} {asset.Symbol}";

    /// <summary>Número para prellenar un campo: sin separador de miles, coma decimal.</summary>
    public static string Input(decimal v)
    {
        var decimals = Math.Abs(v) >= 1 ? 2 : 6;
        return Math.Round(v, decimals).ToString("0." + new string('#', decimals), Nfi);
    }

    public static string Date(DateTime d) => $"{d.Day} {Months[d.Month - 1]} {d.Year}";

    public static string Ago(DateTimeOffset t)
    {
        var span = DateTimeOffset.Now - t;
        if (span.TotalMinutes < 1) return "hace un momento";
        if (span.TotalMinutes < 60) return $"hace {(int)span.TotalMinutes} min";
        if (span.TotalHours < 24) return $"hace {(int)span.TotalHours} h";
        var days = (int)span.TotalDays;
        return days == 1 ? "ayer" : $"hace {days} días";
    }

    /// <summary>Acepta "1234,5", "1 234,5" o "1234.5".</summary>
    public static bool TryParse(string? text, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var s = text.Replace(" ", "").Replace(" ", "").Replace("₡", "").Replace("$", "");
        var lastComma = s.LastIndexOf(',');
        var lastDot = s.LastIndexOf('.');
        if (lastComma >= 0 && lastDot >= 0)
        {
            // El último separador es el decimal; el otro es de miles.
            s = lastComma > lastDot ? s.Replace(".", "").Replace(',', '.') : s.Replace(",", "");
        }
        else
        {
            s = s.Replace(',', '.');
        }
        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
