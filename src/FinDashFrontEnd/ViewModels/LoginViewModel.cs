using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinDashFrontEnd.Services;

namespace FinDashFrontEnd.ViewModels;

public partial class LoginViewModel(IAuthService auth) : ViewModelBase
{
    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool isPasswordHidden = true;

    public bool IsDemo => auth is Services.Mock.MockAuthService;

    [RelayCommand]
    private void TogglePassword() => IsPasswordHidden = !IsPasswordHidden;

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = Validation.Email(Email) ?? (string.IsNullOrEmpty(Password) ? "Escriba la contraseña." : null);
        if (HasError) return;

        IsBusy = true;
        var result = await auth.LoginAsync(Email, Password);
        IsBusy = false;

        if (!result.Ok)
        {
            ErrorMessage = result.Error;
            return;
        }
        Password = string.Empty;
        await GoToAsync("//main/home");
    }

    [RelayCommand]
    private Task GoToRegisterAsync() => GoToAsync("register");
}

internal static class Validation
{
    public static string? Email(string email)
    {
        var e = email.Trim();
        if (e.Length == 0) return "Escriba el correo.";
        var at = e.IndexOf('@');
        return at > 0 && e.LastIndexOf('.') > at + 1 && !e.EndsWith('.') ? null : "El correo no parece válido.";
    }

    public static string? NewPassword(string password) =>
        password.Length >= 8 ? null : "La contraseña necesita al menos 8 caracteres.";
}
