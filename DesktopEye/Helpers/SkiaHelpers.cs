using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace DesktopEye.Helpers;

public static class SkiaHelpers
{
    public static Bitmap ToAvaloniaBitmap(this SKBitmap skBitmap)
    {
        // Convert SKBitmap to Avalonia Bitmap
        var imageInfo = new SKImageInfo(skBitmap.Width, skBitmap.Height);
        using (var skImage = SKImage.FromPixels(imageInfo, skBitmap.GetPixels()))
        {
            var data = skImage.Encode(SKEncodedImageFormat.Png, 100).ToArray();
            using (var stream = new MemoryStream(data))
            {
                return new Bitmap(stream);
            }
        }
    }

    public static SKBitmap ResizeBitmap(SKBitmap bitmap, Point startPoint, Point endPoint)
    {
        SKBitmap croppedBitmap = new();
        var rect = new SKRectI((int)startPoint.X, (int)startPoint.Y, (int)endPoint.X, (int)endPoint.Y);
        bitmap.ExtractSubset(croppedBitmap, rect);
        return bitmap;
    }
}