using System.Collections.Generic;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DesktopEye.Common.Domain.Features.OpticalCharacterRecognition.Interfaces;
using DesktopEye.Common.Domain.Features.TextClassification.Interfaces;
using DesktopEye.Common.Domain.Features.TextTranslation.Interfaces;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Domain.Models.OpticalCharacterRecognition;
using Moq;

namespace DesktopEye.Common.Application.ViewModels.ScreenCapture.Design;

public class DesignScreenCaptureActionsViewModel : ScreenCaptureActionsViewModel
{
    public DesignScreenCaptureActionsViewModel() : base(
        new Mock<IOcrManager>().Object,
        new Mock<ITextClassifierManager>().Object,
        new Mock<ITranslationManager>().Object,
        new Mock<Bugsnag.IClient>().Object
    )
    {
        var words = new List<OcrWord> { new(0, 0, 0, 0, 1, "Le") };
        OcrText = new OcrResult(words, "Le", 1);
        InferredLanguage = Language.English;
        TranslatedText =
            "Bienvenue sur DesktopEye ! Cet outil OCR puissant peut extraire du texte de n'importe quelle capture d'écran avec une précision remarquable. Que vous travailliez avec des documents, des images ou du contenu web, notre technologie IA avancée garantit une reconnaissance de texte précise dans plusieurs langues.";
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