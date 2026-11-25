namespace FinDash.Api;

public static class ProviderMode
{
    public static bool UseFake(IConfiguration config) =>
        string.Equals(config["Providers:Mode"]?.Trim(), "Fake", StringComparison.OrdinalIgnoreCase);
}
