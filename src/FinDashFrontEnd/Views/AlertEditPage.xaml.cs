using FinDashFrontEnd.ViewModels;

namespace FinDashFrontEnd.Views;

public partial class AlertEditPage : ContentPage
{
    readonly AlertEditViewModel _vm;

    public AlertEditPage(AlertEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
