using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using DesktopEye.Common.Exceptions;
using DesktopEye.Common.Extensions;
using DesktopEye.Common.Services.ApplicationPath;
using DesktopEye.Common.Services.Download;
using Microsoft.Extensions.Logging;
using TesseractOCR;
using TesseractOCR.Enums;
using Language = DesktopEye.Common.Enums.Language;
using ScriptNameHelper = DesktopEye.Common.Helpers.ScriptNameHelper;

namespace DesktopEye.Common.Services.OCR;

public class TesseractOcrService : IOcrService, IDisposable
{
    private const string ModelsFolderName = "tessdata";

    private const string DownloadUrl =
        "https://raw.githubusercontent.com/tesseract-ocr/tessdata/refs/heads/main/[language].traineddata";

    private readonly IDownloadService _downloadService;
    private readonly object _lock = new();
    private readonly ILogger<TesseractOcrService> _logger;
    private readonly string _modelsFolderPath;
    private readonly IPathService _pathService;

    private Engine? _engine;

    // Only used to detect language and orientation, unable to extract text
    private Engine? _osdEngine;

    public TesseractOcrService(IPathService pathService, IDownloadService downloadService,
        ILogger<TesseractOcrService> logger)
    {
        _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _modelsFolderPath = Path.Combine(pathService.ModelsDirectory, ModelsFolderName);

        _logger.LogInformation("TesseractOcrService initialized with models folder path: {ModelsFolderPath}",
            _modelsFolderPath);
    }

    public void Dispose()
    {
        _logger.LogInformation("Disposing TesseractOcrService");
        _engine?.Dispose();
        _osdEngine?.Dispose();
    }

