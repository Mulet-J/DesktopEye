using System.Diagnostics;
using DesktopEye.Common.Domain.Features.TextTranslation;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Tests.Fixtures.Translation;
using Xunit.Abstractions;

namespace DesktopEye.Common.Tests.Services.Translation;

public class NllbPyTorchTranslationTest : IClassFixture<NllbPyTorchTranslationTestFixture>
{
    private readonly NllbPyTorchTranslationService _nllbPyTorchTranslationService;
    private readonly ITestOutputHelper _output;

    public NllbPyTorchTranslationTest(NllbPyTorchTranslationTestFixture fixture, ITestOutputHelper output)
    {
        _nllbPyTorchTranslationService = fixture.TranslationService;
        _output = output;
    }

    [Fact]
    public void Translate_FrenchToEnglish_ReturnsOk()
    {
        const string input = "Bonjour, comment allez-vous ?";
        const string expected = "Hi, how are you?";

        var actual = _nllbPyTorchTranslationService.Translate(input, Language.French, Language.English);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Translate_EnglishToFrench_ReturnsOk()
    {
        const string input = "Hi, how are you?";
        const string expected = "Bonjour, comment allez-vous ?";

        var actual = _nllbPyTorchTranslationService.Translate(input, Language.English, Language.French);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Translate_MultipleSentences_ReturnsOk()
    {
        const string input1 = "Hi, how are you?";
        const string expected1 = "Bonjour, comment allez-vous ?";
        const string input2 = "Hi, how are you?";
        const string expected2 = "Bonjour, comment allez-vous ?";

        var stopwatch = Stopwatch.StartNew();
        var actual1 = _nllbPyTorchTranslationService.Translate(input1, Language.English, Language.French);
        var firstTranslationTime = stopwatch.ElapsedMilliseconds;

        stopwatch.Restart();
        var actual2 = _nllbPyTorchTranslationService.Translate(input2, Language.English, Language.French);
        var secondTranslationTime = stopwatch.ElapsedMilliseconds;

        _output.WriteLine($"First translation took: {firstTranslationTime}ms");
        _output.WriteLine($"Second translation took: {secondTranslationTime}ms");

        Assert.Equal(expected1, actual1);
        Assert.Equal(expected2, actual2);
    }
}