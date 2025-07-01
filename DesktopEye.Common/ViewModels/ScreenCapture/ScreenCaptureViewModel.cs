using System;
using Avalonia;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopEye.Common.Helpers;
using DesktopEye.Common.Services.ScreenCapture;
using DesktopEye.Common.ViewModels.Base;
using DesktopEye.Common.Views.ScreenCapture;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopEye.Common.ViewModels.ScreenCapture;

public partial class ScreenCaptureViewModel : ViewModelBase, IDisposable
{
    private readonly IScreenCaptureService _captureService;
    private readonly IServiceProvider _services;
    private readonly Bugsnag.IClient _bugsnag;
    [ObservableProperty] private Bitmap? _bitmap;
    [ObservableProperty] private Rect? _selection;

    protected ScreenCaptureViewModel(IServiceProvider services, IScreenCaptureService captureService, Bugsnag.IClient bugsnag)
    {
        _services = services;
        _captureService = captureService;
        _bugsnag = bugsnag;
        GetScreenBitmap();
    }

    public void Dispose()
    {
        Bitmap?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void GetScreenBitmap()
    {
        Bitmap = _captureService.CaptureScreen();
    }

    [RelayCommand]
    private void ProcessSelection()
    {
        try
        {
            if (!Selection.HasValue) return;
            if (Bitmap is null) return;
            var cropedBitmap = Bitmap.CropBitmap(Selection.Value.TopLeft, Selection.Value.BottomRight);
            var dataContext = _services.GetService<ScreenCaptureActionsViewModel>();
            if (dataContext == null) throw new Exception();
            dataContext.SetBitmap(cropedBitmap);
            var window = new ScreenCaptureActionsWindow
            {
                DataContext = dataContext
            };

            window.Show();
        }
        catch (Exception e)
        {
            // Log the exception using Bugsnag
            _bugsnag.Notify(e);
        }
        
    }
}