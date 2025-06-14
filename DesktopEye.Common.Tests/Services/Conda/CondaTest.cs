using DesktopEye.Common.Classes;
using DesktopEye.Common.Services.ApplicationPath;
using DesktopEye.Common.Services.Conda;
using DesktopEye.Common.Services.Download;
using Microsoft.Extensions.Logging;
using Moq;

namespace DesktopEye.Common.Tests.Services.Conda;

public class CondaTest
{
    private readonly CondaService _condaService;

    public CondaTest()
    {
        IPathService pathService = new PathService();
        ILogger<DownloadService> loggerService = new Logger<DownloadService>(new LoggerFactory());
        var httpClient = new HttpClient();
        IDownloadService downloadService = new DownloadService(httpClient, loggerService);
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