using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopEye.Common.Enums;
using DesktopEye.Common.Services.OCR;
using DesktopEye.Common.Services.TextClassifier;
using DesktopEye.Common.Services.Translation;
using DesktopEye.Common.ViewModels.Base;

namespace DesktopEye.Common.ViewModels.ScreenCapture;

public partial class ScreenCaptureActionsViewModel : ViewModelBase
{
    private readonly ITextClassifierManager _classifierManager;
    private readonly IOcrManager _ocrManager;
    private readonly ITranslationManager _translationManager;

    [ObservableProperty]
    private IEnumerable<ClassifierType> _availableClassifierTypes = Enum.GetValues<ClassifierType>();

    [ObservableProperty] private IEnumerable<Language> _availableLanguages = Enum.GetValues<Language>();

    [ObservableProperty] private IEnumerable<OcrType> _availableOcrTypes = Enum.GetValues<OcrType>();

    [ObservableProperty]
    private IEnumerable<TranslationType> _availableTranslationTypes = Enum.GetValues<TranslationType>();

    [ObservableProperty] private Bitmap? _bitmap;
    [ObservableProperty] private ClassifierType _currentClassifierType;
    [ObservableProperty] private OcrType _currentOcrType;
    [ObservableProperty] private TranslationType _currentTranslationType;
    [ObservableProperty] private Language? _inferredLanguage;
    [ObservableProperty] private string? _ocrText;
    [ObservableProperty] private Language? _targetLanguage;
    [ObservableProperty] private string? _translatedText;

    public ScreenCaptureActionsViewModel(IOcrManager ocrManager, ITextClassifierManager classifierManager,
        ITranslationManager translationManager)
    {
        _ocrManager = ocrManager;
        _classifierManager = classifierManager;
        _translationManager = translationManager;
        _currentOcrType = _ocrManager.GetCurrentOcrType();
        _currentClassifierType = _classifierManager.GetCurrentClassifierType();
        _currentTranslationType = _translationManager.GetCurrentTranslatorType();
    }
    //TODO use Manager SwitchTo here

    public void SetBitmap(Bitmap bitmap)
    {
        Bitmap = bitmap;
    }

    public async Task ExtractText()
    {
        if (Bitmap == null)
            return;

        OcrText = await _ocrManager.GetTextFromBitmapAsync(Bitmap, [Language.English]);
    }

    public async Task InferLanguage()
    {
        if (OcrText == null)
            return;

        InferredLanguage = await _classifierManager.ClassifyTextAsync(OcrText);
    }

    public async Task Translate()
    {
        if (OcrText == null || InferredLanguage == null || TargetLanguage == null)
            return;

        TranslatedText =
            await _translationManager.TranslateAsync(OcrText, InferredLanguage.Value, TargetLanguage.Value);
    }
}