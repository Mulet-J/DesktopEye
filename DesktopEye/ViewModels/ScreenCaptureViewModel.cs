using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopEye.Helpers;
using DesktopEye.Services;

namespace DesktopEye.ViewModels;

public partial class ScreenCaptureViewModel : ViewModelBase
{
    [ObservableProperty] private Bitmap? _bitmap;

    public ScreenCaptureViewModel(IScreenCaptureService screenCaptureService)
    {
        var bitmap = screenCaptureService.CaptureScreen();
        Bitmap = bitmap.ToAvaloniaBitmap();
    }
}