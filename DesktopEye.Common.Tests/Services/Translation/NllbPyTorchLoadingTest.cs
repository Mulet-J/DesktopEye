using Bugsnag;
using DesktopEye.Common.Domain.Features.TextTranslation;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Infrastructure.Configuration;
using DesktopEye.Common.Infrastructure.Configuration.Interfaces;
using DesktopEye.Common.Infrastructure.Models;
using DesktopEye.Common.Infrastructure.Services.ApplicationPath;
using DesktopEye.Common.Infrastructure.Services.Conda;
using DesktopEye.Common.Infrastructure.Services.Download;
using DesktopEye.Common.Infrastructure.Services.Python;
using DesktopEye.Common.Infrastructure.Services.TrainedModel;
using Microsoft.Extensions.Logging;
using Moq;

namespace DesktopEye.Common.Tests.Services.Translation;

public class NllbPyTorchLoadingTest
{
    private readonly ModelDownloadService _modelDownloadService;
    private readonly NllbPyTorchTranslationService _nllbPyTorchTranslationService;

    public NllbPyTorchLoadingTest()
    {
        // Create mock dependencies
        var mockDownloadService = new Mock<IDownloadService>();

        // Create required dependencies
        IAppConfigService appConfigService = new AppConfigService();
        IPathService pathService = new PathService(appConfigService);
        var condaLogger = new Mock<ILogger<CondaService>>();
        var mockBugsnagService = new Mock<IClient>();
        ICondaService condaService = new CondaService(pathService, mockDownloadService.Object,
            mockBugsnagService.Object, condaLogger.Object);
        var runtimeManagerLogger = new Mock<ILogger<PythonRuntimeManager>>();
        IPythonRuntimeManager runtimeManager =
            new PythonRuntimeManager(pathService, condaService, runtimeManagerLogger.Object);
        var serviceLogger = new Mock<ILogger<NllbPyTorchTranslationService>>();

        var modelDownloadLogger = new Mock<ILogger<ModelDownloadService>>();
        var downloadService = new Mock<IDownloadService>();
        _modelDownloadService = new ModelDownloadService(downloadService.Object, pathService, runtimeManager,
            modelDownloadLogger.Object, mockBugsnagService.Object);

        // Create service
        _nllbPyTorchTranslationService =
            new NllbPyTorchTranslationService(condaService, pathService, runtimeManager, serviceLogger.Object);
        _ = _nllbPyTorchTranslationService.LoadRequired();
    }

    [Fact]
    public async Task Translate_FrenchToEnglish_ReturnsOk()
    {
        var model = new Model
        {
            ModelName = "facebook/nllb-200-distilled-600M",
            ModelUrl = "https://huggingface.co/facebook/nllb-200-distilled-600M",
            ModelFolderName = "nllb-pytorch",
            Runtime = ModelRuntime.NllbPyTorch,
            Source = ModelSource.HuggingFace,
            Type = ModelType.TextTranslator
        };
        _ = await _modelDownloadService.DownloadModelAsync(model);
        _ = await _modelDownloadService.DownloadModelAsync(model);
        _ = await _modelDownloadService.DownloadModelAsync(model);
        const string input = "Bonjour, comment allez-vous ?";
        const string expected = "Hi, how are you?";

        var actual = _nllbPyTorchTranslationService.Translate(input, Language.French, Language.English);

        Assert.Equal(expected, actual);
    }
}