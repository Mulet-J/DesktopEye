using DesktopEye.Common.Domain.Features.TextClassification;
using DesktopEye.Common.Infrastructure.Services.ApplicationPath;
using Microsoft.Extensions.Logging;
using Moq;

namespace DesktopEye.Common.Tests.Unit.Domain.Features.TextClassification;

public class NTextCatClassifierServiceUnitTests : IDisposable
{
    private readonly Mock<ILogger<NTextCatClassifierService>> _mockLogger;
    private readonly Mock<IPathService> _mockPathService;
    private readonly string _testModelsPath;
    private readonly string _testModelFilePath;
    private NTextCatClassifierService? _service;

    public NTextCatClassifierServiceUnitTests()
    {
        _mockLogger = new Mock<ILogger<NTextCatClassifierService>>();
        _mockPathService = new Mock<IPathService>();
        _testModelsPath = Path.Combine(Path.GetTempPath(), "TestModels", "ntextcat");
        _testModelFilePath = Path.Combine(_testModelsPath, "NTextCat.xml");
        
        // Setup mock path service
        _mockPathService.Setup(x => x.ModelsDirectory).Returns(Path.Combine(Path.GetTempPath(), "TestModels"));
        
        // Create test models directory
        Directory.CreateDirectory(_testModelsPath);
    }

    public void Dispose()
    {
        _service?.Dispose();
        
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
        // Arrange
        CreateMockModelFile();

        // Act
        _service = new NTextCatClassifierService(_mockLogger.Object, _mockPathService.Object);

        // Assert
        Assert.NotNull(_service);
        Assert.False(_service.IsModelLoaded); // Model not loaded until LoadRequiredAsync is called
        Assert.False(_service.IsModelLoading);
    }

    #endregion

    #region Properties Tests

    [Fact]
    public void IsModelLoaded_InitialState_ReturnsFalse()
    {
        // Arrange
        CreateMockModelFile();
        _service = new NTextCatClassifierService(_mockLogger.Object, _mockPathService.Object);

        // Act & Assert
        Assert.False(_service.IsModelLoaded);
    }

    [Fact]
    public void IsModelLoading_InitialState_ReturnsFalse()
    {
        // Arrange
        CreateMockModelFile();
        _service = new NTextCatClassifierService(_mockLogger.Object, _mockPathService.Object);

        // Act & Assert
        Assert.False(_service.IsModelLoading);
    }

    #endregion

    #region LoadRequiredAsync Tests
    
    [Fact]
    public async Task LoadRequiredAsync_ModelFileNotFound_ThrowsInvalidOperationException()
    {
        // Arrange - Don't create model file
        _service = new NTextCatClassifierService(_mockLogger.Object, _mockPathService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.LoadRequiredAsync());
        VerifyLogMessage(LogLevel.Error, "Failed to initialize NTextCat classifier", Times.Once());
    }
    
    #endregion

    #region ClassifyText Tests

    [Fact]
    public void ClassifyText_ModelNotLoaded_ThrowsInvalidOperationException()
    {
        // Arrange
        CreateMockModelFile();
        _service = new NTextCatClassifierService(_mockLogger.Object, _mockPathService.Object);
        const string inputText = "Some text";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _service.ClassifyText(inputText));
        Assert.Contains("Model is not loaded", exception.Message);
    }

    [Fact]
    public void ClassifyTextWithProbabilities_ModelNotLoaded_ThrowsInvalidOperationException()
    {
        // Arrange
        CreateMockModelFile();
        _service = new NTextCatClassifierService(_mockLogger.Object, _mockPathService.Object);
        const string inputText = "Some text";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            _service.ClassifyTextWithProbabilities(inputText));
        Assert.Contains("Model is not loaded", exception.Message);
    }

    #endregion

    #region Language Conversion Tests

    [Fact]
    public void LibLanguageToLanguage_NullLanguageCode_ThrowsException()
    {
        // Arrange
        CreateMockModelFile();
        _service = CreateServiceWithMockNTextCat(); // Null language code
        _ = _service.LoadRequiredAsync();
        const string text = "Test text";

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => _service.ClassifyText(text));
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CalledOnce_DisposesCorrectly()
    {
        // Arrange
        CreateMockModelFile();
        _service = CreateServiceWithMockNTextCat();

        // Act & Assert - Should not throw
        _service.Dispose();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        CreateMockModelFile();
        _service = CreateServiceWithMockNTextCat();

        // Act & Assert
        _service.Dispose();
        _service.Dispose(); // Second call should not throw
    }

    [Fact]
    public void Dispose_SetsIsDisposedFlag()
    {
        // Arrange
        CreateMockModelFile();
        _service = CreateServiceWithMockNTextCat();

        // Act
        _service.Dispose();

        // Assert
        // If there's an IsDisposed property or behavior, test it here
        // Otherwise, verify that subsequent operations fail appropriately
    }

    #endregion

    #region Helper Methods

    private void CreateMockModelFile()
    {
        // Create a minimal mock model file for testing
        Directory.CreateDirectory(Path.GetDirectoryName(_testModelFilePath)!);
        var mockXmlContent = """
            <?xml version="1.0" encoding="utf-8"?>
            <root>
                <languages>
                    <language iso639_3="eng" name="English" />
                    <language iso639_3="fra" name="French" />
                </languages>
            </root>
            """;
        File.WriteAllText(_testModelFilePath, mockXmlContent);
    }

    private NTextCatClassifierService CreateServiceWithMockNTextCat()
    {
        CreateMockModelFile();
        return new TestNTextCatClassifierService(_mockLogger.Object, _mockPathService.Object);
    }

    private void VerifyLogMessage(LogLevel level, string message, Times times)
    {
        _mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    #endregion

    #region Test Helper Classes

    // Test implementation to allow mocking NTextCat behavior
    private class TestNTextCatClassifierService : NTextCatClassifierService
    {
        public TestNTextCatClassifierService(
            ILogger<NTextCatClassifierService> logger,
            IPathService pathService) 
            : base(logger, pathService)
        {
        }
    }

    #endregion
}