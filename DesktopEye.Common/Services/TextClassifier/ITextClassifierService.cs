using System.Collections.Generic;
using System.Threading.Tasks;
using DesktopEye.Common.Enums;

namespace DesktopEye.Common.Services.TextClassifier;

public interface ITextClassifierService
{
    string Name { get; }
    Language ClassifyText(string text);
    List<(Language language, double confidence)> ClassifyTextWithProbabilities(string text);
    Task<Language> ClassifyTextAsync(string text);
}