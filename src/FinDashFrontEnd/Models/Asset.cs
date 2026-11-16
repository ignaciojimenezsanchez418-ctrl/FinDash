namespace FinDashFrontEnd.Models;

public enum AssetKind { Fiat, Crypto }

public enum Currency { CRC, USD }

/// <summary>Un activo que se puede seguir: el dólar o una criptomoneda (Id = id de CoinGecko).</summary>
public sealed record Asset(string Id, string Symbol, string Name, AssetKind Kind)
{
    /// <summary>Id del dólar (tipo de cambio USD/CRC).</summary>
    public const string UsdId = "usd";

    public bool IsFx => Kind == AssetKind.Fiat;

    /// <summary>Nombre corto para títulos: "Dólar" o el símbolo de la cripto.</summary>
    public string ShortName => IsFx ? "Dólar" : Symbol;

    public string DisplayName => IsFx ? "Dólar (USD/CRC)" : $"{Name} ({Symbol})";

    // El Picker muestra ToString().
    public override string ToString() => DisplayName;
}
