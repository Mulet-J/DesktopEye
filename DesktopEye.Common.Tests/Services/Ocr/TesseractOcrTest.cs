using DesktopEye.Common.Classes;
using DesktopEye.Common.Extensions;
using DesktopEye.Common.Services.OCR;
using DesktopEye.Common.Tests.Fixtures.Ocr;
using SkiaSharp;
using TesseractOCR.Enums;
using Xunit.Abstractions;
using Language = DesktopEye.Common.Enums.Language;

namespace DesktopEye.Common.Tests.Services.Ocr;

public class TesseractOcrTest : IClassFixture<TesseractOcrTestFixture>
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly TesseractOcrService _tesseractOcrService;

    public TesseractOcrTest(TesseractOcrTestFixture fixture, ITestOutputHelper outputHelper)
    {
        _tesseractOcrService = fixture.OcrService;
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task DetectScriptWithOSD_English_shouldReturnLatin()
    {
        var text = "Hello world! This is an example of english text.";
        var expectedScriptName = ScriptName.Latin;
        using var skBitmap = GenerateBitmapWithText(text, 50f, 50f, 20f, 500, 100);
        using var image = skBitmap.ToAvaloniaBitmap().ToTesseractImage();

        var actualScript = await _tesseractOcrService.DetectScriptWithOsdAsync(image);

        Assert.Equal(expectedScriptName, actualScript.scriptName);
    }

    [Fact]
    public async Task DetectScriptWithOSD_Japanese_shouldReturnJapanese()
    {
        var text = "世界、こんいちは。これは日本語のテキストです。";
        var expectedScriptName = ScriptName.Japanese;
        using var skBitmap = GenerateBitmapWithText(text, 50f, 50f, 20f, 500, 100);
        using var image = skBitmap.ToAvaloniaBitmap().ToTesseractImage();

        var actualScript = await _tesseractOcrService.DetectScriptWithOsdAsync(image);

        Assert.Equal(expectedScriptName, actualScript.scriptName);
    }

    [Fact]
    public async Task GetTextFromBitmapAsync_English_shouldReturnSuccess()
    {
        var expected = "Hello world! This is an example of english text.";
        using var skBitmap = GenerateBitmapWithText(expected, 50f, 50f, 20f, 500, 100);
        using var bitmap = skBitmap.ToAvaloniaBitmap();

        var res = await _tesseractOcrService.GetTextFromBitmapAsync(bitmap, [Language.English]);

        var actual = res.Text.Replace("\n", "").Trim();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task GetTextFromBitmapAsync_Japanese_shouldReturnSuccess()
    {
        var expected = "世界、こんいちは。これは日本語のテキストです。";
        using var skBitmap = GenerateBitmapWithText(expected, 50f, 50f, 20f, 500, 100);
        using var bitmap = skBitmap.ToAvaloniaBitmap();

        var res = await _tesseractOcrService.GetTextFromBitmapAsync(bitmap, [Language.Japanese]);

        //TODO implement ocr post processing to handle issues like extra spaces
        var actual = res.Text.Replace("\n", "").Replace(" ", "");

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task GetTextFromBitmapTwoPassAsync_English_shouldReturnSuccess()
    {
        var expected = "Hello world! This is an example of english text.";
        using var skBitmap = GenerateBitmapWithText(expected, 50f, 50f, 20f, 500, 100);
        using var bitmap = skBitmap.ToAvaloniaBitmap();

        var res = await _tesseractOcrService.GetTextFromBitmapAsync(bitmap);

        var actual = res.Text.Replace("\n", "").Trim();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task GetTextFromBitmapTwoPassAsync_Japanese_shouldReturnSuccess()
    {
        var expected = "世界、こんいちは。これは日本語のテキストです。";
        using var skBitmap = GenerateBitmapWithText(expected, 50f, 50f, 20f, 500, 100);
        using var bitmap = skBitmap.ToAvaloniaBitmap();

        var res = await _tesseractOcrService.GetTextFromBitmapAsync(bitmap);

        var actual = res.Text.Replace("\n", "").Replace(" ", "");

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task GetTextFromBitmapTwoPassAsync_Empty_shouldHandleGracefully()
    {
        var gibberish = " ";
        using var skBitmap = GenerateBitmapWithText(gibberish, 50f, 50f, 20f, 500, 100);
        using var bitmap = skBitmap.ToAvaloniaBitmap();


        var res = await _tesseractOcrService.GetTextFromBitmapAsync(bitmap);

        var actual = res.Text.Replace("\n", "").Trim();

        Assert.NotNull(actual);
    }

    [Fact]
    public async Task GetTextFromBitmapUsingScriptNameAsync_English_shouldReturnSuccess()
    {
        var expected = "Hello world! This is an example of english text.";
        using var skBitmap = GenerateBitmapWithText(expected, 50f, 50f, 20f, 500, 100);
        using var image = skBitmap.ToAvaloniaBitmap().ToTesseractImage();

        var res = await _tesseractOcrService.GetTextFromImageUsingScriptNameAsync(image, ScriptName.Latin);

        var actual = res.Text.Replace("\n", "").Trim();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task GetTextFromBitmapUsingScriptNameAsync_Japanese_shouldReturnSuccess()
    {
        var expected = "世界、こんいちは。これは日本語のテキストです。";
        using var skBitmap = GenerateBitmapWithText(expected, 50f, 50f, 20f, 500, 100);
        using var image = skBitmap.ToAvaloniaBitmap().ToTesseractImage();

        OcrResult res = null;
        try
        {
            res = await _tesseractOcrService.GetTextFromImageUsingScriptNameAsync(image, ScriptName.Japanese);
        }
        catch (Exception ex)
        {
            _outputHelper.WriteLine(ex.ToString());
        }

        var actual = res?.Text.Replace("\n", "").Replace(" ", "");

        Assert.Equal(expected, actual);
    }

    #region Helpers

    private SKBitmap GenerateBitmapWithText(string text, float leftMargin, float topMargin, float fontSize, int width,
        int height)
    {
        using var fontManager = SKFontManager.Default;
        var matchingTypeface = fontManager.MatchCharacter(text[0]);

        // Create bitmap and canvas
        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);

        // Clear background to white
        canvas.Clear(SKColors.White);

        canvas.DrawText(text, leftMargin, topMargin, SKTextAlign.Left, new SKFont(matchingTypeface, fontSize),
            new SKPaint());
        return bitmap;
    }

    #endregion
}