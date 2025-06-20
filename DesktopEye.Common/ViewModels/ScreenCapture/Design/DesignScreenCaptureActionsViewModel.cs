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
        OcrText = "Welcome to DesktopEye! This powerful OCR tool can extract text from any screenshot with remarkable accuracy. Whether you're working with documents, images, or web content, our advanced AI technology ensures precise text recognition across multiple languages.";
        InferredLanguage = Language.English;
        TranslatedText = "Bienvenue sur DesktopEye ! Cet outil OCR puissant peut extraire du texte de n'importe quelle capture d'écran avec une précision remarquable. Que vous travailliez avec des documents, des images ou du contenu web, notre technologie IA avancée garantit une reconnaissance de texte précise dans plusieurs langues.";
        TargetLanguage = Language.French;
        
        HasOcrText = true;
        HasInferredLanguage = true;
        HasTranslatedText = true;
        IsExtractingText = false;
        IsDetectingLanguage = false;
        IsTranslating = false;
        ShowInitialMessage = false;
        ShowTranslationWaitMessage = false;

        // Image de démonstration
        Bitmap = CreateMockBitmap();
    }

    private Bitmap CreateMockBitmap()
    {
        var writeableBitmap = new WriteableBitmap(
            new PixelSize(500, 350),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        return writeableBitmap;
    }
}