using SkiaSharp;

namespace FinDashFrontEnd.Theme;

/// <summary>
/// Copia la versión clara u oscura de cada color de Colors.xaml a su llave semántica
/// (por ejemplo "PrimaryLight" o "PrimaryDark" -> "PrimaryColor"). Las vistas usan
/// {DynamicResource PrimaryColor}, así que cambian solas cuando cambia el tema.
/// </summary>
public static class ThemeManager
{
    static readonly string[] Keys =
    [
        "Bg", "Surface", "Surface2", "Border", "Text", "TextMuted",
        "Primary", "OnPrimary", "PrimarySoft",
        "Gain", "GainSoft", "Loss", "LossSoft", "Warn", "WarnSoft",
    ];

    public static event EventHandler? ThemeApplied;

    public static void Initialize(Application app)
    {
        Apply(app);
        app.RequestedThemeChanged += (_, _) => Apply(app);
    }

    public static void Apply(Application app)
    {
        var suffix = app.RequestedTheme == AppTheme.Dark ? "Dark" : "Light";
        foreach (var key in Keys)
        {
            if (app.Resources.TryGetValue(key + suffix, out var value))
                app.Resources[key + "Color"] = value;
        }
        ThemeApplied?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>Color semántico del tema activo, por ejemplo Get("PrimaryColor").</summary>
    public static Color Get(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var v) == true && v is Color c ? c : Colors.Gray;

    /// <summary>Mismo color convertido para SkiaSharp (gráficas de LiveCharts).</summary>
    public static SKColor GetSk(string key)
    {
        var c = Get(key);
        return new SKColor((byte)(c.Red * 255), (byte)(c.Green * 255), (byte)(c.Blue * 255), (byte)(c.Alpha * 255));
    }
}
