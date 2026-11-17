namespace FinDash.Api;

/// <summary>
/// Decide si se usan las fuentes reales o las falsas. Providers:Mode:
/// - Auto (por defecto): la fuente real solo si tiene sus credenciales configuradas.
/// - Real: siempre las reales (CoinGecko funciona sin llave, con un límite más bajo).
/// - Fake: siempre las falsas (pruebas, CI y plan B de la demo).
/// </summary>
public static class ProviderMode
{
    public static bool UseRealCoinGecko(IConfiguration config) => Mode(config) switch
    {
        "real" => true,
        "fake" => false,
        _ => !string.IsNullOrEmpty(config["CoinGecko:ApiKey"]),
    };

    public static bool UseRealBccr(IConfiguration config) => Mode(config) switch
    {
        "real" => true,
        "fake" => false,
        _ => !string.IsNullOrEmpty(config["Bccr:Email"]) && !string.IsNullOrEmpty(config["Bccr:Token"]),
    };

    static string Mode(IConfiguration config) => (config["Providers:Mode"] ?? "Auto").Trim().ToLowerInvariant();
}
