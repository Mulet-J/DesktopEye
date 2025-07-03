using System;
using CommunityToolkit.Mvvm.Input;
using DesktopEye.Common.ViewModels.Base;
using DesktopEye.Common.ViewModels.ScreenCapture;
using DesktopEye.Common.Views.ScreenCapture;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopEye.Common.ViewModels;

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