using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DesktopEye.Common.Enums;
using DesktopEye.Common.Helpers;
using DesktopEye.Common.Interfaces;
using DesktopEye.Common.Services.ApplicationPath;
using DesktopEye.Common.Services.Download;
using Microsoft.Extensions.Logging;
using NTextCat;

namespace DesktopEye.Common.Services.TextClassifier;

public class NTextCatClassifierService : ITextClassifierService, ILoadable
{
    private const string BaseModelName = "Core14.profile.xml";

    private readonly string _baseModelDownloadUrl =
        "https://raw.githubusercontent.com/ivanakcheurov/ntextcat/refs/heads/master/src/LanguageModels/Core14.profile.xml";

    private readonly IDownloadService _downloadService;
    private readonly ILogger<NTextCatClassifierService> _logger;
    private readonly string _modelPath;
    private readonly string _modelsFolder;
    private bool _isDisposed;

    private RankedLanguageIdentifier? _languageIdentifier;
    private Task? _loadingTask;

    public NTextCatClassifierService(IPathService pathService, IDownloadService downloadService,
        ILogger<NTextCatClassifierService> logger)
    {
        _downloadService = downloadService;
        _logger = logger;
        _modelsFolder = pathService.ModelsDirectory;
        _modelPath = Path.Combine(_modelsFolder, BaseModelName);
    }

    public string Name => "NTextCat";

    public bool IsModelLoaded => _languageIdentifier != null;
    public bool IsModelLoading => _loadingTask?.IsCompleted == false;
    public string? LoadedModelName => IsModelLoaded ? BaseModelName : null;

    public Language ClassifyText(string text)
    {
        if (!IsModelLoaded) throw new InvalidOperationException("Model is not loaded. Call PreloadModelAsync first.");

        _logger.LogDebug("Starting text classification for text of length: {TextLength}", text.Length);
        ValidateInput(text);

        try
        {
            var languages = _languageIdentifier!.Identify(text);
            var topLanguage = languages.FirstOrDefault()?.Item1.Iso639_2T;
            var result = LibLanguageToLanguage(topLanguage);

            _logger.LogDebug("Text classification completed. Detected language: {Language} (ISO: {IsoCode})",
                result, topLanguage);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during text classification for text of length: {TextLength}",
                text.Length);
            throw;
        }
    }

    public List<(Language language, double confidence)> ClassifyTextWithProbabilities(string text)
    {
        if (!IsModelLoaded) throw new InvalidOperationException("Model is not loaded. Call PreloadModelAsync first.");

        _logger.LogDebug("Starting text classification with probabilities for text of length: {TextLength}",
            text.Length);

        ValidateInput(text);

        try
        {
            var languages = _languageIdentifier!.Identify(text);
            var results = languages.Select(tuple => (LibLanguageToLanguage(tuple.Item1.Iso639_3), tuple.Item2))
                .ToList();

            _logger.LogDebug("Text classification with probabilities completed. Found {Count} language candidates",
                results.Count);

            if (_logger.IsEnabled(LogLevel.Trace))
                foreach (var (language, confidence) in results.Take(3))
                    _logger.LogTrace("Language candidate: {Language} with confidence: {Confidence:F4}", language,
                        confidence);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error occurred during text classification with probabilities for text of length: {TextLength}",
                text.Length);
            throw;
        }
    }

    public async Task<Language> ClassifyTextAsync(string text, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => ClassifyText(text), cancellationToken);
    }

    public async Task<bool> LoadRequiredAsync(string? modelName = null, CancellationToken cancellationToken = default)
    {
        if (IsModelLoaded || IsModelLoading) return true;

        _loadingTask = LoadModelInternalAsync(cancellationToken);
        await _loadingTask;
        return true;
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        // _languageIdentifier?.Dispose();
        _isDisposed = true;
    }

    private async Task LoadModelInternalAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing NTextCat classifier service with model path: {ModelPath}", _modelPath);

        var downloadResult = await DownloadModelAsync(cancellationToken);
        if (!downloadResult)
        {
            _logger.LogError("Failed to download model file from {Url}", _baseModelDownloadUrl);
            throw new Exception("Failed to download language model");
        }

        try
        {
            _logger.LogDebug("Loading language identifier factory");
            var factory = new RankedLanguageIdentifierFactory();
            _languageIdentifier = factory.Load(_modelPath);
            _logger.LogInformation("Successfully initialized NTextCat classifier with model: {ModelPath}", _modelPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize NTextCat classifier from model: {ModelPath}", _modelPath);
            throw new InvalidOperationException("Failed to initialize NTextCat classifier", ex);
        }
    }

    private async Task<bool> DownloadModelAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Checking if model file exists at: {ModelPath}", _modelPath);

        if (File.Exists(_modelPath))
        {
            _logger.LogDebug("Model file already exists, skipping download");
            return true;
        }

        try
        {
            _logger.LogInformation("Model file not found. Starting download from: {Url}", _baseModelDownloadUrl);

            var success = await _downloadService.DownloadFileAsync(_baseModelDownloadUrl, _modelPath);

            if (success)
                _logger.LogInformation("Successfully downloaded model file to: {ModelPath}", _modelPath);
            else
                _logger.LogWarning("Download service returned false for model download");

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while downloading model from {Url} to {Path}",
                _baseModelDownloadUrl, _modelPath);
            return false;
        }
    }

    private static void ValidateInput(string text)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be empty or whitespace", nameof(text));
    }

    private Language LibLanguageToLanguage(string? language)
    {
        _logger.LogTrace("Converting ISO language code '{IsoCode}' to Language enum", language);
        var result = LanguageHelper.From639_3ToLanguage(language);
        _logger.LogTrace("Converted '{IsoCode}' to {Language}", language, result);
        return result;
    }
}