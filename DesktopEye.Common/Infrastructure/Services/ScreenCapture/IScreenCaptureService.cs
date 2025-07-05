using Avalonia.Media.Imaging;

namespace DesktopEye.Common.Infrastructure.Services.ScreenCapture;

public interface IScreenCaptureService
{
    public Bitmap CaptureScreen();
}