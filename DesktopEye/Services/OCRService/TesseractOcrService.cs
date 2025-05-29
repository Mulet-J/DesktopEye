using System.Collections.Generic;
using DesktopEye.Extensions;
using SkiaSharp;
using TesseractOCR;
using TesseractOCR.Enums;

namespace DesktopEye.Services.OCRService;

public class TesseractOcrService : IOcrService
{
    private readonly Engine _engine;

    public TesseractOcrService(List<Language> languages)
    {
        //TODO make tessdata folder dynamic
        _engine = new Engine("/usr/share/tessdata/", languages);
    }

    public string BitmapToText(SKBitmap bitmap)
    {
        var picture = bitmap.ToTesseractImage();
        using var page = _engine.Process(picture);
        var text = page.Text;
        return text;
    }
}