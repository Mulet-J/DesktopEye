using System.Threading;
using System.Threading.Tasks;
using DesktopEye.Common.Enums;
using DesktopEye.Common.Services.Base;

namespace DesktopEye.Common.Services.TextClassifier;

public interface ITextClassifierManager : IBaseServiceManager<ITextClassifierService, ClassifierType>
{
    Task<Language> ClassifyTextAsync(string text, CancellationToken cancellationToken = default);
}