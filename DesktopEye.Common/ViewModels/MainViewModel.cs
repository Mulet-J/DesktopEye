using System;
using CommunityToolkit.Mvvm.Input;
using DesktopEye.Common.Services.ScreenCaptureService;
using DesktopEye.Common.Views.ScreenCapture;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopEye.Common.ViewModels;

public partial class MainViewModel(IServiceProvider serviceProviderProvider) : ViewModelBase
{
    [RelayCommand]
    private void CaptureRegion()
    {
        var bitmap = serviceProviderProvider.GetService<IScreenCaptureService>()?.CaptureScreen();
        if (bitmap == null) return;
        var fullScreenWindow = new ScreenCaptureWindow
        {
            DataContext = new ScreenCaptureViewModel(bitmap)
        };
        fullScreenWindow.Show();
    }
}