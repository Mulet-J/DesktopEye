using System;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace DesktopEye.Helpers;

public static class BitmapHelpers
{
    public static Bitmap CropBitmap(this Bitmap originalBitmap, Point startPoint, Point endPoint)
    {
        // Calculate crop rectangle dimensions
        var x = Math.Min(startPoint.X, endPoint.X);
        var y = Math.Min(startPoint.Y, endPoint.Y);
        var width = Math.Abs(endPoint.X - startPoint.X);
        var height = Math.Abs(endPoint.Y - startPoint.Y);

        // Validate crop bounds
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Crop area must have positive width and height");

        if (x < 0 || y < 0 || x + width > originalBitmap.PixelSize.Width ||
            y + height > originalBitmap.PixelSize.Height)
            throw new ArgumentException("Crop area exceeds bitmap boundaries");

        // Create crop rectangle
        var sourceRect = new PixelRect((int)x, (int)y, (int)width, (int)height);

        // Create new WriteableBitmap for the cropped area
        var croppedBitmap = new WriteableBitmap(
            new PixelSize((int)width, (int)height),
            originalBitmap.Dpi,
            PixelFormat.Bgra8888,
            AlphaFormat.Premul
        );

        using (var context = croppedBitmap.Lock())
        {
            // Copy pixel data from source rectangle to new bitmap
            originalBitmap.CopyPixels(sourceRect, context.Address, context.RowBytes * context.Size.Height,
                context.RowBytes);
        }

        return croppedBitmap;
    }
}