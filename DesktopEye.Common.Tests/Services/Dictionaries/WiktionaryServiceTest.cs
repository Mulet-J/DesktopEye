/*using System.Net;
using System.Text.Json;
using DesktopEye.Common.Domain.Features.Dictionary;
using DesktopEye.Common.Domain.Models.Dictionary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using OneOf.Types;

namespace DesktopEye.Common.Tests.Services.Dictionaries;

public class WiktionaryServiceTests : IDisposable
{
    private readonly Mock<ILogger<WiktionaryService>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly WiktionaryService _wiktionaryService;

    public WiktionaryServiceTests()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("DesktopEyeClient", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "DesktopEye/1.0");
        });

        var serviceProvider = services.BuildServiceProvider();
        var bugsnag = serviceProvider.GetRequiredService<Bugsnag.IClient>();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = httpClientFactory.CreateClient("DesktopEyeClient");
        _wiktionaryService = new WiktionaryService(httpClientFactory, _mockLogger.Object, bugsnag );
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    [Fact]
    public async Task GetDefinitionsAsync_ValidTerm_ReturnsResponse()
    {
        // Arrange
        var term = "test";
        var expectedResponse = new WiktionaryResponse
        {
            { "en", new List<WiktionaryEntry>
                {
                    new WiktionaryEntry
                    {
                        PartOfSpeech = "Noun",
                        Language = "en",
                        Definitions = new List<WiktionaryDefinition>
                        {
                            new WiktionaryDefinition
                            {
                                Definition = "A procedure intended to establish the quality, performance, or reliability of something.",
                                Examples = new List<string> { "This is a test example." }
                            }
                        }
                    }
                }
            }
        };

        SetupSuccessfulHttpResponse(JsonSerializer.Serialize(expectedResponse));

        // Act
        var response = await _wiktionaryService.GetDefinitionsAsync(term);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.ContainsKey("en"));
        Assert.Equal("Noun", response["en"][0].PartOfSpeech);
    }

    [Fact]
    public async Task GetDefinitionsAsync_CachedTerm_ReturnsCachedResponse()
    {
        // Arrange
        var term = "cached";
        var cachedResponse = new WiktionaryResponse
        {
            { "fr", new List<WiktionaryEntry>
                {
                    new WiktionaryEntry
                    {
                        PartOfSpeech = "verb",
                        Language = "fr",
                        Definitions = new List<WiktionaryDefinition>
                        {
                            new WiktionaryDefinition
                            {
                                Definition = "Un exemple de définition en français.",
                                Examples = new List<string> { "Exemple en français." }
                            }
                        }
                    }
                }
            }
        };

        _wiktionaryService.GetType()
            .GetField("_cache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(_wiktionaryService, new Dictionary<string, WiktionaryResponse> { { term, cachedResponse } });

        // Act
        var response = await _wiktionaryService.GetDefinitionsAsync(term);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.ContainsKey("fr"));
        Assert.Equal("verb", response["fr"][0].PartOfSpeech);
    }

    [Fact]
    public async Task GetDefinitionsAsync_InvalidTerm_ThrowsException()
    {
        // Arrange
        var term = "neuille";
        SetupHttpResponseWithStatusCode(HttpStatusCode.NotFound);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () => await _wiktionaryService.GetDefinitionsAsync(term));
    }
    

    private void SetupSuccessfulHttpResponse(string content)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content)
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
}*/