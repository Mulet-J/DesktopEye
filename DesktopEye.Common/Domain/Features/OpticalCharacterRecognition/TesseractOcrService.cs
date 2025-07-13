using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using DesktopEye.Common.Domain.Features.OpticalCharacterRecognition.Interfaces;
using DesktopEye.Common.Domain.Models.OpticalCharacterRecognition;
using DesktopEye.Common.Infrastructure.Exceptions;
using DesktopEye.Common.Infrastructure.Services.ApplicationPath;
using Microsoft.Extensions.Logging;
using TesseractOCR;
using TesseractOCR.Enums;
using TesseractOCR.Pix;
using Language = DesktopEye.Common.Domain.Models.Language;
using ScriptNameHelper = DesktopEye.Common.Domain.Features.OpticalCharacterRecognition.Helpers.ScriptNameHelper;

namespace DesktopEye.Common.Domain.Features.OpticalCharacterRecognition;

public class TesseractOcrService : IOcrService, IDisposable
{
    private readonly object _lock = new();
    private readonly ILogger<TesseractOcrService> _logger;
    private readonly IPathService _pathService;

    private Engine? _engine;

    // Only used to detect language and orientation, unable to extract text
    private Engine? _osdEngine;

    public TesseractOcrService(IPathService pathService,
        ILogger<TesseractOcrService> logger)
    {
        _pathService = pathService;
        _logger = logger;
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
                _osdEngine = new Engine(Path.Combine(_pathService.ModelsDirectory, "tesseract"),
                    TesseractOCR.Enums.Language.Osd);
                return true;
            }
            catch
            {
                _logger.LogError("Encountered an error when trying to load osd model");
                return false;
            }
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
                // TODO find why the osd randomly returns a completely random scriptname with 0 confidence
                res.DetectOrientationAndScript(out _, out _, out var name,
                    out var scriptConfidence);
                _logger.LogInformation("Detected script is {0} with confidence {1}", name, scriptConfidence);

                if (scriptConfidence < 1)
                {
                    _logger.LogWarning("Confidence too low, falling back to latin");
                    return (ScriptName.Latin, 0);
                }

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
                var res = new OcrResult(words, text, page.MeanConfidence);

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
                _engine = new Engine(Path.Combine(_pathService.ModelsDirectory, "tesseract"), convertedLanguages);

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

    private async Task<bool> SetEngineAsync(List<Language> languages, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => SetEngine(languages), cancellationToken);
    }

    #endregion

    #region Helpers

    public static Language LibLanguageToLanguage(TesseractOCR.Enums.Language libLanguage)
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

    public static TesseractOCR.Enums.Language LanguageToLibLanguage(Language language)
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

    public static List<TesseractOCR.Enums.Language> LanguageToLibLanguage(List<Language> languages)
    {
        return languages.Select(LanguageToLibLanguage).ToList();
    }

    public static List<OcrWord> ParseTsvString(string tsvContent)
    {
        if (string.IsNullOrWhiteSpace(tsvContent))
            return [];
    
        // Normaliser les fins de ligne pour garantir la cohérence
        var normalizedContent = tsvContent.Replace("\r\n", "\n");
        var lines = normalizedContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    
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

    public static OcrWord? ParseTsvLine(string line)
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
            // Validate minimum column count
            if (columns.Length < 12)
                throw new ArgumentException($"Insufficient columns: expected at least 12, got {columns.Length}");

            // Parse numeric values with validation
            if (!int.TryParse(columns[6], out var left))
                throw new FormatException($"Invalid left coordinate: '{columns[6]}'");

            if (!int.TryParse(columns[7], out var top))
                throw new FormatException($"Invalid top coordinate: '{columns[7]}'");

            if (!int.TryParse(columns[8], out var width))
                throw new FormatException($"Invalid width: '{columns[8]}'");

            if (!int.TryParse(columns[9], out var height))
                throw new FormatException($"Invalid height: '{columns[9]}'");

            if (!float.TryParse(columns[10], out var confidence))
                throw new FormatException($"Invalid confidence value: '{columns[10]}'");

            // Validate parsed numeric values
            if (width <= 0 || height <= 0)
                throw new ArgumentException($"Invalid dimensions: width={width}, height={height}");

            // Extract text from remaining columns (handles embedded tabs)
            var text = string.Join("\t", columns.Skip(11)).Trim();

            // Validate text content
            if (string.IsNullOrWhiteSpace(text))
                return null;

            return new OcrWord(left, top, width, height, confidence, text);
        }
        catch (Exception)
        {
            // Skip malformed lines
            return null;
        }
    }

    #endregion
}