using FinDashFrontEnd.ViewModels;

namespace FinDashFrontEnd.Views;

public partial class TransactionPage : ContentPage
{
    readonly TransactionViewModel _vm;

    public TransactionPage(TransactionViewModel vm)
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
