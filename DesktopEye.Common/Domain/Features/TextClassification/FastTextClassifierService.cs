using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DesktopEye.Common.Domain.Features.TextClassification.Interfaces;
using DesktopEye.Common.Exceptions;
using DesktopEye.Common.Interfaces;
using DesktopEye.Common.Services.ApplicationPath;
using FastText.NetWrapper;
using Microsoft.Extensions.Logging;
using Language = DesktopEye.Common.Domain.Models.Language;
using LanguageHelper = DesktopEye.Common.Domain.Features.TextClassification.Helpers.LanguageHelper;

namespace DesktopEye.Common.Domain.Features.TextClassification;

public class FastTextClassifierService : ITextClassifierService, ILoadable
{
    private readonly FastTextWrapper _fastText;
    private readonly ILogger<FastTextClassifierService> _logger;


    public FastTextClassifierService(IPathService pathService, ILogger<FastTextClassifierService> logger,
        string baseModel = "")
    {
        _logger = logger;
        _fastText = new FastTextWrapper();

        var path = Path.Combine(pathService.ModelsDirectory, "FastText", "model.bin");
        _logger.LogInformation("Initializing FastTextClassifierService with model path: {ModelPath}", path);

        try
        {
            if (!File.Exists(path))
            {
                _logger.LogError("FastText model file not found at path: {ModelPath}", path);
                throw new FileNotFoundException($"FastText model file not found at: {path}");
            }

            _fastText.LoadModel(path);
            _logger.LogInformation("FastText model loaded successfully from: {ModelPath}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load FastText model from path: {ModelPath}", path);
            throw;
        }
    }

    public void Dispose()
    {
        _logger.LogDebug("Disposing FastTextClassifierService");

        try
        {
            _fastText.Dispose();
            _logger.LogDebug("FastText wrapper disposed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception occurred while disposing FastText wrapper");
        }

        GC.SuppressFinalize(this);
    }

    public Language ClassifyText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Attempted to classify null or empty text");
            //TODO
            throw new Exception();
        }

        _logger.LogDebug("Classifying text with length: {TextLength}", text.Length);

        try
        {
            var prediction = _fastText.PredictSingle(text);
            _logger.LogDebug("FastText prediction result: {Label}", prediction.Label);

            var language = LibLanguageToLanguage(prediction.Label);
            _logger.LogDebug("Classified text as language: {Language}", language);

            return language;
        }
        catch (LanguageException ex)
        {
            _logger.LogWarning(ex, "Language classification failed for text with length: {TextLength}", text.Length);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during text classification for text with length: {TextLength}",
                text.Length);
            throw;
        }
    }

    public List<(Language language, double confidence)> ClassifyTextWithProbabilities(string text)
    {
        _logger.LogWarning("ClassifyTextWithProbabilities method is not implemented");
        throw new NotImplementedException();
    }

    public Task<Language> ClassifyTextAsync(string text, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> LoadRequiredAsync(string? modelName = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private Language LibLanguageToLanguage(string language)
    {
        _logger.LogDebug("Converting library language format: {LanguageLabel}", language);

        try
        {
            // Format is either "__label__xxx_Latn" or "__label__xx"
            string formatted;
            Language result;
            if (language.Length == 17)
            {
                formatted = language.Substring(9, 2);
                result = LanguageHelper.From639_3ToLanguage(formatted);
                _logger.LogDebug("Extracted language code from long format: {LanguageCode}", formatted);
            }
            else if (language.Length == 11)
            {
                formatted = language.Substring(9);
                result = LanguageHelper.From639_1ToLanguage(formatted);
                _logger.LogDebug("Extracted language code from short format: {LanguageCode}", formatted);
            }
            else
            {
                _logger.LogError("Unexpected language label format: {LanguageLabel} (length: {Length})", language,
                    language.Length);
                throw new LanguageException();
            }

            return result;
        }
        catch (Exception ex) when (!(ex is LanguageException))
        {
            _logger.LogError(ex, "Unexpected error while converting language format: {LanguageLabel}", language);
            throw new LanguageException();
        }
    }
}