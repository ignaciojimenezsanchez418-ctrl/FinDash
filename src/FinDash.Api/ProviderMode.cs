namespace FinDash.Api;

/// <summary>
/// Decide las fuentes de precios. Providers:Mode:
/// - Real (por defecto): CoinGecko para las cripto, funcione o no con llave (sin llave usa la API
///   pública, con un límite más bajo). Tipo de cambio: el BCCR si tiene correo y token, con
///   CoinGecko de respaldo; si no tiene credenciales, CoinGecko directo.
/// - Fake: precios falsos (pruebas, CI y plan B de la demo).
/// </summary>
public static class ProviderMode
{
    public static bool UseFake(IConfiguration config) =>
        string.Equals(config["Providers:Mode"]?.Trim(), "Fake", StringComparison.OrdinalIgnoreCase);

    public static bool HasBccrCredentials(IConfiguration config) =>
        !string.IsNullOrEmpty(config["Bccr:Email"]) && !string.IsNullOrEmpty(config["Bccr:Token"]);
}
