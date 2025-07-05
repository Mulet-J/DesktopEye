using System;
using CommunityToolkit.Mvvm.Input;
using DesktopEye.Common.Application.ViewModels.Base;
using DesktopEye.Common.Application.Views.ScreenCapture;
using Microsoft.Extensions.DependencyInjection;
using ScreenCaptureViewModel = DesktopEye.Common.Application.ViewModels.ScreenCapture.ScreenCaptureViewModel;

namespace DesktopEye.Common.Application.ViewModels;

public partial class MainViewModel(Bugsnag.IClient bugsnag, IServiceProvider services) : ViewModelBase
{
    [RelayCommand]
    private void CaptureRegion()
    {
        try 
        {
            var fullScreenWindow = new ScreenCaptureWindow
            {
                DataContext = services.GetService<ScreenCaptureViewModel>()
            };
            fullScreenWindow.Show();   
        }
        catch (Exception e)
        {
            // Log the exception using Bugsnag
            bugsnag.Notify(e);
        }
    }
}