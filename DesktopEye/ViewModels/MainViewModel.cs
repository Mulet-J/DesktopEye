using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopEye.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private string _greeting = "Welcome to Avalonia!";
}