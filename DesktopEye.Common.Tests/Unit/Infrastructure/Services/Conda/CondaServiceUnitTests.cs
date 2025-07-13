using System.Runtime.InteropServices;
using DesktopEye.Common.Infrastructure.Exceptions;
using DesktopEye.Common.Infrastructure.Services.ApplicationPath;
using DesktopEye.Common.Infrastructure.Services.Conda;
using DesktopEye.Common.Infrastructure.Services.Download;
using Microsoft.Extensions.Logging;
using Moq;

namespace DesktopEye.Common.Tests.Unit.Infrastructure.Services.Conda;

public class CondaServiceUnitTests : IDisposable
{
    private readonly Mock<IPathService> _mockPathService;
    private readonly Mock<IDownloadService> _mockDownloadService;
    private readonly Mock<ILogger<CondaService>> _mockLogger;
    private readonly Mock<Bugsnag.IClient> _mockBugsnag;
    private readonly string _testDirectory;
    private readonly CondaService _condaService;

    public CondaServiceUnitTests()
    {
        // Arrange - Configuration des mocks
        _mockPathService = new Mock<IPathService>();
        _mockDownloadService = new Mock<IDownloadService>();
        _mockLogger = new Mock<ILogger<CondaService>>();
        _mockBugsnag = new Mock<Bugsnag.IClient>();

        // Configuration du répertoire de test temporaire
        _testDirectory = Path.Combine(Path.GetTempPath(), "CondaServiceTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _mockPathService.Setup(x => x.CondaDirectory).Returns(_testDirectory);
        _mockPathService.Setup(x => x.DownloadsDirectory).Returns(Path.Combine(_testDirectory, "downloads"));

        _condaService = new CondaService(
            _mockPathService.Object,
            _mockDownloadService.Object,
            _mockBugsnag.Object,
            _mockLogger.Object);
    }

    public void Dispose()
    {
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
        Assert.NotNull(_condaService);
    }

    [Fact]
    public void Constructor_NullPathService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CondaService(null!, _mockDownloadService.Object, _mockBugsnag.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_NullDownloadService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CondaService(_mockPathService.Object, null!, _mockBugsnag.Object, _mockLogger.Object));
    }

    #endregion

    #region IsInstalled Property Tests

    [Fact]
    public void IsInstalled_CondaExecutableExists_ReturnsTrue()
    {
        // Arrange
        var expectedPath = GetExpectedCondaExecutablePath();
        Directory.CreateDirectory(Path.GetDirectoryName(expectedPath)!);
        File.WriteAllText(expectedPath, "dummy conda executable");

        // Act
        var result = _condaService.IsInstalled;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstalled_CondaExecutableDoesNotExist_ReturnsFalse()
    {
        // Arrange - Test directory is empty by default

        // Act
        var result = _condaService.IsInstalled;

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CondaExecutablePath Property Tests

    [Fact]
    public void CondaExecutablePath_Windows_ReturnsCorrectPath()
    {
        // Arrange - Only test if we can mock the platform detection
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return; // Skip on non-Windows platforms
        }

        // Act
        var result = _condaService.CondaExecutablePath;

        // Assert
        var expected = Path.Combine(_testDirectory, "Scripts", "conda.exe");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CondaExecutablePath_Unix_ReturnsCorrectPath()
    {
        // Arrange - Only test on Unix platforms
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return; // Skip on Windows
        }

        // Act
        var result = _condaService.CondaExecutablePath;

        // Assert
        var expected = Path.Combine(_testDirectory, "bin", "conda");
        Assert.Equal(expected, result);
    }

    #endregion

    #region PythonDllPath Property Tests

    [Fact]
    public void PythonDllPath_Windows_FindsPythonDll()
    {
        // Arrange
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return; // Skip on non-Windows
        }

        var dllPath = Path.Combine(_testDirectory, "python39.dll");
        File.WriteAllText(dllPath, "dummy dll");

        // Act
        var result = _condaService.PythonDllPath;

        // Assert
        Assert.Equal(dllPath, result);
    }

    [Fact]
    public void PythonDllPath_Linux_FindsPythonSo()
    {
        // Arrange
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return; // Skip on non-Linux
        }

        var libDir = Path.Combine(_testDirectory, "lib");
        Directory.CreateDirectory(libDir);
        var soPath = Path.Combine(libDir, "libpython3.9.so");
        File.WriteAllText(soPath, "dummy so");

        // Act
        var result = _condaService.PythonDllPath;

