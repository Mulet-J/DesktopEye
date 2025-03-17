using SkiaSharp;

namespace DesktopEye.Services;

public interface IScreenCaptureService
{
        public SKBitmap CaptureScreen();
}