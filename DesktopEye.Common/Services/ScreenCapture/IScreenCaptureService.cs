using Avalonia.Media.Imaging;

namespace DesktopEye.Common.Services.ScreenCapture;

public interface IScreenCaptureService
{
    public Bitmap CaptureScreen();
}