using DesktopEye.Services.TextClassifierService;

namespace DesktopEye.Tests.Services;

public class FastTextTest
{
    private FastTextService _fastTextService;
    
    public FastTextTest()
    {
        // Initialize the FastTextService with the path to your model
        var modelPath = "C:\\Users\\Rémi\\Downloads\\model.bin"; // Adjust the path as necessary
        _fastTextService = new FastTextService(modelPath);
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
    
    [Fact]
    public void InferLanguage_French_shouldReturnFrench()
    {
        var inputText = "Ceci est un texte en français.";
        var expectedLanguage = "__label__fra_Latn";

        var actualLanguage = _fastTextService.InferLanguage(inputText);

        Assert.Equal(expectedLanguage, actualLanguage);
    }
    
    [Fact]
    public void InferLanguage_English_shouldReturnEnglish()
    {
        var inputText = "This is a text in English.";
        var expectedLanguage = "__label__eng_Latn";

        var actualLanguage = _fastTextService.InferLanguage(inputText);

        Assert.Equal(expectedLanguage, actualLanguage);
    }
}