using FinDashFrontEnd.ViewModels;

namespace FinDashFrontEnd.Views;

public partial class AssetDetailPage : ContentPage
{
    readonly AssetDetailViewModel _vm;

    public AssetDetailPage(AssetDetailViewModel vm)
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
