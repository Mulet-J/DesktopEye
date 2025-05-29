using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopEye.Extensions;
using DesktopEye.Services.ScreenCaptureService;

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