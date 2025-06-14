using DesktopEye.Common.Enums;
using DesktopEye.Common.Services.TextClassifier;
using DesktopEye.Common.Tests.Fixtures.TextClassifier;

namespace DesktopEye.Common.Tests.Services.TextClassifier;

public class FastTextClassifierTest : IClassFixture<FastTextClassifierTestFixture>
{
    private readonly FastTextClassifierService _fastTextClassifierService;


    public FastTextClassifierTest(FastTextClassifierTestFixture fixture)
    {
        _fastTextClassifierService = fixture.ClassifierService;
    }

    [Fact]
    public void ClassifyText_French_shouldReturnFrench()
    {
        const string inputText = "Ceci est un texte en Français.";
        const Language expectedLanguage = Language.French;

        var actualLanguage = _fastTextClassifierService.ClassifyText(inputText);

        Assert.Equal(expectedLanguage, actualLanguage);
    }

    [Fact]
    public void ClassifyText_English_shouldReturnEnglish()
    {
        const string inputText = "This is a text in English.";
        const Language expectedLanguage = Language.English;

        var actualLanguage = _fastTextClassifierService.ClassifyText(inputText);

        Assert.Equal(expectedLanguage, actualLanguage);
    }
}