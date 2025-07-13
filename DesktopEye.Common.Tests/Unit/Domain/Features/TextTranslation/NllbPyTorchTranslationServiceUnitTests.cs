using DesktopEye.Common.Domain.Features.TextTranslation;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Infrastructure.Services.ApplicationPath;
using DesktopEye.Common.Infrastructure.Services.Conda;
using DesktopEye.Common.Infrastructure.Services.Python;
using Microsoft.Extensions.Logging;
using Moq;

namespace DesktopEye.Common.Tests.Unit.Domain.Features.TextTranslation;

public class NllbPyTorchTranslationServiceUnitTests : IDisposable
{
    private readonly Mock<ICondaService> _mockCondaService;
    private readonly Mock<IPathService> _mockPathService;
    private readonly Mock<IPythonRuntimeManager> _mockRuntimeManager;
    private readonly Mock<ILogger<NllbPyTorchTranslationService>> _mockLogger;
    private NllbPyTorchTranslationService? _service;

    public NllbPyTorchTranslationServiceUnitTests()
    {
        _mockCondaService = new Mock<ICondaService>();
        _mockPathService = new Mock<IPathService>();
        _mockRuntimeManager = new Mock<IPythonRuntimeManager>();
        _mockLogger = new Mock<ILogger<NllbPyTorchTranslationService>>();

        // Setup default mock behaviors
        _mockPathService.Setup(x => x.ModelsDirectory).Returns("/test/models");
        _mockRuntimeManager.Setup(x => x.IsRuntimeInitialized).Returns(true);
    }

    public void Dispose()
    {
        _service?.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ValidParameters_CreatesService()
    {
        // Act
        _service = new NllbPyTorchTranslationService(
            _mockCondaService.Object,
            _mockPathService.Object,
            _mockRuntimeManager.Object,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(_service);
        VerifyLogMessage(LogLevel.Information, "Initializing NllbPyTorchTranslationService", Times.Once());
        VerifyLogMessage(LogLevel.Debug, "Starting Python runtime for translation service", Times.Once());
        _mockRuntimeManager.Verify(x => x.StartRuntime(_service), Times.Once());
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new NllbPyTorchTranslationService(
                _mockCondaService.Object,
                _mockPathService.Object,
                _mockRuntimeManager.Object,
                null!));
    }

    [Fact]
    public void Constructor_RuntimeStartupFails_ThrowsException()
    {
        // Arrange
        _mockRuntimeManager
            .Setup(x => x.StartRuntime(It.IsAny<object>()))
            .Throws(new InvalidOperationException("Runtime startup failed"));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            new NllbPyTorchTranslationService(
                _mockCondaService.Object,
                _mockPathService.Object,
                _mockRuntimeManager.Object,
                _mockLogger.Object));

        VerifyLogMessage(LogLevel.Error, "Failed to initialize NllbPyTorchTranslationService", Times.Once());
    }

    #endregion

    #region LoadRequiredAsync Tests

    [Fact]
    public async Task LoadRequiredAsync_AlreadyInitialized_ReturnsTrue()
    {
        // Arrange
        _service = CreateServiceWithMockModels();
        await _service.LoadRequiredAsync(); // First call

        // Act
        var result = await _service.LoadRequiredAsync(); // Second call

        // Assert
        Assert.True(result);
        VerifyLogMessage(LogLevel.Debug, "Model already initialized, skipping preload", Times.Once());
    }

    [Fact]
    public async Task LoadRequiredAsync_ConcurrentCalls_HandledCorrectly()
    {
        // Arrange
        _service = CreateServiceWithMockModels();

        // Act
        var task1 = _service.LoadRequiredAsync();
        var task2 = _service.LoadRequiredAsync();
        var results = await Task.WhenAll(task1, task2);

        // Assert
        Assert.True(results[0]);
        Assert.True(results[1]);
        VerifyLogMessage(LogLevel.Information, "Model preloading completed successfully", Times.Once());
    }

    [Fact]
    public async Task LoadRequiredAsync_WithCustomModelName_UsesCustomModel()
    {
        // Arrange
        _service = CreateServiceWithMockModels();
        const string customModelName = "custom/nllb-model";

        // Act
        var result = await _service.LoadRequiredAsync(customModelName);

        // Assert
        Assert.True(result);
        VerifyLogMessage(LogLevel.Information, $"Starting async model preloading for: {customModelName}", Times.Once());
    }

    #endregion

    #region LoadRequired Synchronous Tests

    [Fact]
    public void LoadRequired_CallsAsyncVersion()
    {
        // Arrange
        _service = CreateServiceWithMockModels();

        // Act
        var result = _service.LoadRequired();

        // Assert
        Assert.True(result);
        VerifyLogMessage(LogLevel.Information, "Loading required components synchronously", Times.Once());
        VerifyLogMessage(LogLevel.Warning, "Synchronous LoadRequired is deprecated", Times.Once());
    }

