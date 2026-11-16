using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinDashFrontEnd.Models;
using FinDashFrontEnd.Services;

namespace FinDashFrontEnd.ViewModels;

/// <summary>Moneda base, intervalo de refresco, tema y cierre de sesión (RF-12).</summary>
public partial class SettingsViewModel : ViewModelBase
{
    static readonly int[] RefreshValues = [30, 60, 300];

    readonly ISettingsService _settings;
    readonly IAuthService _auth;

    public SettingsViewModel(ISettingsService settings, IAuthService auth)
    {
        _settings = settings;
        _auth = auth;
        baseCurrencyIndex = settings.BaseCurrency == Currency.CRC ? 0 : 1;
        refreshIndex = Math.Max(0, Array.IndexOf(RefreshValues, settings.RefreshSeconds));
        themeIndex = settings.Theme switch { AppTheme.Light => 1, AppTheme.Dark => 2, _ => 0 };
        simulateOffline = settings.SimulateOffline;
        simulateNotificationsDenied = settings.SimulateNotificationsDenied;
    }

    public IList<string> CurrencyOptions { get; } = ["Colones (₡)", "Dólares ($)"];
    public IList<string> RefreshOptions { get; } = ["30 s", "1 min", "5 min"];
    public IList<string> ThemeOptions { get; } = ["Sistema", "Claro", "Oscuro"];

    public string Email => _auth.Email ?? "—";
    public string VersionText => $"FinDash CR · versión {AppInfo.Current.VersionString}";

#if DEBUG
    public bool IsDebug => true;
#else
    public bool IsDebug => false;
#endif

    [ObservableProperty] private int baseCurrencyIndex;
    [ObservableProperty] private int refreshIndex;
    [ObservableProperty] private int themeIndex;
    [ObservableProperty] private bool simulateOffline;
    [ObservableProperty] private bool simulateNotificationsDenied;

    partial void OnBaseCurrencyIndexChanged(int value) =>
        _settings.BaseCurrency = value == 0 ? Currency.CRC : Currency.USD;

    partial void OnRefreshIndexChanged(int value) =>
        _settings.RefreshSeconds = RefreshValues[Math.Clamp(value, 0, RefreshValues.Length - 1)];

    partial void OnThemeIndexChanged(int value)
    {
        var theme = value switch { 1 => AppTheme.Light, 2 => AppTheme.Dark, _ => AppTheme.Unspecified };
        _settings.Theme = theme;
        if (Application.Current is { } app) app.UserAppTheme = theme;
    }

    partial void OnSimulateOfflineChanged(bool value) => _settings.SimulateOffline = value;

    partial void OnSimulateNotificationsDeniedChanged(bool value) => _settings.SimulateNotificationsDenied = value;

    public void Refresh() => OnPropertyChanged(nameof(Email));

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var confirm = await Shell.Current.DisplayAlertAsync("Cerrar sesión", "Se cerrará la sesión en este teléfono.", "Cerrar sesión", "Cancelar");
        if (!confirm) return;
        await _auth.LogoutAsync();
        await GoToAsync("//login");
    }
}
