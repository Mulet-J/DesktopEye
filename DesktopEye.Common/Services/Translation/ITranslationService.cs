using System.Threading.Tasks;
using DesktopEye.Common.Enums;

namespace DesktopEye.Common.Services.Translation;

public interface ITranslationService
{
    string Translate(string input, Language sourceLanguage, Language targetLanguage);
    Task<string> TranslateAsync(string input, Language sourceLanguage, Language targetLanguage);
    bool LoadRequired(string modelName);
    Task<bool> LoadRequiredAsync();
    Task<bool> LoadRequiredAsync(string modelName);
}