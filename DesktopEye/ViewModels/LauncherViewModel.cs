using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopEye.Services.ScreenCaptureService;
using DesktopEye.Views;

namespace DesktopEye.ViewModels;

public partial class LauncherViewModel(IScreenCaptureService screenCaptureService) : ViewModelBase
{
    [ObservableProperty]
    private bool _isCapturing;
    
    [ObservableProperty]
    private string _statusMessage = "Prêt";

    [RelayCommand]
    private void CaptureRegion()
    {
        IsCapturing = true;
        StatusMessage = "Sélectionnez une zone à capturer...";
        
        if (App.Current.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow != null)
            {
                desktop.MainWindow.Hide();
            }
        }
        
        var captureViewModel = new ScreenCaptureViewModel(screenCaptureService);
        
        captureViewModel.PropertyChanged += (sender, args) => 
        {
            if (args.PropertyName == nameof(ScreenCaptureViewModel.StatusMessage))
            {
                StatusMessage = captureViewModel.StatusMessage;
            }
            
            if (args.PropertyName == nameof(ScreenCaptureViewModel.IsCapturing) && !captureViewModel.IsCapturing)
            {
                IsCapturing = false;
                
                if (App.Current.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime dt)
                {
                    if (dt.MainWindow != null)
                    {
                        dt.MainWindow.Show();
                        dt.MainWindow.Activate();
                    }
                }
            }
        };
        
        var fullScreenWindow = new ScreenCaptureWindow
        {
            DataContext = captureViewModel
        };
        
        fullScreenWindow.Show();
    }
    
    [RelayCommand]
    private void OpenCaptureFolder()
    {
        string picturesFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        string screenshotsFolder = Path.Combine(picturesFolder, "Screenshots");
        
        try
        {
            if (!Directory.Exists(screenshotsFolder))
            {
                Directory.CreateDirectory(screenshotsFolder);
            }
            
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = screenshotsFolder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening folder: {ex.Message}";
        }
    }
}