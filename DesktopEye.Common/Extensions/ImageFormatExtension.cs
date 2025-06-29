using System;
using System.IO;
using Avalonia.Media.Imaging;
using OpenCvSharp;
using SkiaSharp;
using TesseractOCR.Pix;

namespace DesktopEye.Common.Extensions;

public static class ImageFormatExtension
{
    #region Bitmap

    /// <summary>
    ///     Convert to a tesseract compatible format
    /// </summary>
    /// <param name="bitmap"></param>
    /// <returns></returns>
    public static Image ToTesseractImage(this Bitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        using var stream = new MemoryStream();

        bitmap.Save(stream);
        var pngBytes = stream.ToArray();

        return Image.LoadFromMemory(pngBytes);
    }

    /// <summary>
    ///     Convert to an OpenCV compatible format
    /// </summary>
    /// <param name="bitmap">The Avalonia bitmap to convert</param>
    /// <param name="readMode">The color and depth convertion to apply</param>
    /// <returns>OpenCvSharp Mat containing the image data</returns>
    public static Mat ToMat(this Bitmap bitmap, ImreadModes readMode = ImreadModes.Unchanged)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        using var stream = new MemoryStream();
        // Save bitmap to memory stream as PNG (preserves quality and supports transparency)
        bitmap.Save(stream);

        // Convert memory stream to byte array
        var imageBytes = stream.ToArray();

        // Decode the image data using OpenCV
        var mat = Cv2.ImDecode(imageBytes, readMode);

        return mat;
    }

    /// <summary>
    ///     Convert to a SkiaSharp compatible bitmap
    /// </summary>
    /// <param name="bitmap"></param>
    /// <returns></returns>
    public static SKBitmap ToSkBitmap(this Bitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        using var stream = new MemoryStream();
        bitmap.Save(stream);
        stream.Position = 0;

        return SKBitmap.Decode(stream);
    }

    #endregion

    #region SKBitmap

    /// <summary>
    ///     Convert to an Avalonia compatible bitmap
    /// </summary>
    /// <param name="bitmap"></param>
    /// <returns></returns>
    public static Bitmap ToAvaloniaBitmap(this SKBitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        using var stream = new MemoryStream();
        bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
        stream.Position = 0;
        return new Bitmap(stream);
    }

    /// <summary>
    ///     Convert to a tesseract compatible format
    /// </summary>
    /// <param name="bitmap"></param>
    /// <returns></returns>
    public static Image ToTesseractImage(this SKBitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        using var stream = new MemoryStream();
        bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
        stream.Position = 0;
        return Image.LoadFromMemory(stream);
    }

    /// <summary>
    ///     Convert to an OpenCV compatible format
    /// </summary>
    /// <param name="bitmap"></param>
    /// <returns></returns>
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

    #region Mat

    /// <summary>
    ///     Convert to an Avalonia compatible bitmap
    /// </summary>
    /// <param name="mat"></param>
    /// <returns></returns>
    public static Bitmap ToAvaloniaBitmap(this Mat mat)
    {
        ArgumentNullException.ThrowIfNull(mat);

        // Encode the Mat as PNG bytes
        Cv2.ImEncode(".png", mat, out var imageBytes);

        using var stream = new MemoryStream(imageBytes);
        return new Bitmap(stream);
    }

    /// <summary>
    ///     Convert to a tesseract compatible format
    /// </summary>
    /// <param name="mat"></param>
    /// <returns></returns>
    public static Image ToTesseractImage(this Mat mat)
    {
        ArgumentNullException.ThrowIfNull(mat);

        // Encode the Mat as PNG bytes
        Cv2.ImEncode(".png", mat, out var imageBytes);

        return Image.LoadFromMemory(imageBytes);
    }

    /// <summary>
    ///     Convert to a SkiaSharp compatible bitmap
    /// </summary>
    /// <param name="mat"></param>
    /// <returns></returns>
    public static SKBitmap ToSkBitmap(this Mat mat)
    {
        ArgumentNullException.ThrowIfNull(mat);

        // Encode the Mat as PNG bytes
        Cv2.ImEncode(".png", mat, out var imageBytes);

        using var stream = new MemoryStream(imageBytes);
        return SKBitmap.Decode(stream);
    }

    #endregion
}