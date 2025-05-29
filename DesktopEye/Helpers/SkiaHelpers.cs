using Avalonia;
using SkiaSharp;

namespace DesktopEye.Helpers;

public static class SkiaHelpers
{
    public static SKBitmap CropBitmap(this SKBitmap originalBitmap, Point startPoint, Point endPoint)
    {
        var cropRect = new SKRectI((int)startPoint.X, (int)startPoint.Y, (int)endPoint.X, (int)endPoint.Y);
        var croppedBitmap = new SKBitmap(cropRect.Width, cropRect.Height);

        using var canvas = new SKCanvas(croppedBitmap);
        canvas.DrawBitmap(originalBitmap, cropRect, new SKRect(0, 0, cropRect.Width, cropRect.Height));

        return croppedBitmap;
    }
}