namespace FinDashFrontEnd.Models;

/// <summary>Precio actual de un activo. Para el dólar, PriceCrc es el tipo de cambio de compra.</summary>
public sealed record Quote(string AssetId, decimal PriceUsd, decimal PriceCrc, decimal Change24hPct);

/// <summary>Tipo de cambio oficial del BCCR (códigos 317 compra y 318 venta).</summary>
public sealed record ExchangeRate(decimal Buy, decimal Sell, decimal Change24hPct);

/// <summary>Lo que devuelve un refresco: tipo de cambio, precios y si vienen de la caché.</summary>
public sealed record MarketSnapshot(
    ExchangeRate Fx,
    IReadOnlyDictionary<string, Quote> Quotes,
    bool IsOffline,
    DateTimeOffset UpdatedAt);

public enum ChartRange { Day, Week, Month, Year }

public sealed record PricePoint(DateTimeOffset Time, decimal PriceCrc, decimal PriceUsd);
