using DesktopEye.Common.Services.TranslationService;

namespace DesktopEye.Common.Tests.Services;

public class NllbPyTorchTranslationTest
{
    public void Dispose()
    {
        // TODO release managed resources here
    }

    [Fact]
    public void TestTranslation()
    {
        // Act
        var result = NllbPyTorchTranslationService.Translate("Hello world", "eng_Latn", "fra_Latn");

        // Assert
        Assert.Equal("Bonjour le monde", result);
    }
}