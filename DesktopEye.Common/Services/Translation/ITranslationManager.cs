using System.Threading.Tasks;
using DesktopEye.Common.Enums;

namespace DesktopEye.Common.Services.Translation;

public interface ITranslationManager
{
    Task SwitchToAsync(TranslationType translatorType);
    Task<string> TranslateAsync(string input, Language sourceLanguage, Language targetLanguage);
    TranslationType GetCurrentTranslatorType();
    Task InitializeService();
}