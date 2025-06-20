using DesktopEye.Common.Services.OCR;
using DesktopEye.Common.Tests.Fixtures.Ocr;

namespace DesktopEye.Tests.Services.OCR;

public class TesseractOcrTest : IClassFixture<TesseractOcrTestFixture>
{
    private readonly TesseractOcrService _tesseractOcrService;

    public TesseractOcrTest(TesseractOcrTestFixture fixture)
    {
        _tesseractOcrService = fixture.OcrService;
    }
}