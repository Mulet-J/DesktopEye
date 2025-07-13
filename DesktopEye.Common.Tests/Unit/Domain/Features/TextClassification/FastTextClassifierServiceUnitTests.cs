using DesktopEye.Common.Domain.Features.TextClassification;
using DesktopEye.Common.Infrastructure.Services.ApplicationPath;
using Microsoft.Extensions.Logging;
using Moq;

namespace DesktopEye.Common.Tests.Unit.Domain.Features.TextClassification;

public class FastTextClassifierServiceUnitTests : IDisposable
{
    private readonly Mock<ILogger<FastTextClassifierService>> _mockLogger;
    private readonly Mock<IPathService> _mockPathService;
    private readonly string _testModelsPath;
    private readonly string _testModelFilePath;
    private FastTextClassifierService? _service;

    public FastTextClassifierServiceUnitTests()
    {
        _mockLogger = new Mock<ILogger<FastTextClassifierService>>();
        _mockPathService = new Mock<IPathService>();
        _testModelsPath = Path.Combine(Path.GetTempPath(), "TestModels", "FastText");
        _testModelFilePath = Path.Combine(_testModelsPath, "model.bin");
        
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
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new FastTextClassifierService(_mockPathService.Object, null!));
    }

    [Fact]
    public void Constructor_ModelFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange - Don't create model file

        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(() => 
            new FastTextClassifierService(_mockPathService.Object, _mockLogger.Object));
        
        Assert.Contains("FastText model file not found", exception.Message);
    }

    [Fact]
    public void Constructor_ModelLoadingFails_LogsErrorAndThrows()
    {
        // Arrange
        CreateInvalidModelFile(); // Create file but with invalid content

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => 
            new FastTextClassifierService(_mockPathService.Object, _mockLogger.Object));
        
        VerifyLogMessage(LogLevel.Error, "Failed to load FastText model", Times.Once());
    }

    #endregion

    #region Helper Methods

    private void CreateInvalidModelFile()
    {
        // Create an invalid model file for testing error scenarios
        Directory.CreateDirectory(Path.GetDirectoryName(_testModelFilePath)!);
        File.WriteAllText(_testModelFilePath, "invalid model content");
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
}