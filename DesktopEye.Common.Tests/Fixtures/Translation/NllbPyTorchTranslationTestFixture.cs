using DesktopEye.Common.Domain.Features.TextTranslation;
using DesktopEye.Common.Infrastructure.Services.ApplicationPath;
using DesktopEye.Common.Infrastructure.Services.Conda;
using DesktopEye.Common.Infrastructure.Services.Download;
using DesktopEye.Common.Infrastructure.Services.Python;
using Microsoft.Extensions.Logging;
using Moq;

namespace DesktopEye.Common.Tests.Fixtures.Translation;

public class NllbPyTorchTranslationTestFixture : IDisposable
{
    public NllbPyTorchTranslationTestFixture()
    {
        // Create mock dependencies
        var mockDownloadService = new Mock<IDownloadService>();

        // Create required dependencies
        IPathService pathService = new PathService();
        var condaLogger = new Mock<ILogger<CondaService>>();
        var mockBugsnagService = new Mock<Bugsnag.IClient>();
        ICondaService condaService = new CondaService(pathService, mockDownloadService.Object, mockBugsnagService.Object, condaLogger.Object);
        var runtimeManagerLogger = new Mock<ILogger<PythonRuntimeManager>>();
        IPythonRuntimeManager runtimeManager =
            new PythonRuntimeManager(pathService, condaService, runtimeManagerLogger.Object);
        var serviceLogger = new Mock<ILogger<NllbPyTorchTranslationService>>();

        // Create service
        TranslationService =
            new NllbPyTorchTranslationService(condaService, pathService, runtimeManager, serviceLogger.Object);
        _ = TranslationService.LoadRequired();
    }

    public NllbPyTorchTranslationService TranslationService { get; }

    public void Dispose()
    {
        TranslationService.Dispose();
        GC.SuppressFinalize(this);
    }
}