    public async Task<bool> LoadRequiredAsync(string? modelName = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => LoadRequired(modelName), cancellationToken);
    }

    public bool LoadRequired(string? modelName = null, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            try
            {
                _osdEngine = new Engine(_modelsFolderPath, TesseractOCR.Enums.Language.Osd);
                return true;
            }
            catch
            {
                _logger.LogError("Encountered an error when trying to load osd model");
                return false;
            }
        }
    }

    private async Task<bool> DownloadModelAsync(TesseractOCR.Enums.Language language)
    {
        var stringValue = LanguageHelper.EnumToString(language);
        var modelName = $"{stringValue}.traineddata";
        var modelPath = Path.Combine(_modelsFolderPath, modelName);

        _logger.LogDebug("Checking model availability for language {Language} at path: {ModelPath}", language,
            modelPath);

        if (File.Exists(modelPath))
        {
            _logger.LogDebug("Model for language {Language} already exists", language);
            return true;
        }

        try
        {
            _logger.LogInformation("Downloading model for language {Language}", language);

            // Ensure the models folder exists
            Directory.CreateDirectory(_modelsFolderPath);
            _logger.LogDebug("Created models directory: {_modelsFolderPath}", _modelsFolderPath);

            // Replace [language] placeholder with actual language code
            var downloadUrl = DownloadUrl.Replace("[language]", stringValue.ToLowerInvariant());
            _logger.LogDebug("Download URL for {Language}: {DownloadUrl}", modelName, downloadUrl);

            // Download the model file
            var result = await _downloadService.DownloadFileAsync(downloadUrl, modelPath);

            if (result)
                _logger.LogInformation("Successfully downloaded model for language {Language} to {ModelPath}", language,
                    modelPath);
            else
                _logger.LogError("Failed to download model for language {Language} from {DownloadUrl}", language,
                    downloadUrl);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while downloading model for language {Language}", language);
            return false;
        }
    }

    private async Task<bool> DownloadModelAsync(List<TesseractOCR.Enums.Language> languages)
    {
        if (languages.Count == 0)
        {
            _logger.LogDebug("No languages to download, returning success");
            return true; // Nothing to download, consider it successful
        }

        _logger.LogInformation("Starting download of {LanguageCount} language models: {Languages}",
            languages.Count, string.Join(", ", languages));

        try
        {
            // Create tasks for downloading each model
            var downloadTasks = languages.Select(DownloadModelAsync).ToArray();

            _logger.LogDebug("Created {TaskCount} download tasks", downloadTasks.Length);

            // Wait for all downloads to complete
            var results = await Task.WhenAll(downloadTasks);

            var successCount = results.Count(r => r);
            var failureCount = results.Length - successCount;

            if (failureCount > 0)
                _logger.LogWarning("Download completed with {SuccessCount} successes and {FailureCount} failures",
                    successCount, failureCount);
            else
                _logger.LogInformation("All {LanguageCount} language models downloaded successfully", languages.Count);

            // Return true only if all downloads were successful
            return results.All(result => result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download tessdata models for languages {Languages}",
                string.Join(", ", languages));
            return false;
        }
    }

    #region DetectScript

    public async Task<ScriptName> DetectScriptWithOsdAsync(Bitmap bitmap)
    {
        return await Task.Run(() => DetectScriptWithOsd(bitmap));
    }

    public ScriptName DetectScriptWithOsd(Bitmap bitmap)
    {
        lock (_lock)
        {
            if (_osdEngine == null)
                throw new Exception($"Tried to run OCR with an unintialized engine {nameof(_osdEngine)}");

            var image = bitmap.ToTesseractImage();

            // This sets the minimum characters needed to run the osd. Default is 50
            _osdEngine.SetVariable("min_characters_to_try", 5);

            _osdEngine.DefaultPageSegMode = PageSegMode.OsdOnly;

            using var res = _osdEngine.Process(image);

            try
            {
                res.DetectOrientationAndScript(out _, out _, out var name,
                    out var scriptConfidence);
                return name;
            }
            catch
            {
                _logger.LogWarning("Could not detect the text's script type, falling back to latin.");
                return ScriptName.Latin;
            }
        }
    }

    #endregion


    #region GetText

    /// <summary>
    ///     Does a first pass using OSD to try to detect the possible language, then extract the text using the detected
    ///     script.
    /// </summary>
    /// <param name="bitmap">The source bitmap</param>
    /// <returns></returns>
    public async Task<string> GetTextFromBitmapTwoPassAsync(Bitmap bitmap)
    {
        _logger.LogInformation("Starting two-pass OCR process for bitmap");

        try
        {
            var scriptName = await DetectScriptWithOsdAsync(bitmap);
            _logger.LogInformation("Detected script: {ScriptName}", scriptName);

            var result = await GetTextFromBitmapUsingScriptNameAsync(bitmap, scriptName);
            _logger.LogInformation("Two-pass OCR completed successfully. Text length: {TextLength}",
                result.Length);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during two-pass OCR process");
            throw;
        }
    }

    /// <summary>
    ///     Changes the current engine using the ScriptName as a source and return the bitmap's text
    /// </summary>
    /// <param name="bitmap">The source bitmap</param>
    /// <param name="scriptName">The ScriptName used as a reference for the target language</param>
    /// <returns>The text extracted from the bitmap</returns>
    public async Task<string> GetTextFromBitmapUsingScriptNameAsync(Bitmap bitmap, ScriptName scriptName)
    {
        _logger.LogInformation("Starting OCR with script name: {ScriptName}", scriptName);

        try
        {
            var targetLibLanguage = ScriptNameHelper.GetLanguageForScript(scriptName).FirstOrDefault();
            _logger.LogDebug("Mapped script {ScriptName} to library language: {LibLanguage}", scriptName,
                targetLibLanguage);

            var targetLanguage = LibLanguageToLanguage(targetLibLanguage);
            _logger.LogDebug("Converted library language to target language: {TargetLanguage}", targetLanguage);

            var engineRes = await SetEngineAsync([targetLanguage]);
            _logger.LogInformation("Engine set successfully for language: {TargetLanguage}", targetLanguage);

            var result = await GetTextFromBitmapAsync(bitmap);
            _logger.LogInformation("OCR completed successfully using script {ScriptName}. Text length: {TextLength}",
                scriptName, result.Length);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during OCR with script name: {ScriptName}", scriptName);
            throw;
        }
    }

    public async Task<string> GetTextFromBitmapAsync(Bitmap bitmap, List<Language> languages)
    {
        _logger.LogInformation("Starting OCR with {LanguageCount} languages: {Languages}",
            languages.Count, string.Join(", ", languages));

        await SetEngineAsync(languages);

        try
        {
            var result = await Task.Run(() => GetTextFromBitmap(bitmap));
            _logger.LogInformation("OCR completed successfully with multiple languages. Text length: {TextLength}",
                result?.Length ?? 0);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during OCR with languages: {Languages}",
                string.Join(", ", languages ?? new List<Language>()));
            throw;
        }
    }

    /// <summary>
    ///     Get text from the bitmap without changing the engine.
    /// </summary>
    /// <param name="bitmap">The source bitmap</param>
    /// <returns>The text extracted from the bitmap</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public string GetTextFromBitmap(Bitmap bitmap)
    {
        lock (_lock)
        {
            _logger.LogDebug("Starting OCR text extraction from bitmap");

            try
            {
                if (bitmap == null)
                {
                    _logger.LogError("Bitmap parameter is null");
                    throw new ArgumentNullException(nameof(bitmap));
                }

                if (_engine == null)
                {
                    _logger.LogError("OCR engine is not initialized. Call SetEngine first.");
                    throw new InvalidOperationException("OCR engine is not initialized. Call SetEngine first.");
                }

                var image = bitmap.ToTesseractImage();
                _logger.LogDebug("Bitmap converted to Tesseract image format");

                using var page = _engine.Process(image);
                var text = page.Text;

                _logger.LogInformation("OCR text extraction completed successfully. Extracted {TextLength} characters",
                    text.Length);
                _logger.LogDebug("Extracted text preview: {TextPreview}",
                    string.IsNullOrEmpty(text) ? "[Empty]" : text.Length > 100 ? text[..100] + "..." : text);

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract text from bitmap");
                throw;
            }
        }
    }

    public async Task<string> GetTextFromBitmapAsync(Bitmap bitmap)
    {
        return await Task.Run(() => GetTextFromBitmap(bitmap));
    }

    #endregion

    #region SetEngine

    public bool SetEngine(List<Language> languages)
    {
        _logger.LogInformation("Setting up OCR engine with languages: {Languages}", string.Join(", ", languages));

        lock (_lock)
        {
            try
            {
                if (languages.Count == 0)
                {
                    _logger.LogWarning("No languages provided for OCR engine setup");
                    return false;
                }

                var convertedLanguages = LanguageToLibLanguage(languages);
                _logger.LogDebug("Converted {LanguageCount} languages to library format", convertedLanguages.Count);

                // Check if all required models are available
                // var missingModels = new List<TesseractOCR.Enums.Language>();
                // foreach (var language in convertedLanguages)
                // {
                //     var modelPath = Path.Combine(_modelsFolderPath, $"{language}.traineddata");
                //     if (!File.Exists(modelPath))
                //     {
                //         missingModels.Add(language);
                //         _logger.LogWarning("Missing model file for language {Language} at path: {ModelPath}", language,
                //             modelPath);
                //     }
                // }
                // if (missingModels.Count > 0)
                // {
                //     _logger.LogInformation("Downloading {MissingModelCount} missing language models", missingModels.Count);
                //     var downloadResult = await DownloadModelAsync(missingModels);
                //
                //     if (!downloadResult)
                //     {
                //         _logger.LogError("Failed to download required language models");
                //         return false;
                //     }
                // }

                _engine?.Dispose();
                _engine = new Engine(_modelsFolderPath, convertedLanguages);

                _logger.LogInformation("OCR engine successfully initialized with {LanguageCount} languages",
                    convertedLanguages.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set up OCR engine with languages: {Languages}",
                    string.Join(", ", languages));
                return false;
            }
        }
    }

    public async Task<bool> SetEngineAsync(List<Language> languages)
    {
        return await Task.Run(() => SetEngine(languages));
    }

    #endregion

    #region Helpers

    private static Language LibLanguageToLanguage(TesseractOCR.Enums.Language libLanguage)
    {
        return libLanguage switch
        {
            TesseractOCR.Enums.Language.Danish => Language.Danish,
            TesseractOCR.Enums.Language.German => Language.German,
            TesseractOCR.Enums.Language.English => Language.English,
            TesseractOCR.Enums.Language.French => Language.French,
            TesseractOCR.Enums.Language.Italian => Language.Italian,
            TesseractOCR.Enums.Language.Japanese => Language.Japanese,
            TesseractOCR.Enums.Language.Korean => Language.Korean,
            TesseractOCR.Enums.Language.Dutch => Language.Dutch,
            TesseractOCR.Enums.Language.Norwegian => Language.Norwegian,
            TesseractOCR.Enums.Language.Portuguese => Language.Portuguese,
            TesseractOCR.Enums.Language.Russian => Language.Russian,
            TesseractOCR.Enums.Language.SpanishCastilian => Language.Spanish,
            TesseractOCR.Enums.Language.Swedish => Language.Swedish,
            TesseractOCR.Enums.Language.ChineseSimplified => Language.Chinese,
            _ => throw new LanguageException($"Unsupported library language: {libLanguage}")
        };
    }

    private static TesseractOCR.Enums.Language LanguageToLibLanguage(Language language)
    {
        return language switch
        {
            Language.Danish => TesseractOCR.Enums.Language.Danish,
            Language.German => TesseractOCR.Enums.Language.German,
            Language.English => TesseractOCR.Enums.Language.English,
            Language.French => TesseractOCR.Enums.Language.French,
            Language.Italian => TesseractOCR.Enums.Language.Italian,
            Language.Japanese => TesseractOCR.Enums.Language.Japanese,
            Language.Korean => TesseractOCR.Enums.Language.Korean,
            Language.Dutch => TesseractOCR.Enums.Language.Dutch,
            Language.Norwegian => TesseractOCR.Enums.Language.Norwegian,
            Language.Portuguese => TesseractOCR.Enums.Language.Portuguese,
            Language.Russian => TesseractOCR.Enums.Language.Russian,
            Language.Spanish => TesseractOCR.Enums.Language.SpanishCastilian,
            Language.Swedish => TesseractOCR.Enums.Language.Swedish,
            Language.Chinese => TesseractOCR.Enums.Language.ChineseSimplified,
            _ => throw new LanguageException($"Unsupported language: {language}")
        };
    }

    private static List<TesseractOCR.Enums.Language> LanguageToLibLanguage(List<Language> languages)
    {
        return languages.Select(LanguageToLibLanguage).ToList();
    }

    #endregion
}