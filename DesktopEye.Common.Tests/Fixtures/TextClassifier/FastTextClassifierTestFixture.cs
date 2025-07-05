using DesktopEye.Common.Domain.Features.TextClassification;
using DesktopEye.Common.Services.ApplicationPath;
using Microsoft.Extensions.Logging;
using Moq;

namespace DesktopEye.Common.Tests.Fixtures.TextClassifier;

public class FastTextClassifierTestFixture : IDisposable
{
    public FastTextClassifierTestFixture()
    {
        IPathService pathService = new PathService();
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