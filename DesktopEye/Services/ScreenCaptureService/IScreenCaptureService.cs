using SkiaSharp;

namespace DesktopEye.Services.ScreenCaptureService;

public interface IScreenCaptureService
{
    public SKBitmap CaptureScreen();
}