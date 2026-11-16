using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinDashFrontEnd.Services;

namespace FinDashFrontEnd.ViewModels;

public partial class RegisterViewModel(IAuthService auth) : ViewModelBase
{
    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string confirmPassword = string.Empty;

    [RelayCommand]
    private async Task RegisterAsync()
    {
        ErrorMessage = Validation.Email(Email)
            ?? Validation.NewPassword(Password)
            ?? (Password != ConfirmPassword ? "Las contraseñas no coinciden." : null);
        if (HasError) return;

        IsBusy = true;
        var result = await auth.RegisterAsync(Email, Password);
        IsBusy = false;

        if (!result.Ok)
        {
            ErrorMessage = result.Error;
            return;
        }
        await GoToAsync("//main/home");
    }
}
