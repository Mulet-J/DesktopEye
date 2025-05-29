using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopEye.Services.ScreenCaptureService;
using DesktopEye.Views;
using DesktopEye.Views.ScreenCapture;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopEye.ViewModels;

public partial class MainViewModel(IServiceProvider serviceProviderProvider) : ViewModelBase
{
    [RelayCommand]
    private void CaptureRegion()
    {
        var bitmap = serviceProviderProvider.GetService<IScreenCaptureService>()?.CaptureScreen();
        if (bitmap == null) return;
        var fullScreenWindow = new ScreenCaptureWindow()
        {
            DataContext = new ScreenCaptureViewModel(bitmap)
        };
        fullScreenWindow.Show();
    }
}