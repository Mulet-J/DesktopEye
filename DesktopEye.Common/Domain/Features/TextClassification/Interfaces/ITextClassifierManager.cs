using System.Threading;
using System.Threading.Tasks;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Domain.Models.TextClassification;
using DesktopEye.Common.Infrastructure.Services.Base;

namespace DesktopEye.Common.Domain.Features.TextClassification.Interfaces;

public interface ITextClassifierManager : IBaseServiceManager<ITextClassifierService, ClassifierType>
{
    Task<Language> ClassifyTextAsync(string text, CancellationToken cancellationToken = default);
}