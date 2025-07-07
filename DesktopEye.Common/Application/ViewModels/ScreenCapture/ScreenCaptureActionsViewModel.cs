using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopEye.Common.Application.ViewModels.Base;
using DesktopEye.Common.Domain.Features.OpticalCharacterRecognition.Interfaces;
using DesktopEye.Common.Domain.Features.TextClassification.Interfaces;
using DesktopEye.Common.Domain.Features.TextTranslation.Interfaces;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Domain.Models.OpticalCharacterRecognition;
using DesktopEye.Common.Domain.Models.TextClassification;
using DesktopEye.Common.Domain.Models.TextTranslation;

namespace DesktopEye.Common.Application.ViewModels.ScreenCapture;

public partial class ScreenCaptureActionsViewModel : ViewModelBase
{
    // Services
    private readonly ITextClassifierOrchestrator _classifierOrchestrator;
    private readonly IOcrOrchestrator _ocrOrchestrator;
    private readonly ITranslationOrchestrator _translationOrchestrator;
    private readonly Bugsnag.IClient _bugsnag;

    // Available options
    [ObservableProperty]
    private IEnumerable<ClassifierType> _availableClassifierTypes = Enum.GetValues<ClassifierType>();

    [ObservableProperty]
    private IEnumerable<TranslationType> _availableTranslationTypes = Enum.GetValues<TranslationType>();

    [ObservableProperty] private IEnumerable<Language> _availableLanguages = Enum.GetValues<Language>();
    [ObservableProperty] private IEnumerable<OcrType> _availableOcrTypes = Enum.GetValues<OcrType>();

    // Data properties
    [ObservableProperty] private Bitmap? _bitmap;
    [ObservableProperty] private ClassifierType _currentClassifierType;
    [ObservableProperty] private OcrType _currentOcrType;
    [ObservableProperty] private TranslationType _currentTranslationType;
    [ObservableProperty] private bool _hasInferredLanguage;
    [ObservableProperty] private bool _hasOcrText;
    [ObservableProperty] private bool _hasTranslatedText;
    [ObservableProperty] private Language? _inferredLanguage;
    [ObservableProperty] private bool _isDetectingLanguage;

    // UI state properties
    [ObservableProperty] private bool _isExtractingText;
    [ObservableProperty] private bool _isProcessingImage;
    [ObservableProperty] private bool _isTranslating;
    [ObservableProperty] private OcrResult? _ocrText;
    [ObservableProperty] private bool _showInitialMessage = true;
    [ObservableProperty] private bool _showTranslationWaitMessage = true;
    [ObservableProperty] private Language? _targetLanguage;
    [ObservableProperty] private string? _translatedText;
    // Dans la classe ScreenCaptureActionsViewModel
    [ObservableProperty] private ICommand? _relaunchAnalysisCommand;

    public ScreenCaptureActionsViewModel(IOcrOrchestrator ocrOrchestrator, ITextClassifierOrchestrator classifierOrchestrator,
        ITranslationOrchestrator translationOrchestrator, Bugsnag.IClient bugsnag)
    {
        _ocrOrchestrator = ocrOrchestrator;
        _classifierOrchestrator = classifierOrchestrator;
        _translationOrchestrator = translationOrchestrator;
        _bugsnag = bugsnag;
        _currentOcrType = _ocrOrchestrator.CurrentServiceType;
        _currentClassifierType = _classifierOrchestrator.CurrentServiceType;
        _currentTranslationType = _translationOrchestrator.CurrentServiceType;
        // Langue par défaut
        _targetLanguage = Language.French;
        // Initialiser la commande
        RelaunchAnalysisCommand = new AsyncRelayCommand(RelaunchAnalysis);
    }

    public void SetBitmap(Bitmap bitmap)
    {
        try
        {
            Bitmap = bitmap;
            // Reset des états et démarrage de l'analyse automatique
            ResetResults();
            _ = Task.Run(StartAutoAnalysis);
        }
        catch (Exception e)
        {
            // Log the exception using Bugsnag
            _bugsnag.Notify(e);
        }
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
        catch (Exception e)
        {
            // Log the exception using Bugsnag
            _bugsnag.Notify(e);
        }
        finally
        {
            IsProcessingImage = false;
        }
    }
    private async Task RelaunchAnalysis()
    {
        IsProcessingImage = true;
        try
        {
            // Petite pause pour l'UX
            await Task.Delay(500);
            ShowInitialMessage = false;
            if (HasOcrText)
            {
                if (HasInferredLanguage)
                {
                    await ExtractTextWithLanguage();
                    if (TargetLanguage.HasValue)
                    {
                        ShowTranslationWaitMessage = false;
                        await Translate();
                    }
                }
            }
        }
        catch (Exception e)
        {
            // Log the exception using Bugsnag
            _bugsnag.Notify(e);
        }
        finally
        {
            IsProcessingImage = false;
        }
    }

    private async Task ExtractText()
    {
        if (Bitmap == null)
            return;
        IsProcessingImage = true;
        IsExtractingText = true;
        try
        {
            OcrText = await _ocrOrchestrator.GetTextFromBitmapAsync(Bitmap);
            HasOcrText = !string.IsNullOrWhiteSpace(OcrText.Text);
        } catch (Exception e)
        {
            // Log the exception using Bugsnag
            _bugsnag.Notify(e);
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
            OcrText = await _ocrOrchestrator.GetTextFromBitmapAsync(Bitmap, listLanguages);
        } catch (Exception e)
        {
            // Log the exception using Bugsnag
            _bugsnag.Notify(e);
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
            InferredLanguage = await _classifierOrchestrator.ClassifyTextAsync(OcrText.Text);
            HasInferredLanguage = InferredLanguage.HasValue;
        } catch (Exception e)
        {
            // Log the exception using Bugsnag
            _bugsnag.Notify(e);
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
                await _translationOrchestrator.TranslateAsync(OcrText.Text, InferredLanguage.Value, TargetLanguage.Value);
            HasTranslatedText = !string.IsNullOrWhiteSpace(TranslatedText);
        } catch (Exception e)
        {
            // Log the exception using Bugsnag
            _bugsnag.Notify(e);
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