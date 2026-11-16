using FinDashFrontEnd.ViewModels;

namespace FinDashFrontEnd.Views;

public partial class PortfolioPage : ContentPage
{
    readonly PortfolioViewModel _vm;

    public PortfolioPage(PortfolioViewModel vm)
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
