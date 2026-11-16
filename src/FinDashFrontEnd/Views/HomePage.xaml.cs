using FinDashFrontEnd.ViewModels;

namespace FinDashFrontEnd.Views;

public partial class HomePage : ContentPage
{
    readonly HomeViewModel _vm;
    IDispatcherTimer? _timer;

    public HomePage(HomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();

        // Refresco automático mientras la pantalla está visible (RF-03).
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(_vm.RefreshSeconds);
        _timer.Tick += async (_, _) => await _vm.LoadAsync(force: true);
        _timer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
        _timer = null;
    }
}
