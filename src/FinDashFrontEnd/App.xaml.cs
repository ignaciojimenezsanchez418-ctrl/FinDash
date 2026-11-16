using FinDashFrontEnd.Services;
using FinDashFrontEnd.Theme;

namespace FinDashFrontEnd;

public partial class App : Application
{
    public App(ISettingsService settings)
    {
        InitializeComponent();
        UserAppTheme = settings.Theme;
#if DEBUG
        // Atajo de desarrollo: FINDASH_THEME=Light o Dark fuerza el tema.
        if (Enum.TryParse<AppTheme>(Environment.GetEnvironmentVariable("FINDASH_THEME"), out var forced))
            UserAppTheme = forced;
#endif
        ThemeManager.Initialize(this);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell()) { Title = "FinDash CR" };
#if WINDOWS
        // En Windows se abre con tamaño de teléfono para revisar la UI.
        window.Width = 420;
        window.Height = 880;
#endif
        return window;
    }
}
