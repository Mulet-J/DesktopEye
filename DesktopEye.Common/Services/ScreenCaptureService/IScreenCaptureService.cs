using Avalonia.Media.Imaging;

namespace DesktopEye.Common.Services.ScreenCaptureService;

public interface IScreenCaptureService
{
    public Bitmap CaptureScreen();
}