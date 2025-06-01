namespace DesktopEye.Common.Tests.Services;

public class TesseractOcrTest : IDisposable
{
    //TODO
    public TesseractOcrTest()
    {
        var a = 1;
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }

    // [Fact]
    // public void OcrLoremIpsumTest()
    // {
    //     var assetsPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets");
    //     var imagePath = Path.Combine(assetsPath, "multilines_lorem_ipsum_w_on_b.png");
    //     var textPath = Path.Combine(assetsPath, "multilines_lorem_ipsum_w_on_b.txt");
    //
    //     var engine = new TesseractOcrService([Language.Latin]);
    //
    //     var expected = File.ReadAllText(textPath);
    //
    //     var image = SKBitmap.Decode(imagePath);
    //     var text = engine.BitmapToText(image);
    //     var actual = Regex.Replace(text, @"\n+", " ");
    //
    //     var mat = image.ToMat();
    //     mat.SaveImage(assetsPath + "/output.png");
    //
    //     Assert.Equal(expected, actual);
    // }
}