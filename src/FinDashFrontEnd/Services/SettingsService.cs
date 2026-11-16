using FinDashFrontEnd.Models;

namespace FinDashFrontEnd.Services;

/// <summary>Ajustes locales del usuario guardados en Preferences (RF-12).</summary>
public sealed class SettingsService : ISettingsService
{
    public Currency BaseCurrency
    {
        get => (Currency)Preferences.Default.Get(nameof(BaseCurrency), (int)Currency.CRC);
        set => Preferences.Default.Set(nameof(BaseCurrency), (int)value);
    }

    public int RefreshSeconds
    {
        get => Preferences.Default.Get(nameof(RefreshSeconds), 60);
        set => Preferences.Default.Set(nameof(RefreshSeconds), value);
    }

    public AppTheme Theme
    {
        get => (AppTheme)Preferences.Default.Get(nameof(Theme), (int)AppTheme.Unspecified);
        set => Preferences.Default.Set(nameof(Theme), (int)value);
    }

    public bool SimulateOffline { get; set; }

    public bool SimulateNotificationsDenied { get; set; }
}
