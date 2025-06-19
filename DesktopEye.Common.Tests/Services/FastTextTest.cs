using DesktopEye.Common.Services.TextClassifierService;

namespace DesktopEye.Common.Tests.Services;

public class FastTextTest
{
    private readonly FastTextService _fastTextService;

    public FastTextTest()
    {
        var modelPath = "model.bin";
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