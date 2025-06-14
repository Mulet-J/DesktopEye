using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using DesktopEye.Common.Enums;
using DesktopEye.Common.Exceptions;
using DesktopEye.Common.Extensions;
using DesktopEye.Common.Services.ApplicationPath;
using DesktopEye.Common.Services.Download;
using Microsoft.Extensions.Logging;
using TesseractOCR;

namespace DesktopEye.Common.Services.OCR;

public class TesseractOcrService : IOcrService, IDisposable
{
    private const string ModelsFolderName = "tessdata";

    private const string DownloadUrl =
        "https://raw.githubusercontent.com/tesseract-ocr/tessdata/refs/heads/main/[language].traineddata";

    private readonly IDownloadService _downloadService;
    private readonly ILogger<TesseractOcrService> _logger;
    private readonly string _modelsFolderPath;
    private Engine? _engine;

    public TesseractOcrService(IPathService pathService, IDownloadService downloadService,
        ILogger<TesseractOcrService> logger)
    {
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
        _engine = null;
    }

    public async Task<string> GetTextFromBitmapAsync(Bitmap bitmap, List<Language> languages)
    {
        _logger.LogDebug("Starting OCR text extraction from bitmap");

        if (!await SetEngineAsync(languages)) throw new Exception();

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

            var picture = bitmap.ToTesseractImage();
            _logger.LogDebug("Bitmap converted to Tesseract image format");

            using var page = _engine.Process(picture);
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

    public async Task<bool> SetEngineAsync(List<Language> languages)
    {
        _logger.LogInformation("Setting up OCR engine with languages: {Languages}", string.Join(", ", languages));

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
            var missingModels = new List<TesseractOCR.Enums.Language>();
            foreach (var language in convertedLanguages)
            {
                var modelPath = Path.Combine(_modelsFolderPath, $"{language}.traineddata");
                if (!File.Exists(modelPath))
                {
                    missingModels.Add(language);
                    _logger.LogWarning("Missing model file for language {Language} at path: {ModelPath}", language,
                        modelPath);
                }
            }

            if (missingModels.Count > 0)
            {
                _logger.LogInformation("Downloading {MissingModelCount} missing language models", missingModels.Count);
                var downloadResult = await DownloadModelAsync(missingModels);

                if (!downloadResult)
                {
                    _logger.LogError("Failed to download required language models");
                    return false;
                }
            }

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

    private async Task<bool> DownloadModelAsync(TesseractOCR.Enums.Language language)
    {
        var stringValue = language.GetStringValue();
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
            _logger.LogDebug("Created models directory: {ModelsFolderPath}", _modelsFolderPath);

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

    private static TesseractOCR.Enums.Language LanguageToLibLanguage(Language language)
    {
        return language switch
        {
            Language.English => TesseractOCR.Enums.Language.English,
            Language.French => TesseractOCR.Enums.Language.French,
            Language.German => TesseractOCR.Enums.Language.German,
            Language.Spanish => TesseractOCR.Enums.Language.SpanishCastilian,
            _ => throw new LanguageException($"Unsupported language: {language}")
        };
    }

    private static List<TesseractOCR.Enums.Language> LanguageToLibLanguage(List<Language> languages)
    {
        return languages.Select(LanguageToLibLanguage).ToList();
    }
}