using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopEye.Common.Application.ViewModels.Base;
using DesktopEye.Common.Domain.Features.Dictionary.Helpers;
using DesktopEye.Common.Domain.Features.OpticalCharacterRecognition.Interfaces;
using DesktopEye.Common.Domain.Features.TextClassification.Interfaces;
using DesktopEye.Common.Domain.Features.TextTranslation.Interfaces;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Domain.Models.OpticalCharacterRecognition;
using DesktopEye.Common.Domain.Models.TextClassification;
using DesktopEye.Common.Domain.Models.TextTranslation;
using DesktopEye.Common.Infrastructure.Services.TTS;
using DesktopEye.Common.Infrastructure.Services.Dialog;
using DesktopEye.Common.Infrastructure.Services.Dictionary;
using MsBox.Avalonia;

namespace DesktopEye.Common.Application.ViewModels.ScreenCapture;

public partial class ScreenCaptureActionsViewModel : ViewModelBase
{
    // Services
    private readonly ITextClassifierOrchestrator _classifierOrchestrator;
    private readonly IOcrOrchestrator _ocrOrchestrator;
    private readonly ITranslationOrchestrator _translationOrchestrator;
    private readonly ITtsService _ttsService;
    private readonly Bugsnag.IClient _bugsnag;
    private readonly IWiktionaryService _wiktionaryService;
    private readonly IDialogService _dialogService;

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
    [ObservableProperty] private AudioPlayerViewModel _audioPlayer;
    [ObservableProperty] private string _activeTab = "TextTab";

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
        ITranslationOrchestrator translationOrchestrator, Bugsnag.IClient bugsnag, ITtsService ttsService, AudioPlayerViewModel audioPlayerViewModel,IWiktionaryService wiktionaryService, IDialogService dialogService)
    {
        _ocrOrchestrator = ocrOrchestrator;
        _classifierOrchestrator = classifierOrchestrator;
        _translationOrchestrator = translationOrchestrator;
        _ttsService = ttsService;
        _audioPlayer = audioPlayerViewModel;
        _bugsnag = bugsnag;


        _wiktionaryService = wiktionaryService;
        _dialogService = dialogService;
        _currentOcrType = _ocrOrchestrator.CurrentServiceType;
        _currentClassifierType = _classifierOrchestrator.CurrentServiceType;
        _currentTranslationType = _translationOrchestrator.CurrentServiceType;
        _ocrText = new OcrResult( null, string.Empty, 0);
        // Langue par défaut
        _targetLanguage = Language.French;
        PropertyChanged += OnPropertyChangedHandler;
        // Initialiser la commande
        RelaunchAnalysisCommand = new AsyncRelayCommand(RelaunchAnalysis);
    }
    
    public async Task HandleDefinitionLookupAsync(string selectedText)
    {
        if (string.IsNullOrWhiteSpace(selectedText))
            return;
        try
        {
            var normalizedText = selectedText.ToLower().Trim();
            var response = await _wiktionaryService.GetDefinitionsAsync(normalizedText);
            
            string message;
            if (response == null || !response.Any())
            {
                message = $"Aucune définition trouvée pour \"{normalizedText}\".";
            }
            else
            {
                message = WiktionaryFormatter.FormatDefinitions(response, normalizedText);
            }
            
            await _dialogService.ShowMessageBoxAsync("Wiktionary Definition", message);
        }
        catch (Exception ex)
        {
            _bugsnag.Notify(ex);
            await _dialogService.ShowMessageBoxAsync("Error", 
                "Impossible de récupérer les définitions pour le terme sélectionné.");
        }
    }
    
    
    public void SetBitmap(Bitmap bitmap)
    {
        try
        {
            Console.WriteLine("[DEBUG] Entrée dans SetBitmap");
            if (bitmap == null)
            {
                Console.WriteLine("[DEBUG] Le bitmap est null");
                return;
            }
        
            Bitmap = bitmap;
            Console.WriteLine("[DEBUG] Bitmap défini");
        
            // Reset des états et démarrage de l'analyse automatique
            Console.WriteLine("[DEBUG] Avant ResetResults");
            ResetResults();
            Console.WriteLine("[DEBUG] Après ResetResults");

            // Lancer la tâche
            Task.Run(async () => {
                Console.WriteLine("[DEBUG] Démarrage de StartAutoAnalysis");
                await StartAutoAnalysis();
                Console.WriteLine("[DEBUG] Fin de StartAutoAnalysis");
            });
        }
        catch (Exception e)
        {
            Console.WriteLine($"[DEBUG] Exception dans SetBitmap: {e.Message}");
            Console.WriteLine($"[DEBUG] StackTrace: {e.StackTrace}");
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
    
        Console.WriteLine("[DEBUG] Début ExtractText");
        IsProcessingImage = true;
        IsExtractingText = true;
        try
        {
            Console.WriteLine("[DEBUG] Avant appel à GetTextFromBitmapAsync");
            //OcrText = await _ocrOrchestrator.GetTextFromBitmapAsync(Bitmap);
            OcrText = await GetTextWithTimeoutAsync(Bitmap);
            Console.WriteLine($"[DEBUG] Après GetTextFromBitmapAsync, texte reçu: {OcrText?.Text?.Length ?? 0} caractères");
            HasOcrText = !string.IsNullOrWhiteSpace(OcrText?.Text);
            Console.WriteLine($"[DEBUG] HasOcrText défini à {HasOcrText}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[DEBUG] Exception dans ExtractText: {e.Message}");
            _bugsnag.Notify(e);
        }
        finally
        {
            IsProcessingImage = false;
            IsExtractingText = false;
            Console.WriteLine("[DEBUG] Fin ExtractText");
        }
    }
    
    private async Task<OcrResult> GetTextWithTimeoutAsync(Bitmap bitmap, IEnumerable<Language>? languages = null)
    {
        try
        {
            var ocrTask = _ocrOrchestrator.GetTextFromBitmapAsync(bitmap);
        
            // 20 secondes de timeout
            if (await Task.WhenAny(ocrTask, Task.Delay(20000)) != ocrTask)
            {
                Console.WriteLine("[DEBUG] Timeout dépassé pour l'OCR");
                return new OcrResult(null, "Le traitement OCR a pris trop de temps", 0);
            }
        
            return await ocrTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Exception dans GetTextWithTimeoutAsync: {ex.Message}");
            return new OcrResult(null, string.Empty, 0);
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
        }
        catch (Exception e)
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
        }
        catch (Exception e)
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
                await _translationOrchestrator.TranslateAsync(OcrText.Text, InferredLanguage.Value,
                    TargetLanguage.Value);
            HasTranslatedText = !string.IsNullOrWhiteSpace(TranslatedText);
        }
        catch (Exception e)
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
        //OcrText = null;
        OcrText = new OcrResult(null, string.Empty, 0);
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
        HasOcrText = value !=null && !string.IsNullOrWhiteSpace(value!.Text);
        
        OnPropertyChanged(nameof(DisplayText));
    }

    partial void OnInferredLanguageChanged(Language? value)
    {
        HasInferredLanguage = value.HasValue;
    }

    partial void OnTranslatedTextChanged(string? value)
    {
        HasTranslatedText = !string.IsNullOrWhiteSpace(value);
    }

    private void OnPropertyChangedHandler(object? sender, PropertyChangedEventArgs e)
    {
        // Mettre à jour l'AudioPlayer quand les textes ou la langue changent
        if (e.PropertyName == nameof(OcrText) ||
            e.PropertyName == nameof(TranslatedText) ||
            e.PropertyName == nameof(InferredLanguage) ||
            e.PropertyName == nameof(TargetLanguage) ||
            e.PropertyName == nameof(ActiveTab))
        {
            UpdateAudioPlayerSource();
        }
    }

    private void UpdateAudioPlayerSource()
    {
        try
        {
            if (AudioPlayer == null)
            {
                Console.WriteLine("[DEBUG] AudioPlayer est null dans UpdateAudioPlayerSource");
                return;
            }
            
            // Déterminer le texte et la langue en fonction de l'onglet actif
            string textToPlay = ActiveTab == "TextTab" && HasOcrText
                ? OcrText?.Text ?? string.Empty
                : HasTranslatedText
                    ? TranslatedText ?? string.Empty
                    : string.Empty;

            Language langToUse = ActiveTab == "TextTab"
                ? InferredLanguage ?? Language.English
                : TargetLanguage ?? Language.French;

            // Mettre à jour l'AudioPlayer seulement si les valeurs ont changé
            if (textToPlay != AudioPlayer.Text || langToUse != AudioPlayer.Language)
            {
                AudioPlayer.Text = textToPlay;
                AudioPlayer.Language = langToUse;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Exception dans UpdateAudioPlayerSource: {ex.Message}");
            _bugsnag.Notify(ex);
        }
    }
}