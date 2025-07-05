using System.Threading;
using System.Threading.Tasks;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Domain.Models.TextTranslation;
using DesktopEye.Common.Infrastructure.Services.Base;

namespace DesktopEye.Common.Domain.Features.TextTranslation.Interfaces;

public interface ITranslationManager : IBaseServiceManager<ITranslationService, TranslationType>
{
    Task<string> TranslateAsync(string input, Language sourceLanguage, Language targetLanguage,
        CancellationToken cancellationToken = default);
}