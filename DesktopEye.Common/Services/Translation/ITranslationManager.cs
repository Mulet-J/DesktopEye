using System.Threading;
using System.Threading.Tasks;
using DesktopEye.Common.Enums;
using DesktopEye.Common.Services.Base;

namespace DesktopEye.Common.Services.Translation;

public interface ITranslationManager : IBaseServiceManager<ITranslationService, TranslationType>
{
    Task<string> TranslateAsync(string input, Language sourceLanguage, Language targetLanguage,
        CancellationToken cancellationToken = default);
}