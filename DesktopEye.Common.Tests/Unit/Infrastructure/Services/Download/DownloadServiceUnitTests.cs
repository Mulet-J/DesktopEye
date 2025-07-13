using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using DesktopEye.Common.Infrastructure.Services.Download;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace DesktopEye.Common.Tests.Unit.Infrastructure.Services.Download;

public class DownloadServiceUnitTests : IDisposable
{
    private readonly Mock<ILogger<DownloadService>> _mockLogger;
    private readonly Mock<Bugsnag.IClient> _mockBugsnag;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly DownloadService _downloadService;
    private readonly string _testDirectory;

    public DownloadServiceUnitTests()
    {
        var mockHttpClientFactory =
            // Arrange - Configuration des mocks
            new Mock<IHttpClientFactory>();
        _mockLogger = new Mock<ILogger<DownloadService>>();
        _mockBugsnag = new Mock<Bugsnag.IClient>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        // Configuration du HttpClient mocké
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        mockHttpClientFactory
            .Setup(x => x.CreateClient("DesktopEyeClient"))
            .Returns(_httpClient);

        // Configuration du répertoire de test temporaire
        _testDirectory = Path.Combine(Path.GetTempPath(), "DownloadServiceTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _downloadService = new DownloadService(
            mockHttpClientFactory.Object,
            _mockLogger.Object,
            _mockBugsnag.Object);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        
        try
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ValidParameters_InitializesSuccessfully()
    {
        // Act & Assert - Constructor should not throw
        Assert.NotNull(_downloadService);
    }

    #endregion

    #region Successful Download Tests

    [Fact]
    public async Task DownloadFileAsync_SuccessfulDownload_ReturnsTrue()
    {
        // Arrange
        var url = "https://example.com/test.txt";
        var destinationPath = Path.Combine(_testDirectory, "test.txt");
        var content = "Test file content";

        SetupSuccessfulHttpResponse(content);

        // Act
        var result = await _downloadService.DownloadFileAsync(url, destinationPath);

        // Assert
        Assert.True(result);
        Assert.True(File.Exists(destinationPath));
        
        var downloadedContent = await File.ReadAllTextAsync(destinationPath);
        Assert.Equal(content, downloadedContent);
    }

    [Fact]
    public async Task DownloadFileAsync_CreatesDirectoryIfNotExists_ReturnsTrue()
    {
        // Arrange
        var url = "https://example.com/test.txt";
        var subDirectory = Path.Combine(_testDirectory, "subdirectory");
        var destinationPath = Path.Combine(subDirectory, "test.txt");
        var content = "Test content";

        SetupSuccessfulHttpResponse(content);

        // Act
        var result = await _downloadService.DownloadFileAsync(url, destinationPath);

        // Assert
        Assert.True(result);
        Assert.True(Directory.Exists(subDirectory));
        Assert.True(File.Exists(destinationPath));
    }

    [Fact]
    public async Task DownloadFileAsync_LargeFile_HandlesCorrectly()
    {
        // Arrange
        var url = "https://example.com/largefile.txt";
        var destinationPath = Path.Combine(_testDirectory, "largefile.txt");
        
        // Simuler un fichier de 1MB
        var largeContent = new string('A', 1024 * 1024);
        SetupSuccessfulHttpResponse(largeContent);

        // Act
        var result = await _downloadService.DownloadFileAsync(url, destinationPath);

        // Assert
        Assert.True(result);
        Assert.True(File.Exists(destinationPath));
        
        var fileInfo = new FileInfo(destinationPath);
        Assert.Equal(largeContent.Length, fileInfo.Length);
    }

    #endregion

    #region HTTP Error Tests

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task DownloadFileAsync_HttpError_ReturnsFalse(HttpStatusCode statusCode)
    {
        // Arrange
        var url = "https://example.com/test.txt";
        var destinationPath = Path.Combine(_testDirectory, "test.txt");

        SetupHttpResponseWithStatusCode(statusCode);

        // Act
        var result = await _downloadService.DownloadFileAsync(url, destinationPath);

        // Assert
        Assert.False(result);
        Assert.False(File.Exists(destinationPath));
        VerifyLogLevel(LogLevel.Error, Times.Once());
        VerifyBugsnagNotified(Times.Once());
    }

    [Fact]
    public async Task DownloadFileAsync_HttpRequestException_ReturnsFalse()
    {
        // Arrange
        var url = "https://example.com/test.txt";
        var destinationPath = Path.Combine(_testDirectory, "test.txt");

        SetupHttpRequestException("Network error");

        // Act
        var result = await _downloadService.DownloadFileAsync(url, destinationPath);

        // Assert
        Assert.False(result);
        Assert.False(File.Exists(destinationPath));
        VerifyLogLevel(LogLevel.Error, Times.Once());
        VerifyBugsnagNotified(Times.Once());
    }

    [Fact]
    public async Task DownloadFileAsync_TaskCancelledException_ReturnsFalse()
    {
        // Arrange
        var url = "https://example.com/test.txt";
        var destinationPath = Path.Combine(_testDirectory, "test.txt");

        SetupTaskCancelledException();

        // Act
        var result = await _downloadService.DownloadFileAsync(url, destinationPath);

        // Assert
        Assert.False(result);
        Assert.False(File.Exists(destinationPath));
        VerifyLogLevel(LogLevel.Error, Times.Once());
        VerifyBugsnagNotified(Times.Once());
    }

    #endregion

    #region File System Error Tests

    [Fact]
    public async Task DownloadFileAsync_UnauthorizedAccess_ReturnsFalse()
    {
        // Arrange
        var url = "https://example.com/test.txt";
        var restrictedPath = GetRestrictedPath();
        
        if (string.IsNullOrEmpty(restrictedPath))
        {
            // Skip test if we can't determine a restricted path for this platform
            return;
        }

        SetupSuccessfulHttpResponse("test content");

        // Act
        var result = await _downloadService.DownloadFileAsync(url, restrictedPath);

        // Assert
        Assert.False(result);
        VerifyLogLevel(LogLevel.Error, Times.Once());
        VerifyBugsnagNotified(Times.Once());
    }

    [Fact]
    public async Task DownloadFileAsync_FileInUse_ReturnsFalse()
    {
        // Arrange
        var url = "https://example.com/test.txt";
        var destinationPath = Path.Combine(_testDirectory, "locked_file.txt");
        
        SetupSuccessfulHttpResponse("test content");

        // Créer et verrouiller le fichier
        using var lockedFileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

        // Act
        var result = await _downloadService.DownloadFileAsync(url, destinationPath);

        // Assert
        Assert.False(result);
        VerifyLogLevel(LogLevel.Error, Times.Once());
        VerifyBugsnagNotified(Times.Once());
    }

    [Fact]
    public async Task DownloadFileAsync_InvalidPath_ReturnsFalse()
    {
        // Arrange
        var url = "https://example.com/test.txt";
        var invalidPath = GetInvalidPath();

        SetupSuccessfulHttpResponse("test content");

        // Act
        var result = await _downloadService.DownloadFileAsync(url, invalidPath);

        // Assert
        Assert.False(result);
        VerifyLogLevel(LogLevel.Error, Times.Once());
        VerifyBugsnagNotified(Times.Once());
    }

    #endregion

    #region Platform-Specific Tests

    [Fact]
    public async Task DownloadFileAsync_UnixPlatform_SetsExecutablePermissions()
    {
        // Arrange
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return; // Skip on Windows
        }

        var url = "https://example.com/script.sh";
        var destinationPath = Path.Combine(_testDirectory, "script.sh");
        var content = "#!/bin/bash\necho 'test'";

        SetupSuccessfulHttpResponse(content);

        // Act
        var result = await _downloadService.DownloadFileAsync(url, destinationPath);

        // Assert
        Assert.True(result);
        Assert.True(File.Exists(destinationPath));
        
        // Vérifier que les permissions d'exécution sont définies
        var fileMode = File.GetUnixFileMode(destinationPath);
        Assert.True(fileMode.HasFlag(UnixFileMode.UserExecute));
    }

