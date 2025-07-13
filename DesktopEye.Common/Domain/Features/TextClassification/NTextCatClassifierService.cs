using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DesktopEye.Common.Domain.Features.TextClassification.Helpers;
using DesktopEye.Common.Domain.Features.TextClassification.Interfaces;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Infrastructure.Configuration.Interfaces;
using DesktopEye.Common.Infrastructure.Models;
using DesktopEye.Common.Infrastructure.Services.ApplicationPath;
using DesktopEye.Common.Infrastructure.Services.TrainedModel;
using Microsoft.Extensions.Logging;
using NTextCat;

namespace DesktopEye.Common.Domain.Features.TextClassification;

public class NTextCatClassifierService : ITextClassifierService, ILoadable
{
    private readonly ILogger<NTextCatClassifierService> _logger;
    private readonly ModelRegistry _modelRegistry = new ModelRegistry();
    private readonly IPathService _pathService;
    private bool _isDisposed;

    private RankedLanguageIdentifier? _languageIdentifier;
    private Task? _loadingTask;

    public NTextCatClassifierService(ILogger<NTextCatClassifierService> logger, IPathService pathService)
    {
        _logger = logger;
        _pathService = pathService;
    }

    private Model NTextCatModel =>
        _modelRegistry.DefaultModels.FirstOrDefault(model => model.ModelName == "NTextCat.xml") ??
        throw new InvalidOperationException("NTextCat model not found in registry");

    public bool IsModelLoading => _loadingTask?.IsCompleted == false;
    public bool IsModelLoaded => _languageIdentifier != null;

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

        _loadingTask = LoadModelInternalAsync();
        await _loadingTask;
        return true;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
    }

    private Task LoadModelInternalAsync()
    {
        var modelPath = Path.Combine(_pathService.ModelsDirectory, NTextCatModel.ModelFolderName,
            NTextCatModel.ModelName);
        _logger.LogInformation("Initializing NTextCat classifier service with model path: {ModelPath}", modelPath);

        try
        {
            _logger.LogDebug("Loading language identifier factory");
            var factory = new RankedLanguageIdentifierFactory();
            _languageIdentifier = factory.Load(modelPath);
            _logger.LogInformation("Successfully initialized NTextCat classifier with model: {ModelPath}", modelPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize NTextCat classifier from model: {ModelPath}", modelPath);
            throw new InvalidOperationException("Failed to initialize NTextCat classifier", ex);
        }

        return Task.CompletedTask;
    }

    private static void ValidateInput(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException(@"Text cannot be empty or whitespace", nameof(text));
    }

    private Language LibLanguageToLanguage(string? language)
    {
        _logger.LogTrace("Converting ISO language code '{IsoCode}' to Language enum", language);
        var result = LanguageHelper.From639_3ToLanguage(language);
        _logger.LogTrace("Converted '{IsoCode}' to {Language}", language, result);
        return result;
    }
}