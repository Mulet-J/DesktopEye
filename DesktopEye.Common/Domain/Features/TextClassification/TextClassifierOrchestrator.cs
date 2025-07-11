using System;
using System.Threading;
using System.Threading.Tasks;
using DesktopEye.Common.Domain.Features.TextClassification.Interfaces;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Domain.Models.TextClassification;
using DesktopEye.Common.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

namespace DesktopEye.Common.Domain.Features.TextClassification;

public class TextClassifierOrchestrator : ServiceOrchestrator<ITextClassifierService, ClassifierType>,
    ITextClassifierOrchestrator
{
    public TextClassifierOrchestrator(IServiceProvider services,  Bugsnag.IClient bugsnag, ILogger<TextClassifierOrchestrator>? logger = null)
        : base(services, bugsnag, logger)
    {
    }

    /// <summary>
    ///     Gets the current classifier type
    /// </summary>
    /// <returns>Current classifier type</returns>
    public ClassifierType GetCurrentClassifierType => CurrentServiceType;

    #region ServiceFuncs

    protected override ClassifierType GetDefaultServiceType()
    {
        return ClassifierType.NTextCat;
    }

    #endregion

    #region Classification

    /// <summary>
    ///     Classifies text asynchronously using the current classifier
    /// </summary>
    /// <param name="text">Text to classify</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detected language</returns>
    public async Task<Language> ClassifyTextAsync(string text, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithServiceAsync(async (service, ct) =>
        {
            Logger?.LogDebug("Classifying text using {ClassifierType}", CurrentServiceType);
            return await service.ClassifyTextAsync(text, ct);
        }, cancellationToken);
    }


    /// <summary>
    ///     Classifies text synchronously using the current classifier
    /// </summary>
    /// <param name="text">Text to classify</param>
    /// <returns>Detected language</returns>
    public Language ClassifyText(string text)
    {
        return ExecuteWithService(service =>
        {
            Logger?.LogDebug("Classifying text using {ClassifierType}", CurrentServiceType);
            return service.ClassifyText(text);
        });
    }

    #endregion
}