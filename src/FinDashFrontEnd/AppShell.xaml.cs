using FinDashFrontEnd.Views;

namespace FinDashFrontEnd;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Pantallas que se abren encima de la pestaña actual.
        Routing.RegisterRoute("register", typeof(RegisterPage));
        Routing.RegisterRoute("asset", typeof(AssetDetailPage));       // ?id=bitcoin
        Routing.RegisterRoute("search", typeof(SearchAssetPage));      // modal
        Routing.RegisterRoute("transaction", typeof(TransactionPage)); // modal, ?assetId=
        Routing.RegisterRoute("alertedit", typeof(AlertEditPage));     // modal, ?alertId= o ?assetId=

#if DEBUG
        // Atajo de desarrollo: FINDASH_START_ROUTE=//main/home abre esa pantalla al iniciar.
        var startRoute = Environment.GetEnvironmentVariable("FINDASH_START_ROUTE");
        if (!string.IsNullOrEmpty(startRoute))
            Loaded += async (_, _) => await GoToAsync(startRoute);
#endif
    }
}
