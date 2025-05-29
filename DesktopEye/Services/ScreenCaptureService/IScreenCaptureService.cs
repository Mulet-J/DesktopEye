using Avalonia.Media.Imaging;

namespace DesktopEye.Services.ScreenCaptureService;

public interface IScreenCaptureService
{
    public Bitmap CaptureScreen();
}