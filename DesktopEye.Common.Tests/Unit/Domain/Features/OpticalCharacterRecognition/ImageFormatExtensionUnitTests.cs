using Avalonia.Media.Imaging;
using DesktopEye.Common.Domain.Features.OpticalCharacterRecognition;
using SkiaSharp;

namespace DesktopEye.Common.Tests.Unit.Domain.Features.OpticalCharacterRecognition;

public class ImageFormatExtensionUnitTests : IDisposable
{
    private readonly List<IDisposable> _disposables = new();

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();
    }

    #region Bitmap Extensions Tests

    [Fact]
    public void ToTesseractImage_NullBitmap_ThrowsArgumentNullException()
    {
        // Arrange
        Bitmap? nullBitmap = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullBitmap!.ToTesseractImage());
    }

    [Fact]
    public void ToMat_NullBitmap_ThrowsArgumentNullException()
    {
        // Arrange
        Bitmap? nullBitmap = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullBitmap!.ToMat());
    }

    [Fact]
    public void ToSkBitmap_NullBitmap_ThrowsArgumentNullException()
    {
        // Arrange
        Bitmap? nullBitmap = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullBitmap!.ToSkBitmap());
    }

    #endregion

    #region SKBitmap Extensions Tests
    
    [Fact]
    public void ToAvaloniaBitmap_NullSkBitmap_ThrowsArgumentNullException()
    {
        // Arrange
        SKBitmap? nullBitmap = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullBitmap!.ToAvaloniaBitmap());
    }

    [Fact]
    public void ToTesseractImage_NullSkBitmap_ThrowsArgumentNullException()
    {
        // Arrange
        SKBitmap? nullBitmap = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullBitmap!.ToTesseractImage());
    }

    [Fact]
    public void ToMat_NullSkBitmap_ThrowsArgumentNullException()
    {
        // Arrange
        SKBitmap? nullBitmap = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullBitmap!.ToMat());
    }

    #endregion

    #region Helper Methods

    private static SKBitmap CreateTestSkBitmap(SKColorType colorType = SKColorType.Bgra8888, int width = 100, int height = 100)
    {
        var bitmap = new SKBitmap(width, height, colorType, SKAlphaType.Premul);
        
        // Fill with a simple pattern
        using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint();
        paint.Color = SKColors.Blue;
        canvas.Clear(SKColors.White);
        canvas.DrawRect(10, 10, width - 20, height - 20, paint);
        
        return bitmap;
    }

    #endregion
}