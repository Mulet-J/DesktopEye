using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using DesktopEye.Common.Services.Download;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace DesktopEye.Common.Tests.Services.Download;

public class DownloadServiceTests : IDisposable
{
    private readonly Mock<ILogger<DownloadService>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly DownloadService _downloadService;
    private readonly string _tempDirectory;

    public DownloadServiceTests()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("DesktopEyeClient", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "DesktopEye/1.0");
        });
    
        // Build the service provider and get the HttpClientFactory
        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var bugsnag = serviceProvider.GetRequiredService<Bugsnag.IClient>();
        _mockLogger = new Mock<ILogger<DownloadService>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = httpClientFactory.CreateClient("DesktopEyeClient");
        _downloadService = new DownloadService(httpClientFactory,_mockLogger.Object, bugsnag);
        
        // Create a temporary directory for test files
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        
        // Clean up temp directory
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    #region HTTP Error Tests

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task DownloadFileAsync_HttpError_ReturnsFalse(HttpStatusCode statusCode)
    {
        // Arrange
        var url = "https://example.com/test.txt";
        var destinationPath = Path.Combine(_tempDirectory, "test.txt");

        SetupHttpResponseWithStatusCode(statusCode);

        // Act
        var result = await _downloadService.DownloadFileAsync(url, destinationPath);

        // Assert
        Assert.False(result);
        VerifyLogLevel(LogLevel.Error, Times.Once());
    }

    [Fact]
    public async Task DownloadFileAsync_HttpRequestException_ReturnsFalse()
    {
        // Arrange
        var url = "https://example.com/test.txt";
        var destinationPath = Path.Combine(_tempDirectory, "test.txt");

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _downloadService.DownloadFileAsync(url, destinationPath);

        // Assert
        Assert.False(result);
        VerifyLogLevel(LogLevel.Error, Times.Once());
    }

    #endregion

    #region File System Error Tests

    [Fact]
    public async Task DownloadFileAsync_UnauthorizedAccess_ReturnsFalse()
    {
        // Arrange
        var url = "https://example.com/test.txt";
        var restrictedPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? @"C:\Windows\System32\test.txt" 
            : "/root/test.txt";

        SetupSuccessfulHttpResponse("test content");

        // Act
        var result = await _downloadService.DownloadFileAsync(url, restrictedPath);

        // Assert
        Assert.False(result);
        VerifyLogLevel(LogLevel.Error, Times.Once());
    }

    [Fact]
    public async Task DownloadFileAsync_FileInUse_ReturnsFalse()
    {
        // Arrange
        var url = "https://example.com/test.txt";
        var destinationPath = Path.Combine(_tempDirectory, "locked_file.txt");
        
        // Create and lock the file
        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        
        SetupSuccessfulHttpResponse("test content");

        // Act
        var result = await _downloadService.DownloadFileAsync(url, destinationPath);

        // Assert
        Assert.False(result);
        VerifyLogLevel(LogLevel.Error, Times.Once());
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

    private void VerifyLogLevel(LogLevel level, Times times)
    {
        _mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            times);
    }

    #endregion
}

// Additional integration tests for more complex scenarios
public class DownloadServiceIntegrationTests : IDisposable
{
    private readonly HttpClient _realHttpClient;
    private readonly DownloadService _downloadService;
    private readonly string _tempDirectory;

    public DownloadServiceIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("DesktopEyeClient", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "DesktopEye/1.0");
        });
    
        // Build the service provider and get the HttpClientFactory
        var serviceProvider = services.BuildServiceProvider();
        var bugsnag = serviceProvider.GetRequiredService<Bugsnag.IClient>();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var mockLogger = new Mock<ILogger<DownloadService>>();
        _realHttpClient = httpClientFactory.CreateClient("DesktopEyeClient");
        _downloadService = new DownloadService(httpClientFactory, mockLogger.Object, bugsnag);
        
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        _realHttpClient?.Dispose();
        
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public async Task DownloadFileAsync_RealHttpRequest_WithValidUrl_Succeeds()
    {
        // Arrange
        var url = "https://httpbin.org/robots.txt"; // Reliable test endpoint
        var destinationPath = Path.Combine(_tempDirectory, "robots.txt");

        // Act
        var result = await _downloadService.DownloadFileAsync(url, destinationPath);

        // Assert
        Assert.True(result);
        Assert.True(File.Exists(destinationPath));
        
        var content = await File.ReadAllTextAsync(destinationPath);
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task DownloadFileAsync_RealHttpRequest_WithInvalidUrl_ReturnsFalse()
    {
        // Arrange
        var url = "https://httpbin.org/status/404";
        var destinationPath = Path.Combine(_tempDirectory, "notfound.txt");

        // Act
        var result = await _downloadService.DownloadFileAsync(url, destinationPath);

        // Assert
        Assert.False(result);
        Assert.False(File.Exists(destinationPath));
    }
}