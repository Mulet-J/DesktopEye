using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopEye.Common.Classes;
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
    [ObservableProperty] private bool _hasInferredLanguage;
    [ObservableProperty] private bool _hasOcrText;
    [ObservableProperty] private bool _hasTranslatedText;
    [ObservableProperty] private Language? _inferredLanguage;
    [ObservableProperty] private bool _isDetectingLanguage;

    // Nouvelles propriétés pour l'UI Google Translate
    [ObservableProperty] private bool _isExtractingText;
    [ObservableProperty] private bool _isProcessingImage;
    [ObservableProperty] private bool _isTranslating;
    [ObservableProperty] private OcrResult? _ocrText;
    [ObservableProperty] private bool _showInitialMessage = true;
    [ObservableProperty] private bool _showTranslationWaitMessage = true;
    [ObservableProperty] private Language? _targetLanguage;
    [ObservableProperty] private string? _translatedText;

    public ScreenCaptureActionsViewModel(IOcrManager ocrManager, ITextClassifierManager classifierManager,
        ITranslationManager translationManager)
    {
        _ocrManager = ocrManager;
        _classifierManager = classifierManager;
        _translationManager = translationManager;
        _currentOcrType = _ocrManager.CurrentServiceType;
        _currentClassifierType = _classifierManager.CurrentServiceType;
        _currentTranslationType = _translationManager.CurrentServiceType;

        // Langue par défaut
        _targetLanguage = Language.French;
    }

    public void SetBitmap(Bitmap bitmap)
    {
        Bitmap = bitmap;
        // Reset des états et démarrage de l'analyse automatique
        ResetResults();
        _ = Task.Run(StartAutoAnalysis);
    }

    private async Task StartAutoAnalysis()
    {
        IsProcessingImage = true;
        try
        {
            // Petite pause pour l'UX
            await Task.Delay(500);

            ShowInitialMessage = false;
            await ExtractText();

            if (HasOcrText)
            {
                await InferLanguage();

                if (HasInferredLanguage)
                {
                    // Second pass with a specific language to maximize accuracy
                    await ExtractTextWithLanguage();

                    if (TargetLanguage.HasValue)
                    {
                        ShowTranslationWaitMessage = false;
                        await Translate();
                    }
                }
            }
        }
        finally
        {
            IsProcessingImage = false;
        }
    }

    public async Task ExtractText()
    {
        if (Bitmap == null)
            return;
        IsProcessingImage = true;
        IsExtractingText = true;
        try
        {
            OcrText = await _ocrManager.GetTextFromBitmapAsync(Bitmap);
            HasOcrText = !string.IsNullOrWhiteSpace(OcrText.Text);
        }
        finally
        {
            IsProcessingImage = false;
            IsExtractingText = false;
        }
    }

    private async Task ExtractTextWithLanguage()
    {
        // Null checking
        if (InferredLanguage is not { } language)
            return;

        if (Bitmap == null)
            return;

        IsExtractingText = true;
        try
        {
            var listLanguages = new List<Language> { language };
            OcrText = await _ocrManager.GetTextFromBitmapAsync(Bitmap, listLanguages);
        }
        finally
        {
            IsExtractingText = false;
        }
    }

    private async Task InferLanguage()
    {
        if (OcrText == null)
            return;

        IsProcessingImage = true;
        IsDetectingLanguage = true;
        try
        {
            InferredLanguage = await _classifierManager.ClassifyTextAsync(OcrText.Text);
            HasInferredLanguage = InferredLanguage.HasValue;
        }
        finally
        {
            IsProcessingImage = false;
            IsDetectingLanguage = false;
        }
    }

    private async Task Translate()
    {
        if (OcrText == null || InferredLanguage == null || TargetLanguage == null)
            return;

        IsProcessingImage = true;
        IsTranslating = true;
        try
        {
            TranslatedText =
                await _translationManager.TranslateAsync(OcrText.Text, InferredLanguage.Value, TargetLanguage.Value);
            HasTranslatedText = !string.IsNullOrWhiteSpace(TranslatedText);
        }
        finally
        {
            IsProcessingImage = false;
            IsTranslating = false;
        }
    }

    public async Task MagicAnalyze()
    {
        // Séquence complète d'analyse
        await ExtractText();
        if (HasOcrText)
        {
            await InferLanguage();
            if (HasInferredLanguage && TargetLanguage.HasValue) await Translate();
        }
    }

    private void ResetResults()
    {
        OcrText = null;
        InferredLanguage = null;
        TranslatedText = null;
        HasOcrText = false;
        HasInferredLanguage = false;
        HasTranslatedText = false;
        IsExtractingText = false;
        IsDetectingLanguage = false;
        IsTranslating = false;
        ShowInitialMessage = true;
        ShowTranslationWaitMessage = true;
    }

    // Méthode pour mettre à jour les propriétés HasXxx quand les propriétés changent
    partial void OnOcrTextChanged(OcrResult? value)
    {
        HasOcrText = !string.IsNullOrWhiteSpace(value!.Text);
    }

    partial void OnInferredLanguageChanged(Language? value)
    {
        HasInferredLanguage = value.HasValue;
    }

    partial void OnTranslatedTextChanged(string? value)
    {
        HasTranslatedText = !string.IsNullOrWhiteSpace(value);
    }
}