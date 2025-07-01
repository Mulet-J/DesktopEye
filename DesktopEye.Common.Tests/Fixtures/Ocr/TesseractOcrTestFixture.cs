using Avalonia;
using DesktopEye.Common.Services.ApplicationPath;
using DesktopEye.Common.Services.Download;
using DesktopEye.Common.Services.OCR;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Moq;

namespace DesktopEye.Common.Tests.Fixtures.Ocr;

[UsedImplicitly]
public class TesseractOcrTestFixture : IDisposable
{
    public TesseractOcrTestFixture()
    {
        AppBuilder.Configure<Application>()
            .UsePlatformDetect()
            .WithInterFont()
            .SetupWithoutStarting();

        IPathService pathService = new PathService();
        var mockDownloadLogger = new Mock<ILogger<DownloadService>>();
        var mockDownloadFactory = new Mock<IHttpClientFactory>();
        var mockDownloadService = new Mock<DownloadService>(mockDownloadFactory.Object, mockDownloadLogger.Object);
        var mockOcrLogger = new Mock<ILogger<TesseractOcrService>>();
        OcrService = new TesseractOcrService(pathService, mockDownloadService.Object, mockOcrLogger.Object);
        OcrService.LoadRequired();
    }

    public TesseractOcrService OcrService { get; }

    public void Dispose()
    {
        OcrService.Dispose();
        GC.SuppressFinalize(this);
    }
}