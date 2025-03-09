using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopEye.Helpers;
using DesktopEye.Services.ScreenCaptureService;

namespace DesktopEye.ViewModels;

public partial class ImageViewModel : ViewModelBase
{
    private readonly IScreenCaptureService _screenCaptureService;
    [ObservableProperty] private Bitmap? _bitmap;

    public ImageViewModel(IScreenCaptureService screenCaptureService)
    {
        _screenCaptureService = screenCaptureService;
        var bitmap = _screenCaptureService.CaptureScreen();
        Bitmap = bitmap.ToAvaloniaBitmap();
    }
}