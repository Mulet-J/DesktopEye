using System;
using System.IO;
using Avalonia.Media.Imaging;
using OpenCvSharp;
using SkiaSharp;
using TesseractOCR.Pix;

namespace DesktopEye.Extensions;

public static class ImageFormatExtension
{
    #region Bitmap

    public static Image ToTesseractImage(this Bitmap bitmap)
    {
        if (bitmap == null)
            throw new ArgumentNullException(nameof(bitmap));

        // Convert Avalonia Bitmap to PNG byte array
        using (var memoryStream = new MemoryStream())
        {
            bitmap.Save(memoryStream);
            var pngBytes = memoryStream.ToArray();

            // Load the PNG data into Tesseract Image
            return Image.LoadFromMemory(pngBytes);
        }
    }

    #endregion

    #region SKBitmap

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

    public static Image ToTesseractImage(this SKBitmap bitmap)
    {
        using var memStream = new MemoryStream();
        // Encode the bitmap to PNG format
        bitmap.Encode(memStream, SKEncodedImageFormat.Png, 100);

        // Reset the position of the stream to the beginning
        memStream.Position = 0;

        // Create a Pix object from the memory stream
        return Image.LoadFromMemory(memStream);
    }

    public static Mat ToMat(this SKBitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        var height = bitmap.Height;
        var width = bitmap.Width;

        var mat = new Mat(height, width, MatType.CV_8UC4);

        using var pixmap = bitmap.PeekPixels();

        var pixels = pixmap.GetPixels();

        long byteSize = pixmap.RowBytes * height;

        // Seul dieu et Deepseek comprennent ce que ce code fait
        unsafe
        {
            Buffer.MemoryCopy(
                (void*)pixels,
                (void*)mat.Data,
                byteSize,
                byteSize);
        }

        if (bitmap.ColorType == SKColorType.Rgba8888) Cv2.CvtColor(mat, mat, ColorConversionCodes.RGBA2BGRA);

        return mat;
    }

    #endregion
}