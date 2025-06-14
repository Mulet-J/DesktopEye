using System.Threading.Tasks;
using DesktopEye.Common.Enums;

namespace DesktopEye.Common.Services.TextClassifier;

public interface ITextClassifierManager
{
    Task SwitchToAsync(ClassifierType classifierType);
    Task<Language> ClassifyTextAsync(string text);
    ClassifierType GetCurrentClassifierType();
}