using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FinDashFrontEnd.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool hasError;

    partial void OnErrorMessageChanged(string? value) => HasError = !string.IsNullOrEmpty(value);

    protected static Task GoToAsync(string route) => Shell.Current.GoToAsync(route);

    [RelayCommand]
    protected virtual Task BackAsync() => GoToAsync("..");
}
