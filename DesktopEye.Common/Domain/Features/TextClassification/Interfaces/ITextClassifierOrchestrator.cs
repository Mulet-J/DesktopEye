using System.Threading;
using System.Threading.Tasks;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Domain.Models.TextClassification;
using DesktopEye.Common.Infrastructure.Configuration;

namespace DesktopEye.Common.Domain.Features.TextClassification.Interfaces;

public interface ITextClassifierOrchestrator : IServiceOrchestrator<ITextClassifierService, ClassifierType>
{
    Task<Language> ClassifyTextAsync(string text, CancellationToken cancellationToken = default);
}