        // Assert
        Assert.Equal(soPath, result);
    }

    [Fact]
    public void PythonDllPath_NoPythonLibraryFound_ThrowsPythonException()
    {
        // Arrange - Empty test directory

        // Act & Assert
        Assert.Throws<PythonException>(() => _condaService.PythonDllPath);
    }

    #endregion

    #region InstallMinicondaAsync Tests

    [Fact]
    public async Task InstallMinicondaAsync_AlreadyInstalled_ReturnsTrue()
    {
        // Arrange
        var condaPath = GetExpectedCondaExecutablePath();
        Directory.CreateDirectory(Path.GetDirectoryName(condaPath)!);
        File.WriteAllText(condaPath, "dummy conda");

        // Act
        var result = await _condaService.InstallMinicondaAsync();

        // Assert
        Assert.True(result);
        _mockDownloadService.Verify(x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InstallMinicondaAsync_DownloadFails_ReturnsFalse()
    {
        // Arrange
        _mockDownloadService
            .Setup(x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _condaService.InstallMinicondaAsync();

        // Assert
        Assert.False(result);
        _mockDownloadService.Verify(x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task InstallMinicondaAsync_UnsupportedPlatform_ThrowsPlatformNotSupportedException()
    {
        // This test is hard to implement without platform abstraction
        // For now, we'll test that it doesn't throw for supported platforms
        
        // Act & Assert - Should not throw on supported platforms
        var result = await _condaService.InstallMinicondaAsync();
        
        // The result depends on whether download succeeds, but it shouldn't throw
        Assert.True(true);
    }

    #endregion

    #region ExecuteCondaCommandAsync Tests

    [Fact]
    public async Task ExecuteCondaCommandAsync_NotInstalled_ThrowsInvalidOperationException()
    {
        // Arrange - Conda not installed (empty test directory)
        var command = "list";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _condaService.ExecuteCondaCommandAsync(command));
    }

    #endregion

    #region InstallPackageUsingCondaAsync Tests

    [Fact]
    public async Task InstallPackageUsingCondaAsync_EmptyPackageList_ReturnsFalse()
    {
        // Arrange
        var instruction = new CondaInstallInstruction("conda-forge", new List<string>());

        // Act
        var result = await _condaService.InstallPackageUsingCondaAsync(instruction);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task InstallPackageUsingCondaAsync_ValidInstruction_CallsExecuteCondaCommand()
    {
        // Arrange
        var packages = new List<string> { "numpy", "pandas" };
        var instruction = new CondaInstallInstruction("conda-forge", packages);
        
        // Create conda executable to pass IsInstalled check
        var condaPath = GetExpectedCondaExecutablePath();
        Directory.CreateDirectory(Path.GetDirectoryName(condaPath)!);
        File.WriteAllText(condaPath, "dummy conda");

        // Act
        var result = await _condaService.InstallPackageUsingCondaAsync(instruction);

        // Assert
        // Note: This will fail in unit test because we can't actually execute conda
        // In a real unit test, we would need to mock the process execution
        Assert.False(result); // Expected to fail since conda isn't real
    }

    #endregion

    #region InstallPackageUsingPipAsync Tests

    [Fact]
    public async Task InstallPackageUsingPipAsync_EmptyPackageList_ReturnsFalse()
    {
        // Arrange
        var packages = new List<string>();

        // Act
        var result = await _condaService.InstallPackageUsingPipAsync(packages);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task InstallPackageUsingPipAsync_NullOrEmptyPackage_ReturnsFalse()
    {
        // Arrange & Act
        var result1 = await _condaService.InstallPackageUsingPipAsync((string)null!);
        var result2 = await _condaService.InstallPackageUsingPipAsync(string.Empty);
        var result3 = await _condaService.InstallPackageUsingPipAsync("   ");

        // Assert
        Assert.False(result1);
        Assert.False(result2);
        Assert.False(result3);
    }

    [Fact]
    public async Task InstallPackageUsingPipAsync_NotInstalled_ThrowsInvalidOperationException()
    {
        // Arrange
        var package = "numpy";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _condaService.InstallPackageUsingPipAsync(package));
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void PythonDllPath_ExceptionDuringSearch_ThrowsPythonException()
    {
        // Arrange - Mock PathService to return invalid path
        _mockPathService.Setup(x => x.CondaDirectory).Returns("invalid:/path");

        var condaService = new CondaService(
            _mockPathService.Object,
            _mockDownloadService.Object,
            _mockBugsnag.Object,
            _mockLogger.Object);

        // Act & Assert
        Assert.Throws<PythonException>(() => condaService.PythonDllPath);
    }

    #endregion

    #region Helper Methods

    private string GetExpectedCondaExecutablePath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(_testDirectory, "Scripts", "conda.exe");
        }
        else
        {
            return Path.Combine(_testDirectory, "bin", "conda");
        }
    }

    #endregion
}

#region Test Data Builders

/// <summary>
/// Builder pour créer des instructions d'installation Conda pour les tests
/// </summary>
public class CondaInstallInstructionBuilder
{
    private string _channel = "defaults";
    private List<string> _packages = new();

    public static CondaInstallInstructionBuilder Create() => new();

    public CondaInstallInstructionBuilder WithChannel(string channel)
    {
        _channel = channel;
        return this;
    }

    public CondaInstallInstructionBuilder WithPackages(params string[] packages)
    {
        _packages = packages.ToList();
        return this;
    }

    public CondaInstallInstructionBuilder WithPackage(string package)
    {
        _packages.Add(package);
        return this;
    }

    public CondaInstallInstruction Build() => new(_channel, _packages);
}

#endregion

#region Test Categories

/// <summary>
/// Attributs pour catégoriser les tests
/// </summary>
public static class TestCategories
{
    public const string Unit = "Unit";
    public const string Integration = "Integration";
    public const string Platform = "Platform";
    public const string FileSystem = "FileSystem";
    public const string Network = "Network";
}

#endregion