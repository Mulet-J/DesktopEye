using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using DesktopEye.Common.Classes;
using DesktopEye.Common.Exceptions;
using DesktopEye.Common.Extensions;
using DesktopEye.Common.Services.ApplicationPath;
using DesktopEye.Common.Services.Download;
using Microsoft.Extensions.Logging;
using TesseractOCR;
using TesseractOCR.Enums;
using TesseractOCR.Pix;
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

    private (ScriptName scriptName, float confidence) DetectScriptWithOsd(Image image)
    {
        lock (_lock)
        {
            if (_osdEngine == null)
                throw new Exception($"Tried to run OCR with an unintialized engine {nameof(_osdEngine)}");


            // This sets the minimum characters needed to run the osd. Default is 50
            _osdEngine.SetVariable("min_characters_to_try", 5);

            _osdEngine.DefaultPageSegMode = PageSegMode.OsdOnly;

            using var res = _osdEngine.Process(image);

            try
            {
                res.DetectOrientationAndScript(out _, out _, out var name,
                    out var scriptConfidence);
                _logger.LogInformation("Detected script is {0} with confidence {1}", name, scriptConfidence);
                return (name, scriptConfidence);
            }
            catch
            {
                _logger.LogWarning("Could not detect the text's script type, falling back to latin.");
                return (ScriptName.Latin, 0);
            }
        }
    }

    public async Task<(ScriptName scriptName, float confidence)> DetectScriptWithOsdAsync(Image image,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => DetectScriptWithOsd(image), cancellationToken);
    }

    #endregion


    #region GetText

    /// <summary>
    ///     Does a first pass using OSD to try to detect the possible language, then extract the text using the detected
    ///     script.
    /// </summary>
    /// <param name="bitmap">The source bitmap</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="preprocess"></param>
    /// <returns>The extracted text</returns>
    public async Task<OcrResult> GetTextFromBitmapAsync(Bitmap bitmap,
        CancellationToken cancellationToken = default, bool preprocess = true)
    {
        _logger.LogInformation("Starting two-pass OCR process for bitmap");

        Image? image = null;

        if (preprocess)
        {
            using var mat = bitmap.ToMat();
            using var processedMat = ImagePreprocessor.PreprocessImage(mat);
            image = processedMat.ToTesseractImage();
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            image ??= bitmap.ToTesseractImage();

            var script = await DetectScriptWithOsdAsync(image, cancellationToken);

            var result = await GetTextFromImageUsingScriptNameAsync(image, script.scriptName, cancellationToken);
            _logger.LogInformation("Two-pass OCR completed successfully. Word count: {WordCount}",
                result.Words.Count);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Two-pass OCR operation was cancelled");
            throw;
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
    /// <param name="image">The source image</param>
    /// <param name="scriptName">The ScriptName used as a reference for the target language</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The text extracted from the bitmap</returns>
    public async Task<OcrResult> GetTextFromImageUsingScriptNameAsync(Image image, ScriptName scriptName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting OCR with script name: {ScriptName}", scriptName);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var targetLibLanguage = ScriptNameHelper.GetLanguageForScript(scriptName).FirstOrDefault();
            _logger.LogDebug("Mapped script {ScriptName} to library language: {LibLanguage}", scriptName,
                targetLibLanguage);

            var targetLanguage = LibLanguageToLanguage(targetLibLanguage);
            _logger.LogDebug("Converted library language to target language: {TargetLanguage}", targetLanguage);

            _ = await SetEngineAsync([targetLanguage], cancellationToken);
            _logger.LogInformation("Engine set successfully for language: {TargetLanguage}", targetLanguage);

            var result = await GetTextFromImageKeepEngineAsync(image, cancellationToken);
            _logger.LogInformation("OCR completed successfully using script {ScriptName}. Word count: {WordCount}",
                scriptName, result.Words.Count);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("OCR operation with script name {ScriptName} was cancelled", scriptName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during OCR with script name: {ScriptName}", scriptName);
            throw;
        }
    }

    public async Task<OcrResult> GetTextFromBitmapAsync(Bitmap bitmap, List<Language> languages,
        CancellationToken cancellationToken = default, bool preprocess = true)
    {
        _logger.LogInformation("Starting OCR with {LanguageCount} languages: {Languages}",
            languages.Count, string.Join(", ", languages));

        Image? image = null;

        if (preprocess)
        {
            using var mat = bitmap.ToMat();
            using var processedMat = ImagePreprocessor.PreprocessImage(mat);
            image = processedMat.ToTesseractImage();
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await SetEngineAsync(languages, cancellationToken);

            image ??= bitmap.ToTesseractImage();

            var result = await Task.Run(() => GetTextFromImageKeepEngine(image), cancellationToken);
            _logger.LogInformation("OCR completed successfully with multiple languages. Word count: {WordCount}",
                result.Words.Count);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("OCR operation with languages {Languages} was cancelled",
                string.Join(", ", languages));
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during OCR with languages: {Languages}",
                string.Join(", ", languages));
            throw;
        }
    }

    /// <summary>
    ///     Get text from the bitmap without changing the engine.
    /// </summary>
    /// <param name="image">The source image</param>
    /// <returns>The text extracted from the bitmap</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    private OcrResult GetTextFromImageKeepEngine(Image image)
    {
        lock (_lock)
        {
            _logger.LogDebug("Starting OCR text extraction from bitmap");

            try
            {
                if (image == null)
                {
                    _logger.LogError("Bitmap parameter is null");
                    throw new ArgumentNullException(nameof(image));
                }

                if (_engine == null)
                {
                    _logger.LogError("OCR engine is not initialized. Call SetEngine first.");
                    throw new InvalidOperationException("OCR engine is not initialized.");
                }

                _logger.LogDebug("Bitmap converted to Tesseract image format");

                using var page = _engine.Process(image);

                var words = ParseTsvString(page.TsvText);
                var text = page.Text;
                var res = new OcrResult(words, text);

                _logger.LogInformation("OCR text extraction completed successfully. Extracted {WordCount} words",
                    res.Words.Count);

                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract text from bitmap");
                throw;
            }
        }
    }

    private async Task<OcrResult> GetTextFromImageKeepEngineAsync(Image image,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => GetTextFromImageKeepEngine(image), cancellationToken);
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

    public async Task<bool> SetEngineAsync(List<Language> languages, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => SetEngine(languages), cancellationToken);
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

    private static List<OcrWord> ParseTsvString(string tsvContent)
    {
        var lines = tsvContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        var ocrWords = new List<OcrWord>();

        // Skip header row (first line)
        for (var i = 1; i < lines.Length; i++)
        {
            var word = ParseTsvLine(lines[i]);
            if (word != null)
                ocrWords.Add(word);
        }

        return ocrWords;
    }

    private static OcrWord? ParseTsvLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        var columns = line.Split('\t');

        // Tesseract TSV format has 12 columns:
        // level, page_num, block_num, par_num, line_num, word_num, left, top, width, height, conf, text
        if (columns.Length < 12)
            return null;

        try
        {
            // Parse numeric values
            if (int.TryParse(columns[6], out var left) ||
                int.TryParse(columns[7], out var top) ||
                int.TryParse(columns[8], out var width) ||
                int.TryParse(columns[9], out var height) ||
                float.TryParse(columns[10], out var confidence))
                return null;

            // Get text (last column, may contain spaces if rejoined)
            var text = columns[11];

            // Handle case where text might contain tabs (rejoin remaining columns)
            if (columns.Length > 12) text = string.Join("\t", columns.Skip(11));

            // Skip empty text or very low confidence
            if (string.IsNullOrWhiteSpace(text) || confidence < 0)
                return null;

            return new OcrWord(left, top, width, height, confidence, text.Trim());
        }
        catch (Exception)
        {
            // Skip malformed lines
            return null;
        }
    }

    #endregion
}