using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;
using SkiaSharp;
using Tesseract;

namespace DesktopEye.Helpers;

public static class SkiaHelpers
{
    public static Bitmap ToAvaloniaBitmap(this SKBitmap skBitmap)
    {
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

    public static SKBitmap CropBitmap(this SKBitmap originalBitmap, Point startPoint, Point endPoint)
    {
        var cropRect = new SKRectI((int)startPoint.X, (int)startPoint.Y, (int)endPoint.X, (int)endPoint.Y);
        var croppedBitmap = new SKBitmap(cropRect.Width, cropRect.Height);

        using var canvas = new SKCanvas(croppedBitmap);
        canvas.DrawBitmap(originalBitmap, cropRect, new SKRect(0, 0, cropRect.Width, cropRect.Height));

        return croppedBitmap;
    }

    public static Pix ToTesseractPix(this SKBitmap bitmap)
    {
        using (var data = bitmap.Encode(SKEncodedImageFormat.Png, 100))
        {
            using var stream = File.OpenWrite("temp.png");
            data.SaveTo(stream);
        }

        return Pix.LoadFromFile("temp.png");
    }
}