using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DesktopEye.Common.Enums;
using DesktopEye.Common.Services.OCR;
using DesktopEye.Common.Services.TextClassifier;
using DesktopEye.Common.Services.Translation;
using Moq;

namespace DesktopEye.Common.ViewModels.ScreenCapture.Design;

public class DesignScreenCaptureActionsViewModel : ScreenCaptureActionsViewModel
{
    public DesignScreenCaptureActionsViewModel() : base(
        new Mock<IOcrManager>().Object,
        new Mock<ITextClassifierManager>().Object,
        new Mock<ITranslationManager>().Object
    )
    {
        // Set mock data for design-time preview
        OcrText = "This is sample extracted text from an image that would be processed by the OCR engine.";
        InferredLanguage = Language.English;
        TranslatedText = "Este es un texto de ejemplo extraído de una imagen que sería procesado por el motor OCR.";
        TargetLanguage = Language.Spanish;

        // You can also create a mock bitmap if needed
        Bitmap = CreateMockBitmap(); // Implement this method if you need a sample image
    }

    // Optional: Create a mock bitmap for design-time
    private Bitmap CreateMockBitmap()
    {
        // Create a simple colored bitmap for preview
        var writeableBitmap = new WriteableBitmap(
            new PixelSize(200, 100),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        return writeableBitmap;
    }
}