    #endregion

    #region Translation Tests

    [Fact]
    public void Translate_EmptyText_ReturnsEmptyString()
    {
        // Arrange
        _service = CreateServiceWithMockModels();

        // Act
        var result = _service.Translate("", Language.English, Language.French);

        // Assert
        Assert.Equal(string.Empty, result);
        VerifyLogMessage(LogLevel.Warning, "Translation requested with empty or null text", Times.Once());
    }

    [Fact]
    public void Translate_NullText_ReturnsEmptyString()
    {
        // Arrange
        _service = CreateServiceWithMockModels();

        // Act
        var result = _service.Translate(null!, Language.English, Language.French);

        // Assert
        Assert.Equal(string.Empty, result);
        VerifyLogMessage(LogLevel.Warning, "Translation requested with empty or null text", Times.Once());
    }

    [Fact]
    public void Translate_WhitespaceOnlyText_ReturnsEmptyString()
    {
        // Arrange
        _service = CreateServiceWithMockModels();

        // Act
        var result = _service.Translate("   \t\n   ", Language.English, Language.French);

        // Assert
        Assert.Equal(string.Empty, result);
        VerifyLogMessage(LogLevel.Warning, "Translation requested with empty or null text", Times.Once());
    }

    [Fact]
    public void Translate_TokenizerNull_ThrowsInvalidOperationException()
    {
        // Arrange
        _service = CreateServiceWithNullTokenizer();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            _service.Translate("test", Language.English, Language.French));
        Assert.Contains("Tokenizer is not loaded", exception.Message);
        VerifyLogMessage(LogLevel.Error, "Tokenizer is null, cannot perform translation", Times.Once());
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CalledOnce_StopsRuntimeCorrectly()
    {
        // Arrange
        _service = CreateServiceWithMockModels();

        // Act
        _service.Dispose();

        // Assert
        _mockRuntimeManager.Verify(x => x.StopRuntime(_service), Times.Once());
        VerifyLogMessage(LogLevel.Debug, "Disposing NllbPyTorchTranslationService", Times.Once());
        VerifyLogMessage(LogLevel.Debug, "Stopping Python runtime", Times.Once());
        VerifyLogMessage(LogLevel.Information, "Python runtime stopped successfully", Times.Once());
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        _service = CreateServiceWithMockModels();

        // Act & Assert
        _service.Dispose();
        _service.Dispose(); // Second call should not throw
    }

    [Fact]
    public void Dispose_RuntimeStopFails_LogsError()
    {
        // Arrange
        _service = CreateServiceWithMockModels();
        _mockRuntimeManager
            .Setup(x => x.StopRuntime(It.IsAny<object>()))
            .Throws(new InvalidOperationException("Runtime stop failed"));

        // Act
        _service.Dispose();

        // Assert
        VerifyLogMessage(LogLevel.Error, "Error occurred while stopping Python runtime during disposal", Times.Once());
    }

    #endregion

    #region Helper Methods

    private TestNllbPyTorchTranslationService CreateServiceWithMockModels(string? translationResult = "Mock translation")
    {
        return new TestNllbPyTorchTranslationService(
            _mockCondaService.Object,
            _mockPathService.Object,
            _mockRuntimeManager.Object,
            _mockLogger.Object,
            translationResult: translationResult);
    }
    
    private TestNllbPyTorchTranslationService CreateServiceWithNullTokenizer()
    {
        return new TestNllbPyTorchTranslationService(
            _mockCondaService.Object,
            _mockPathService.Object,
            _mockRuntimeManager.Object,
            _mockLogger.Object,
            nullTokenizer: true);
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

    // Test implementation to allow mocking NLLB PyTorch behavior
    private class TestNllbPyTorchTranslationService : NllbPyTorchTranslationService
    {
        private readonly string? _translationResult;
        private readonly bool _failTokenizerLoad;
        private readonly bool _failModelLoad;
        private readonly bool _nullTokenizer;
        private readonly bool _nullModel;
        private readonly bool _failTranslation;
        private bool _isInitialized;
        private dynamic? _mockTokenizer;
        private dynamic? _mockModel;

        public TestNllbPyTorchTranslationService(
            ICondaService condaService,
            IPathService pathService,
            IPythonRuntimeManager runtimeManager,
            ILogger<NllbPyTorchTranslationService> logger,
            string? translationResult = null,
            bool failTokenizerLoad = false,
            bool failModelLoad = false,
            bool nullTokenizer = false,
            bool nullModel = false,
            bool failTranslation = false) 
            : base(condaService, pathService, runtimeManager, logger)
        {
            _translationResult = translationResult;
            _failTokenizerLoad = failTokenizerLoad;
            _failModelLoad = failModelLoad;
            _nullTokenizer = nullTokenizer;
            _nullModel = nullModel;
            _failTranslation = failTranslation;
        }
    }

    #endregion
}