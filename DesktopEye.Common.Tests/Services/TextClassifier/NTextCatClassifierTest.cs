using DesktopEye.Common.Domain.Features.TextClassification;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Infrastructure.Services.ApplicationPath;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace DesktopEye.Common.Tests.Services.TextClassifier;

public class NTextCatClassifierTest
{
    private readonly NTextCatClassifierService _nTextCatClassifierService;

    public NTextCatClassifierTest()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("DesktopEyeClient",
            client => { client.DefaultRequestHeaders.Add("User-Agent", "DesktopEye/1.0"); });

        // Build the service provider and get the HttpClientFactory
        var nTextCatLogger = new Mock<ILogger<NTextCatClassifierService>>();
        var pathServiceMock = new Mock<IPathService>();

        // Create required dependencies
        _nTextCatClassifierService = new NTextCatClassifierService(nTextCatLogger.Object, pathServiceMock.Object);
    }

    // [Fact]
    // public async Task DownloadModel_shouldReturnTrue()
    // {
    //     const bool expected = true;
    //
    //     // var actual = await _nTextCatClassifierService.DownloadModelAsync();
    //
    //     Assert.Equal(expected, actual);
    // }
    
    [Fact]
    public async Task PreloadModelAsync_ShouldLoadModel()
    {
        // Act
        await _nTextCatClassifierService.LoadRequiredAsync();

        // Assert
        Assert.True(_nTextCatClassifierService.IsModelLoaded);
    }

    [Fact]
    public void ClassifyText_French_shouldReturnFrench()
    {
        var inputText = "Ceci est un texte en français.";
        var expectedLanguage = Language.French;
        
        _ = _nTextCatClassifierService.LoadRequiredAsync();

        var actualLanguage = _nTextCatClassifierService.ClassifyText(inputText);

        Assert.Equal(expectedLanguage, actualLanguage);
    }

    [Fact]
    public void ClassifyText_English_shouldReturnEnglish()
    {
        var inputText = "This is a text in English.";
        var expectedLanguage = Language.English;
        
        _ = _nTextCatClassifierService.LoadRequiredAsync();

        var actualLanguage = _nTextCatClassifierService.ClassifyText(inputText);

        Assert.Equal(expectedLanguage, actualLanguage);
    }

    [Fact]
    public void ClassifyTextWithProbabilities_French_shouldReturnFrench()
    {
        var inputText = "Ceci est un texte en français.";
        var expectedLanguage = Language.French;
        
        _ = _nTextCatClassifierService.LoadRequiredAsync();

        var actualLanguage = _nTextCatClassifierService.ClassifyTextWithProbabilities(inputText);

        Assert.Equal(expectedLanguage, actualLanguage.FirstOrDefault().language);
    }

    // [Fact]
    // public void ClassifyTextWithProbabilities_English_shouldReturnEnglish()
    // {
    //     var inputText = "This is a text in English.";
    //     var expectedLanguage = Language.English;
    //
    //     var actualLanguage = _nTextCatClassifierService.ClassifyTextWithProbabilities(inputText);
    //
    //     ;
    // }
}