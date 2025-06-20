using DesktopEye.Common.Services.ApplicationPath;
using DesktopEye.Common.Services.Download;
using DesktopEye.Common.Services.OCR;
using Microsoft.Extensions.Logging;
using Moq;

namespace DesktopEye.Common.Tests.Fixtures.Ocr;

public class TesseractOcrTestFixture : IDisposable
{
    public TesseractOcrTestFixture()
    {
        IPathService pathService = new PathService();
        var mockOcrLogger = new Mock<ILogger<TesseractOcrService>>();
        var mockDownloadService = new Mock<DownloadService>();
        OcrService = new TesseractOcrService(pathService, mockDownloadService.Object, mockOcrLogger.Object);
        OcrService.LoadRequiredAsync().GetAwaiter().GetResult();
    }

    public TesseractOcrService OcrService { get; }

    public void Dispose()
    {
        OcrService.Dispose();
        GC.SuppressFinalize(this);
    }
}