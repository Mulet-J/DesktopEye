using DesktopEye.Common.Domain.Features.TextClassification;
using DesktopEye.Common.Infrastructure.Configuration;
using DesktopEye.Common.Infrastructure.Configuration.Interfaces;
using DesktopEye.Common.Infrastructure.Services.ApplicationPath;
using Microsoft.Extensions.Logging;
using Moq;

namespace DesktopEye.Common.Tests.Fixtures.TextClassifier;

public class FastTextClassifierTestFixture : IDisposable
{
    public FastTextClassifierTestFixture()
    {
        IAppConfigService appConfigService = new AppConfigService();
        IPathService pathService = new PathService(appConfigService);
        var classifierLogger = new Mock<ILogger<FastTextClassifierService>>();
        ClassifierService = new FastTextClassifierService(pathService, classifierLogger.Object);
    }

    public FastTextClassifierService ClassifierService { get; }

    public void Dispose()
    {
        ClassifierService.Dispose();
        GC.SuppressFinalize(this);
    }
}