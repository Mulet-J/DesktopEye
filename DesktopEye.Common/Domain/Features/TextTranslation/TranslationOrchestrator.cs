using System;
using System.Threading;
using System.Threading.Tasks;
using DesktopEye.Common.Domain.Features.TextTranslation.Interfaces;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Domain.Models.TextTranslation;
using DesktopEye.Common.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

namespace DesktopEye.Common.Domain.Features.TextTranslation;

public class TranslationOrchestrator : ServiceOrchestrator<ITranslationService, TranslationType>, ITranslationOrchestrator
{
    public TranslationOrchestrator(IServiceProvider services,  Bugsnag.IClient bugsnag, ILogger<TranslationOrchestrator>? logger = null)
        : base(services, bugsnag, logger)
    {
    }

    /// <summary>
    ///     Gets the current translator type
    /// </summary>
    public TranslationType GetCurrentTranslatorType => CurrentServiceType;

    /// <summary>
    ///     Translates text using the current translator
    /// </summary>
    /// <param name="input">Text to translate</param>
    /// <param name="sourceLanguage">Source language</param>
    /// <param name="targetLanguage">Target language</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Translated text</returns>
    public async Task<string> TranslateAsync(string input, Language sourceLanguage, Language targetLanguage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            Logger?.LogDebug("Empty input provided for translation");
            return string.Empty;
        }

        return await ExecuteWithServiceAsync(async (service, ct) =>
        {
            Logger?.LogDebug("Translating text from {SourceLanguage} to {TargetLanguage} using {TranslatorType}",
                sourceLanguage, targetLanguage, CurrentServiceType);

            return await service.TranslateAsync(input, sourceLanguage, targetLanguage, ct);
        }, cancellationToken);
    }

    /// <summary>
    ///     Preloads the model for the current translator
    /// </summary>
    /// <param name="modelName">Optional specific model name to load</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if preloading was successful</returns>
    public async Task<bool> LoadCurrentModelAsync(string? modelName = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithServiceAsync(async (service, ct) =>
        {
            Logger?.LogDebug("Preloading model for current translator: {TranslatorType}", CurrentServiceType);
            return await service.LoadRequiredAsync(modelName, ct);
        }, cancellationToken);
    }

    protected override TranslationType GetDefaultServiceType()
    {
        return TranslationType.Nllb;
    }
}