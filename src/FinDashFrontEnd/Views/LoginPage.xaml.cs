using FinDashFrontEnd.Theme;
using FinDashFrontEnd.ViewModels;

namespace FinDashFrontEnd.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        UpdateEye(vm);
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LoginViewModel.IsPasswordHidden)) UpdateEye(vm);
        };
    }

    void UpdateEye(LoginViewModel vm) =>
        EyeButton.Text = vm.IsPasswordHidden ? Icons.Visibility : Icons.VisibilityOff;
}
