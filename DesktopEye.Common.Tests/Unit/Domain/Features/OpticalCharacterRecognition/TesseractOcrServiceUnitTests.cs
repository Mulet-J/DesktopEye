using DesktopEye.Common.Domain.Features.OpticalCharacterRecognition;
using DesktopEye.Common.Infrastructure.Exceptions;
using DesktopEye.Common.Infrastructure.Services.ApplicationPath;
using Microsoft.Extensions.Logging;
using Moq;
using Language = DesktopEye.Common.Domain.Models.Language;

namespace DesktopEye.Common.Tests.Unit.Domain.Features.OpticalCharacterRecognition;

public class TesseractOcrServiceUnitTests : IDisposable
{
    private readonly Mock<ILogger<TesseractOcrService>> _mockLogger;
    private readonly Mock<IPathService> _mockPathService;
    private readonly TesseractOcrService _service;
    private readonly string _testModelsPath;

    public TesseractOcrServiceUnitTests()
    {
        _mockLogger = new Mock<ILogger<TesseractOcrService>>();
        _mockPathService = new Mock<IPathService>();
        _testModelsPath = Path.Combine(Path.GetTempPath(), "TestModels", "tesseract");
        
        // Setup mock path service
        _mockPathService.Setup(x => x.ModelsDirectory).Returns(Path.Combine(Path.GetTempPath(), "TestModels"));
        
        // Create test models directory
        Directory.CreateDirectory(_testModelsPath);
        
        _service = new TesseractOcrService(_mockPathService.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _service.Dispose();
        
        // Cleanup test directory
        if (Directory.Exists(_testModelsPath))
        {
            Directory.Delete(_testModelsPath, true);
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ValidParameters_CreatesService()
    {
        // Arrange & Act
        var service = new TesseractOcrService(_mockPathService.Object, _mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region Language Conversion Tests

    [Theory]
    [InlineData(TesseractOCR.Enums.Language.English, Language.English)]
    [InlineData(TesseractOCR.Enums.Language.French, Language.French)]
    [InlineData(TesseractOCR.Enums.Language.German, Language.German)]
    [InlineData(TesseractOCR.Enums.Language.Japanese, Language.Japanese)]
    [InlineData(TesseractOCR.Enums.Language.Korean, Language.Korean)]
    [InlineData(TesseractOCR.Enums.Language.Portuguese, Language.Portuguese)]
    [InlineData(TesseractOCR.Enums.Language.Italian, Language.Italian)]
    [InlineData(TesseractOCR.Enums.Language.Dutch, Language.Dutch)]
    [InlineData(TesseractOCR.Enums.Language.Russian, Language.Russian)]
    [InlineData(TesseractOCR.Enums.Language.Swedish, Language.Swedish)]
    [InlineData(TesseractOCR.Enums.Language.Norwegian, Language.Norwegian)]
    [InlineData(TesseractOCR.Enums.Language.Danish, Language.Danish)]
    public void LibLanguageToLanguage_ValidLanguage_ReturnsCorrectMapping(
        TesseractOCR.Enums.Language input, 
        Language expected)
    {
        // Act
        var result = TesseractOcrService.LibLanguageToLanguage(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void LibLanguageToLanguage_UnsupportedLanguage_ThrowsLanguageException()
    {
        // Arrange
        const TesseractOCR.Enums.Language unsupportedLanguage = (TesseractOCR.Enums.Language)999;

        // Act & Assert
        Assert.Throws<LanguageException>(() => 
            TesseractOcrService.LibLanguageToLanguage(unsupportedLanguage));
    }

    [Theory]
    [InlineData(Language.English, TesseractOCR.Enums.Language.English)]
    [InlineData(Language.French, TesseractOCR.Enums.Language.French)]
    [InlineData(Language.German, TesseractOCR.Enums.Language.German)]
    [InlineData(Language.Spanish, TesseractOCR.Enums.Language.SpanishCastilian)]
    [InlineData(Language.Chinese, TesseractOCR.Enums.Language.ChineseSimplified)]
    public void LanguageToLibLanguage_ValidLanguage_ReturnsCorrectMapping(
        Language input, 
        TesseractOCR.Enums.Language expected)
    {
        // Act
        var result = TesseractOcrService.LanguageToLibLanguage(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void LanguageToLibLanguage_UnsupportedLanguage_ThrowsLanguageException()
    {
        // Arrange
        var unsupportedLanguage = (Language)999;

        // Act & Assert
        Assert.Throws<LanguageException>(() => 
            TesseractOcrService.LanguageToLibLanguage(unsupportedLanguage));
    }

    [Fact]
    public void LanguageToLibLanguage_MultipleLanguages_ReturnsCorrectList()
    {
        // Arrange
        var input = new List<Language> { Language.English, Language.French, Language.German };
        var expected = new List<TesseractOCR.Enums.Language> 
        { 
            TesseractOCR.Enums.Language.English, 
            TesseractOCR.Enums.Language.French, 
            TesseractOCR.Enums.Language.German 
        };

        // Act
        var result = TesseractOcrService.LanguageToLibLanguage(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void LanguageToLibLanguage_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        var input = new List<Language>();

        // Act
        var result = TesseractOcrService.LanguageToLibLanguage(input);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region TSV Parsing Tests
    

    [Fact]
    public void ParseTsvString_EmptyString_ReturnsEmptyList()
    {
        // Arrange
        var tsvContent = "";

        // Act
        var result = TesseractOcrService.ParseTsvString(tsvContent);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseTsvString_OnlyHeader_ReturnsEmptyList()
    {
        // Arrange
        var tsvContent = "level	page_num	block_num	par_num	line_num	word_num	left	top	width	height	conf	text";

        // Act
        var result = TesseractOcrService.ParseTsvString(tsvContent);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseTsvLine_EmptyLine_ReturnsNull()
    {
        // Arrange
        var line = "";

        // Act
        var result = TesseractOcrService.ParseTsvLine(line);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseTsvLine_WhitespaceLine_ReturnsNull()
    {
        // Arrange
        var line = "   \t   ";

        // Act
        var result = TesseractOcrService.ParseTsvLine(line);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseTsvLine_InsufficientColumns_ReturnsNull()
    {
        // Arrange
        var line = "5	1	1	1	1";

        // Act
        var result = TesseractOcrService.ParseTsvLine(line);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseTsvLine_InvalidNumericValues_ReturnsNull()
    {
        // Arrange
        var line = "5	1	1	1	1	1	abc	200	50	20	95.5	Hello";

        // Act
        var result = TesseractOcrService.ParseTsvLine(line);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseTsvLine_NegativeDimensions_ReturnsNull()
    {
        // Arrange
        var line = "5	1	1	1	1	1	100	200	-50	20	95.5	Hello";

        // Act
        var result = TesseractOcrService.ParseTsvLine(line);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseTsvLine_ZeroDimensions_ReturnsNull()
    {
        // Arrange
        var line = "5	1	1	1	1	1	100	200	0	20	95.5	Hello";

        // Act
        var result = TesseractOcrService.ParseTsvLine(line);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseTsvLine_EmptyText_ReturnsNull()
    {
        // Arrange
        var line = "5	1	1	1	1	1	100	200	50	20	95.5	";

        // Act
        var result = TesseractOcrService.ParseTsvLine(line);

        // Assert
        Assert.Null(result);
    }

    #endregion
    
    #region Input Validation Tests

    [Fact]
    public async Task GetTextFromBitmapAsync_NullBitmap_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.GetTextFromBitmapAsync(null!));
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var service = new TesseractOcrService(_mockPathService.Object, _mockLogger.Object);

        // Act & Assert
        service.Dispose();
        service.Dispose(); // Should not throw
    }

    #endregion

    #region Helper Methods

    #endregion
}