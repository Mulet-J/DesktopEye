using System;
using System.Threading;
using System.Threading.Tasks;
using DesktopEye.Common.Enums;

namespace DesktopEye.Common.Services.Translation;

public interface ITranslationService : IDisposable
{
    string Translate(string text, Language sourceLanguage, Language targetLanguage);

    Task<string> TranslateAsync(string text, Language sourceLanguage, Language targetLanguage,
        CancellationToken cancellationToken = default);

    Task<bool> LoadRequiredAsync(string? modelName = null, CancellationToken cancellationToken = default);

    bool LoadRequired(string? modelName = null);
}