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
    [ObservableProperty] private Bitmap? _bitmap;
    [ObservableProperty] private Rect? _selection;

    public ScreenCaptureViewModel(IServiceProvider services, IScreenCaptureService captureService)
    {
        _services = services;
        _captureService = captureService;
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
}