    [Fact]
    public async Task DownloadFileAsync_WindowsPlatform_DoesNotSetExecutablePermissions()
    {
        // Arrange
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return; // Skip on non-Windows
        }

        var url = "https://example.com/file.exe";
        var destinationPath = Path.Combine(_testDirectory, "file.exe");
        var content = "dummy executable content";

        SetupSuccessfulHttpResponse(content);

        // Act
        var result = await _downloadService.DownloadFileAsync(url, destinationPath);

        // Assert
        Assert.True(result);
        Assert.True(File.Exists(destinationPath));
        
        // Sur Windows, on vérifie juste que le fichier existe
        // Le service ne modifie pas les permissions sur Windows
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task DownloadFileAsync_EmptyResponse_CreatesEmptyFile()
    {
        // Arrange
        var url = "https://example.com/empty.txt";
        var destinationPath = Path.Combine(_testDirectory, "empty.txt");

        SetupSuccessfulHttpResponse("");

        // Act
        var result = await _downloadService.DownloadFileAsync(url, destinationPath);

        // Assert
        Assert.True(result);
        Assert.True(File.Exists(destinationPath));
        
        var content = await File.ReadAllTextAsync(destinationPath);
        Assert.Empty(content);
    }

    [Fact]
    public async Task DownloadFileAsync_SpecialCharactersInPath_HandlesCorrectly()
    {
        // Arrange
        var url = "https://example.com/test.txt";
        var fileName = "test file with spaces & special chars.txt";
        var destinationPath = Path.Combine(_testDirectory, fileName);
        var content = "Test content";

        SetupSuccessfulHttpResponse(content);

        // Act
        var result = await _downloadService.DownloadFileAsync(url, destinationPath);

        // Assert
        Assert.True(result);
        Assert.True(File.Exists(destinationPath));
    }

    [Fact]
    public async Task DownloadFileAsync_LongPath_HandlesCorrectly()
    {
        // Arrange
        var url = "https://example.com/test.txt";
        var longDirectoryName = new string('a', 100);
        var longDirectory = Path.Combine(_testDirectory, longDirectoryName);
        var destinationPath = Path.Combine(longDirectory, "test.txt");
        var content = "Test content";

        SetupSuccessfulHttpResponse(content);

        // Act
        var result = await _downloadService.DownloadFileAsync(url, destinationPath);

        // Assert
        Assert.True(result);
        Assert.True(File.Exists(destinationPath));
    }

    #endregion

    #region Helper Methods

    private void SetupSuccessfulHttpResponse(string content)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void SetupHttpResponseWithStatusCode(HttpStatusCode statusCode)
    {
        var response = new HttpResponseMessage(statusCode);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void SetupHttpRequestException(string message)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException(message));
    }

    private void SetupTaskCancelledException()
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timed out"));
    }

    private void VerifyLogLevel(LogLevel level, Times times)
    {
        _mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    private void VerifyBugsnagNotified(Times times)
    {
        _mockBugsnag.Verify(
            x => x.Notify(It.IsAny<Exception>()),
            times);
    }

    private static string GetRestrictedPath()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? @"C:\Windows\System32\test.txt" 
            : "/root/test.txt";
    }

    private static string GetInvalidPath()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "C:\\Invalid|Path\\file.txt"
            : "/invalid\0path/file.txt";
    }

    #endregion
}