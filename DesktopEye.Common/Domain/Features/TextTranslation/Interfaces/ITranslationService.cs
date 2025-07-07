using System;
using System.Threading;
using System.Threading.Tasks;
using DesktopEye.Common.Domain.Models;

namespace DesktopEye.Common.Domain.Features.TextTranslation.Interfaces;

public interface ITranslationService : IDisposable
{
    string Translate(string text, Language sourceLanguage, Language targetLanguage);

    Task<string> TranslateAsync(string text, Language sourceLanguage, Language targetLanguage,
        CancellationToken cancellationToken = default);

    Task<bool> LoadRequiredAsync(string? modelName = null, CancellationToken cancellationToken = default);

    bool LoadRequired(string? modelName = null);
}