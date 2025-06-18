using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DesktopEye.Common.Enums;

namespace DesktopEye.Common.Services.TextClassifier;

public interface ITextClassifierService : IDisposable
{
    Language ClassifyText(string text);

    List<(Language language, double confidence)> ClassifyTextWithProbabilities(string text);

    Task<Language> ClassifyTextAsync(string text, CancellationToken cancellationToken = default);

    Task<bool> LoadRequiredAsync(string? modelName = null, CancellationToken cancellationToken = default);
}