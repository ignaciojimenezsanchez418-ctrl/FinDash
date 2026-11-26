using FinDashFrontEnd.Services;
using FinDashFrontEnd.Services.Api;
using FinDashFrontEnd.Services.Mock;
using FinDashFrontEnd.ViewModels;
using FinDashFrontEnd.Views;
using LiveChartsCore.SkiaSharpView.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace FinDashFrontEnd;

public static class MauiProgram
{
    // API local (docs/backend.md). El emulador de Android ve la PC en 10.0.2.2.
    // shortcut: dirección fija de desarrollo, cambiarla por la de la nube al publicar (US-604).
#if ANDROID
    const string ApiBaseUrl = "http://10.0.2.2:5182/";
#else
    const string ApiBaseUrl = "http://localhost:5182/";
#endif

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .UseLiveCharts()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Inter-Regular.ttf", "InterRegular");
                fonts.AddFont("Inter-Medium.ttf", "InterMedium");
                fonts.AddFont("Inter-SemiBold.ttf", "InterSemiBold");
                fonts.AddFont("Inter-Bold.ttf", "InterBold");
                fonts.AddFont("RobotoMono-Medium.ttf", "MonoMedium");
                fonts.AddFont("RobotoMono-SemiBold.ttf", "MonoSemiBold");
                fonts.AddFont("MaterialIconsRound-Regular.otf", "Icons");
            });

        RemoveNativeInputChrome();

        // Servicios. Auth ya usa la API; los demás siguen siendo Mock* hasta conectarlos (US-108).
        var services = builder.Services;
        services.AddSingleton(new HttpClient { BaseAddress = new Uri(ApiBaseUrl), Timeout = TimeSpan.FromSeconds(15) });
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<MockBackend>();
        services.AddSingleton<IAuthService, ApiAuthService>();
        services.AddSingleton<IMarketService, MockMarketService>();
        services.AddSingleton<IPortfolioService, MockPortfolioService>();
        services.AddSingleton<IAlertService, MockAlertService>();

        // Pantallas y ViewModels.
        services.AddTransient<LoginPage>().AddTransient<LoginViewModel>();
        services.AddTransient<RegisterPage>().AddTransient<RegisterViewModel>();
        services.AddTransient<HomePage>().AddTransient<HomeViewModel>();
        services.AddTransient<SearchAssetPage>().AddTransient<SearchAssetViewModel>();
        services.AddTransient<AssetDetailPage>().AddTransient<AssetDetailViewModel>();
        services.AddTransient<PortfolioPage>().AddTransient<PortfolioViewModel>();
        services.AddTransient<TransactionPage>().AddTransient<TransactionViewModel>();
        services.AddTransient<AlertsPage>().AddTransient<AlertsViewModel>();
        services.AddTransient<AlertEditPage>().AddTransient<AlertEditViewModel>();
        services.AddTransient<SettingsPage>().AddTransient<SettingsViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    /// <summary>
    /// Quita el subrayado de Android y el borde de Windows a los campos: el borde lo pone
    /// el estilo FieldBox, igual en las dos plataformas.
    /// </summary>
    static void RemoveNativeInputChrome()
    {
#if ANDROID
        var transparent = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
        EntryHandler.Mapper.AppendToMapping("NoChrome", (h, _) => h.PlatformView.BackgroundTintList = transparent);
        PickerHandler.Mapper.AppendToMapping("NoChrome", (h, _) => h.PlatformView.BackgroundTintList = transparent);
        DatePickerHandler.Mapper.AppendToMapping("NoChrome", (h, _) => h.PlatformView.BackgroundTintList = transparent);
#elif WINDOWS
        var none = new Microsoft.UI.Xaml.Thickness(0);
        EntryHandler.Mapper.AppendToMapping("NoChrome", (h, _) => h.PlatformView.BorderThickness = none);
        PickerHandler.Mapper.AppendToMapping("NoChrome", (h, _) => h.PlatformView.BorderThickness = none);
        DatePickerHandler.Mapper.AppendToMapping("NoChrome", (h, _) => h.PlatformView.BorderThickness = none);
#endif
    }
}
