using DesktopEye.Services.TranslationService;

namespace DesktopEye.Tests.Services;

public class NllbPyTorchTranslationTest
{
    public NllbPyTorchTranslationTest()
    {
        
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
    
    [Fact]
    public void TestTranslation()
    {
        // Act
        string result = NllbPyTorchTranslationService.Translate("Hello world", "eng_Latn", "fra_Latn");
        
        // Assert
        Assert.Equal("Bonjour le monde", result);
    }
}