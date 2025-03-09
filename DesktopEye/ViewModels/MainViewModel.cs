using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DesktopEye.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private int _qty;

    [RelayCommand]
    private void Increment()
    {
        Qty++;
    }

    [RelayCommand]
    private void Decrement()
    {
        Qty--;
    }
}