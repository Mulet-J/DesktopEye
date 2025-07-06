using System.Threading;
using System.Threading.Tasks;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Domain.Models.TextTranslation;
using DesktopEye.Common.Infrastructure.Configuration;

namespace DesktopEye.Common.Domain.Features.TextTranslation.Interfaces;

public interface ITranslationOrchestrator : IServiceOrchestrator<ITranslationService, TranslationType>
{
    Task<string> TranslateAsync(string input, Language sourceLanguage, Language targetLanguage,
        CancellationToken cancellationToken = default);
}