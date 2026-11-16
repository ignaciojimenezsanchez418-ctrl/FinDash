using FinDashFrontEnd.ViewModels;

namespace FinDashFrontEnd.Views;

public partial class SearchAssetPage : ContentPage
{
    readonly SearchAssetViewModel _vm;

    public SearchAssetPage(SearchAssetViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
        QueryEntry.Focus();
    }
}
