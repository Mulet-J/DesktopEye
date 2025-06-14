using DesktopEye.Common.Services.ApplicationPath;
using DesktopEye.Common.Services.Conda;
using DesktopEye.Common.Services.Download;
using DesktopEye.Common.Services.Python;
using DesktopEye.Common.Services.Translation;
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
        ICondaService condaService = new CondaService(pathService, mockDownloadService.Object, condaLogger.Object);
        var runtimeManagerLogger = new Mock<ILogger<PythonRuntimeManager>>();
        IPythonRuntimeManager runtimeManager =
            new PythonRuntimeManager(pathService, condaService, runtimeManagerLogger.Object);

        // Create service
        TranslationService = new NllbPyTorchTranslationService(condaService, pathService, runtimeManager);
        _ = TranslationService.LoadRequired();
    }

    public NllbPyTorchTranslationService TranslationService { get; }

    public void Dispose()
    {
        TranslationService.Dispose();
        GC.SuppressFinalize(this);
    }
}