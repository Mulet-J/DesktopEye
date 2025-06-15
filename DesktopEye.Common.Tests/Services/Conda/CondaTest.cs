using DesktopEye.Common.Classes;
using DesktopEye.Common.Services.ApplicationPath;
using DesktopEye.Common.Services.Conda;
using DesktopEye.Common.Services.Download;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace DesktopEye.Common.Tests.Services.Conda;

public class CondaTest
{
    private readonly CondaService _condaService;
    private readonly IHttpClientFactory _httpClientFactory;

    public CondaTest()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("DesktopEyeClient", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "DesktopEye/1.0");
        });
    
        // Build the service provider and get the HttpClientFactory
        var serviceProvider = services.BuildServiceProvider();
        _httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        
        IPathService pathService = new PathService();
        ILogger<DownloadService> loggerService = new Logger<DownloadService>(new LoggerFactory());
        
        IDownloadService downloadService = new DownloadService(_httpClientFactory, loggerService);
        var condaLogger = new Mock<ILogger<CondaService>>();
        _condaService = new CondaService(pathService, downloadService, condaLogger.Object);
    }

    [Fact]
    public async Task InstallAsync_shouldReturnTrue()
    {
        // const bool expected = false;
        var returned = await _condaService.InstallMinicondaAsync();

        const bool expected = true;
        var actual = _condaService.IsInstalled;

        Assert.Equal(expected, returned);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task InstallPackageUsingPip_shouldReturnTrue()
    {
        List<string> packages = ["transformers", "torch", "accelerate"];
        const bool expected = true;

        var actual = await _condaService.InstallPackageUsingPipAsync(packages);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task InstallPackageUsingConda_shouldReturnTrue()
    {
        var instruction =
            new CondaInstallInstruction("conda-forge", ["transformers"]);
        const bool expected = true;

        var actual = await _condaService.InstallPackageUsingCondaAsync(instruction);

        Assert.Equal(expected, actual);
    }
}