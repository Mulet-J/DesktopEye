using DesktopEye.Helpers;
using SkiaSharp;
using Tesseract;

namespace DesktopEye.Services.OCRService;

public class TesseractOcrService : IOcrService
{
    public static void a(SKBitmap bitmap)
    {
        var picture = bitmap.ToTesseractPix();
        using var engine = new TesseractEngine("/usr/share/tessdata/", "eng", EngineMode.Default);
        using var page = engine.Process(picture);
        var text = page.GetText();
        ;
    }
}