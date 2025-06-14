using System;
using CommunityToolkit.Mvvm.Input;
using DesktopEye.Common.ViewModels.Base;
using DesktopEye.Common.ViewModels.ScreenCapture;
using DesktopEye.Common.Views.ScreenCapture;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopEye.Common.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IServiceProvider _services;

    public MainViewModel(IServiceProvider services)
    {
        _services = services;
    }

    [RelayCommand]
    private void CaptureRegion()
    {
        var fullScreenWindow = new ScreenCaptureWindow
        {
            DataContext = _services.GetService<ScreenCaptureViewModel>()
        };
        fullScreenWindow.Show();
    }
}