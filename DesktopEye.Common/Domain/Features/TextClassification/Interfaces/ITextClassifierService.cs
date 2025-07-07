using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DesktopEye.Common.Domain.Models;

namespace DesktopEye.Common.Domain.Features.TextClassification.Interfaces;

public interface ITextClassifierService : IDisposable
{
    Language ClassifyText(string text);

    List<(Language language, double confidence)> ClassifyTextWithProbabilities(string text);

    Task<Language> ClassifyTextAsync(string text, CancellationToken cancellationToken = default);

    Task<bool> LoadRequiredAsync(string? modelName = null, CancellationToken cancellationToken = default);
}