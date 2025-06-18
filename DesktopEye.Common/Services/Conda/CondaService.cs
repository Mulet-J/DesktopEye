using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DesktopEye.Common.Classes;
using DesktopEye.Common.Exceptions;
using DesktopEye.Common.Services.ApplicationPath;
using DesktopEye.Common.Services.Download;
using Microsoft.Extensions.Logging;

namespace DesktopEye.Common.Services.Conda;

public class CondaService : ICondaService
{
    private readonly IDownloadService _downloadService;

    private readonly Dictionary<string, string> _downloadUrls = new()
    {
        { "Windows-x64", "https://repo.anaconda.com/miniconda/Miniconda3-latest-Windows-x86_64.exe" },
        { "Windows-x86", "https://repo.anaconda.com/miniconda/Miniconda3-latest-Windows-x86.exe" },
        { "Linux-x64", "https://repo.anaconda.com/miniconda/Miniconda3-latest-Linux-x86_64.sh" },
        { "macOS-x64", "https://repo.anaconda.com/miniconda/Miniconda3-latest-MacOSX-x86_64.sh" },
        { "macOS-arm64", "https://repo.anaconda.com/miniconda/Miniconda3-latest-MacOSX-arm64.sh" }
    };

    private readonly ILogger<CondaService> _logger;

    private readonly IPathService _pathService;

    public CondaService(IPathService pathService, IDownloadService downloadService, ILogger<CondaService> logger)
    {
        _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
        _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("CondaService initialized with conda directory: {CondaDirectory}",
            _pathService.CondaDirectory);
    }

    public string CondaDirectoryPath => _pathService.CondaDirectory;

    /// <summary>
    ///     The path to the conda environment's python dll.
    ///     Should be a .so on Linux, a .dll on Windows, and a .dylib on MacOs
    /// </summary>
    /// <exception cref="PythonException"></exception>
    public string PythonDllPath
    {
        get
        {
            _logger.LogDebug("Searching for Python DLL in conda directory: {CondaDir}", CondaDirectoryPath);
            var condaDir = CondaDirectoryPath;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _logger.LogDebug("Platform detected: Windows - searching for python3*.dll");

                // On Windows, look for python3X.dll in the main conda directory
                var pythonDlls = Directory.GetFiles(condaDir, "python3*.dll", SearchOption.TopDirectoryOnly);
                if (pythonDlls.Length > 0)
                {
                    Array.Sort(pythonDlls);
                    var selectedDll = pythonDlls[^1];
                    _logger.LogDebug("Found Python DLL in main directory: {DllPath}", selectedDll);
                    return selectedDll;
                }

                // Fallback: look in DLLs subdirectory
                var dllsDir = Path.Combine(condaDir, "DLLs");
                _logger.LogDebug("Searching fallback location: {DllsDir}", dllsDir);

                if (Directory.Exists(dllsDir))
                {
                    pythonDlls = Directory.GetFiles(dllsDir, "python3*.dll", SearchOption.TopDirectoryOnly);
                    if (pythonDlls.Length > 0)
                    {
                        Array.Sort(pythonDlls);
                        var selectedDll = pythonDlls[^1];
                        _logger.LogDebug("Found Python DLL in DLLs subdirectory: {DllPath}", selectedDll);
                        return selectedDll;
                    }
                }

                _logger.LogWarning("No Python DLL found in Windows conda directory: {CondaDir}", condaDir);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _logger.LogDebug("Platform detected: Linux - searching for libpython3.*.so");

                // On Linux, look for libpython3.X.so in lib directory
                var libDir = Path.Combine(condaDir, "lib");
                if (Directory.Exists(libDir))
                {
                    // Also check for .so files without version suffix
                    var pythonLibs = Directory.GetFiles(libDir, "libpython3.*.so", SearchOption.TopDirectoryOnly);
                    if (pythonLibs.Length > 0)
                    {
                        Array.Sort(pythonLibs);
                        var selectedLib = pythonLibs[^1];
                        _logger.LogDebug("Found Python library: {LibPath}", selectedLib);
                        return selectedLib;
                    }

                    pythonLibs = Directory.GetFiles(libDir, "libpython3.*.so.*", SearchOption.TopDirectoryOnly);
                    if (pythonLibs.Length > 0)
                    {
                        Array.Sort(pythonLibs);
                        var selectedLib = pythonLibs[^1];
                        _logger.LogDebug("Found Python library with version suffix: {LibPath}", selectedLib);
                        return selectedLib;
                    }
                }

                _logger.LogWarning("No Python library found in Linux conda directory: {LibDir}", libDir);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _logger.LogDebug("Platform detected: macOS - searching for libpython3.*.dylib");

                // On macOS, look for libpython3.X.dylib in lib directory
                var libDir = Path.Combine(condaDir, "lib");
                if (Directory.Exists(libDir))
                {
                    var pythonLibs = Directory.GetFiles(libDir, "libpython3.*.dylib", SearchOption.TopDirectoryOnly);
                    if (pythonLibs.Length > 0)
                    {
                        Array.Sort(pythonLibs);
                        var selectedLib = pythonLibs[^1];
                        _logger.LogDebug("Found Python library on macOS: {LibPath}", selectedLib);
                        return selectedLib;
                    }
                }

                _logger.LogWarning("No Python library found in macOS conda directory: {LibDir}", libDir);
            }

            _logger.LogError("Could not find Python DLL/library in conda directory: {CondaDir}", condaDir);
            throw new PythonException("Could not find python dll");
        }
    }

    public string CondaExecutablePath
    {
        get
        {
            var condaDir = CondaDirectoryPath;
            string executablePath;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                executablePath = Path.Combine(condaDir, "Scripts", "conda.exe");
            else
                executablePath = Path.Combine(condaDir, "bin", "conda");

            _logger.LogDebug("Conda executable path: {ExecutablePath}", executablePath);
            return executablePath;
        }
    }

    public bool IsInstalled
    {
        get
        {
            try
            {
                var isInstalled = File.Exists(CondaExecutablePath);
                _logger.LogDebug("Conda installation check: {IsInstalled} (path: {ExecutablePath})",
                    isInstalled, CondaExecutablePath);
                return isInstalled;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if Conda is installed");
                return false;
            }
        }
    }

    public async Task<bool> InstallMinicondaAsync()
    {
        _logger.LogInformation("Starting Miniconda installation process");

        try
        {
            if (IsInstalled)
            {
                _logger.LogInformation("Miniconda is already installed, skipping installation");
                return true;
            }

            var platform = GetPlatformString();
            _logger.LogInformation("Detected platform: {Platform}", platform);

            if (!_downloadUrls.TryGetValue(platform, out var downloadUrl))
            {
                _logger.LogError("Platform {Platform} is not supported", platform);
                throw new PlatformNotSupportedException($"Platform {platform} is not supported");
            }

            var installerPath = Path.Combine(_pathService.DownloadsDirectory, GetInstallerFileName(platform));
            _logger.LogInformation("Downloading Miniconda installer to: {InstallerPath}", installerPath);

            // Ensure directories exist
            Directory.CreateDirectory(_pathService.DownloadsDirectory);

            // Download installer
            var downloadSuccess = await _downloadService.DownloadFileAsync(downloadUrl, installerPath);
            if (!downloadSuccess)
            {
                _logger.LogError("Failed to download Miniconda installer from: {DownloadUrl}", downloadUrl);
                return false;
            }

            _logger.LogInformation("Successfully downloaded Miniconda installer");

            // Install Miniconda
            _logger.LogInformation("Starting Miniconda installation to: {InstallPath}", CondaDirectoryPath);
            var installSuccess = await InstallMinicondaAsync(installerPath, CondaDirectoryPath);

            if (installSuccess)
                _logger.LogInformation("Miniconda installation completed successfully");
            else
                _logger.LogError("Miniconda installation failed");

            // Clean up installer
            try
            {
                File.Delete(installerPath);
                _logger.LogDebug("Cleaned up installer file: {InstallerPath}", installerPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up installer file: {InstallerPath}", installerPath);
            }

            return installSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Miniconda installation");
            return false;
        }
    }

    public async Task<string> ExecuteCondaCommandAsync(string command, string? environmentName = null)
    {
        _logger.LogDebug("Executing conda command: {Command} (environment: {Environment})",
            command, environmentName ?? "base");

        if (!IsInstalled)
        {
            _logger.LogError("Cannot execute conda command - Conda is not installed");
            throw new InvalidOperationException("Conda is not installed");
        }

        try
        {
            using var process = new Process();
            process.StartInfo.FileName = CondaExecutablePath;
            process.StartInfo.Arguments = command;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            // Set environment variables if needed
            if (!string.IsNullOrWhiteSpace(environmentName))
            {
                var condaDefaultEnv = Path.Combine(CondaDirectoryPath, "envs", environmentName);
                process.StartInfo.EnvironmentVariables["CONDA_DEFAULT_ENV"] = environmentName;
                process.StartInfo.EnvironmentVariables["CONDA_PREFIX"] = condaDefaultEnv;
                _logger.LogDebug("Set environment variables for conda environment: {Environment}", environmentName);
            }

            var stopwatch = Stopwatch.StartNew();
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            stopwatch.Stop();

            _logger.LogDebug("Conda command completed in {ElapsedMs}ms with exit code: {ExitCode}",
                stopwatch.ElapsedMilliseconds, process.ExitCode);

            if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                _logger.LogWarning("Conda command returned non-zero exit code {ExitCode}. Error: {Error}",
                    process.ExitCode, error.Trim());

            // Return output, or error if output is empty
            var result = !string.IsNullOrEmpty(output) ? output : error;
            _logger.LogTrace("Conda command output: {Output}", result.Trim());

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute conda command: {Command}", command);
            throw new InvalidOperationException($"Failed to execute conda command: {command}", ex);
        }
    }

    /// <summary>
    ///     Install packages in the specified (base by default) environment
    /// </summary>
    /// <param name="instruction">The packages to install and the targeted channel</param>
    /// <param name="environmentName">The environment in which the packages should be installed</param>
    /// <returns>Returns true if the packages were properly installed or already exist, false if any issue was encountered</returns>
    public async Task<bool> InstallPackageUsingCondaAsync(CondaInstallInstruction instruction,
        string? environmentName = null)
    {
        _logger.LogInformation(
            "Installing conda packages: {Packages} from channel: {Channel} in environment: {Environment}",
            string.Join(", ", instruction.Packages), instruction.Channel, environmentName ?? "base");

        try
        {
            var commandBuilder = new StringBuilder("install");

            if (!string.IsNullOrEmpty(environmentName)) commandBuilder.Append($" -n {environmentName}");

            if (!string.IsNullOrEmpty(instruction.Channel)) commandBuilder.Append($" -c {instruction.Channel}");

            if (instruction.Packages.Count > 0)
            {
                commandBuilder.Append($" {string.Join(" ", instruction.Packages)}");
            }
            else
            {
                _logger.LogWarning("No packages specified for installation");
                return false;
            }

            commandBuilder.Append(" -y");

            var command = commandBuilder.ToString();
            _logger.LogDebug("Generated conda install command: {Command}", command);

            var result = await ExecuteCondaCommandAsync(command, environmentName);

            var success = !string.IsNullOrEmpty(result);
            if (success)
                _logger.LogInformation("Successfully installed conda packages: {Packages}",
                    string.Join(", ", instruction.Packages));
            else
                _logger.LogError("Failed to install conda packages: {Packages}",
                    string.Join(", ", instruction.Packages));

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error installing conda packages: {Packages}",
                string.Join(", ", instruction.Packages));
            return false;
        }
    }

    public async Task<bool> InstallPackageUsingCondaAsync(List<CondaInstallInstruction> instructions,
        string? environmentName = null)
    {
        if (instructions.Count == 0)
        {
            _logger.LogWarning("No installation instructions provided");
            return false;
        }

        _logger.LogInformation("Installing {Count} conda package instruction(s) in environment: {Environment}",
            instructions.Count, environmentName ?? "base");

        try
        {
            foreach (var instruction in instructions)
            {
                var success = await InstallPackageUsingCondaAsync(instruction, environmentName);
                if (!success)
                {
                    _logger.LogError("Failed to install packages from instruction with channel: {Channel}",
                        instruction.Channel);
                    return false;
                }
            }

            _logger.LogInformation("Successfully installed all conda package instructions");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error installing conda package instructions");
            return false;
        }
    }

    public async Task<bool> InstallPackageUsingPipAsync(List<string> packages, string? environmentName = null)
    {
        if (packages.Count == 0)
        {
            _logger.LogWarning("No packages provided for pip installation");
            return false;
        }

        _logger.LogInformation("Installing {Count} pip package(s): {Packages} in environment: {Environment}",
            packages.Count, string.Join(", ", packages), environmentName ?? "base");

        try
        {
            foreach (var package in packages)
            {
                var success = await InstallPackageUsingPipAsync(package);
                if (!success)
                {
                    _logger.LogError("Failed to install pip package: {Package}", package);
                    return false;
                }
            }

            _logger.LogInformation("Successfully installed all pip packages");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error installing pip packages: {Packages}", string.Join(", ", packages));
            return false;
        }
    }

    public async Task<bool> InstallPackageUsingPipAsync(string package, string? environmentName = null)
    {
        if (string.IsNullOrWhiteSpace(package))
        {
            _logger.LogWarning("Package name is null or empty for pip installation");
            return false;
        }

        _logger.LogInformation("Installing pip package: {Package} in environment: {Environment}",
            package, environmentName ?? "base");

        if (!IsInstalled)
        {
            _logger.LogError("Cannot install pip package - Conda is not installed");
            throw new InvalidOperationException("Conda is not installed");
        }

        try
        {
            // Determine the pip executable path
            string pipExecutablePath;

            if (!string.IsNullOrWhiteSpace(environmentName))
            {
                // Use pip from the specified conda environment
                var envPath = Path.Combine(CondaDirectoryPath, "envs", environmentName);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    pipExecutablePath = Path.Combine(envPath, "Scripts", "pip.exe");
                else
                    pipExecutablePath = Path.Combine(envPath, "bin", "pip");
            }
            else
            {
                // Use pip from the base conda environment
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    pipExecutablePath = Path.Combine(CondaDirectoryPath, "Scripts", "pip.exe");
                else
                    pipExecutablePath = Path.Combine(CondaDirectoryPath, "bin", "pip");
            }

            _logger.LogDebug("Using pip executable: {PipPath}", pipExecutablePath);

            // Check if pip exists
            if (!File.Exists(pipExecutablePath))
            {
                _logger.LogError("Pip executable not found at: {PipPath}", pipExecutablePath);
                return false;
            }

            using var process = new Process();
            process.StartInfo.FileName = pipExecutablePath;
            process.StartInfo.Arguments = $"install {package} --quiet";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            // Set environment variables if using a specific environment
            if (!string.IsNullOrWhiteSpace(environmentName))
            {
                var condaDefaultEnv = Path.Combine(CondaDirectoryPath, "envs", environmentName);
                process.StartInfo.EnvironmentVariables["CONDA_DEFAULT_ENV"] = environmentName;
                process.StartInfo.EnvironmentVariables["CONDA_PREFIX"] = condaDefaultEnv;
            }

            var stopwatch = Stopwatch.StartNew();
            process.Start();

            _ = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            stopwatch.Stop();

            _logger.LogDebug("Pip install completed in {ElapsedMs}ms with exit code: {ExitCode}",
                stopwatch.ElapsedMilliseconds, process.ExitCode);

            if (process.ExitCode == 0)
                _logger.LogInformation("Successfully installed pip package: {Package}", package);
            else
                _logger.LogError("Failed to install pip package: {Package}. Exit code: {ExitCode}, Error: {Error}",
                    package, process.ExitCode, error.Trim());

            // Return true if the process exited successfully
            // Pip typically returns 0 on success
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error installing pip package: {Package}", package);
            return false;
        }
    }

    private string GetPlatformString()
    {
        string platform;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            platform = RuntimeInformation.ProcessArchitecture == Architecture.X64 ? "Windows-x64" : "Windows-x86";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            platform = "Linux-x64";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            platform = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "macOS-arm64" : "macOS-x64";
        }
        else
        {
            _logger.LogError("Unsupported platform detected");
            throw new PlatformNotSupportedException("Unsupported platform");
        }

        _logger.LogDebug("Detected platform: {Platform}", platform);
        return platform;
    }

    private string GetInstallerFileName(string platform)
    {
        var fileName = platform.StartsWith("Windows") ? "miniconda_installer.exe" : "miniconda_installer.sh";
        _logger.LogDebug("Generated installer filename: {FileName} for platform: {Platform}", fileName, platform);
        return fileName;
    }

    private async Task<bool> InstallMinicondaAsync(string installerPath, string installDirectory)
    {
        _logger.LogInformation("Installing Miniconda from: {InstallerPath} to: {InstallDirectory}",
            installerPath, installDirectory);

        try
        {
            // Delete folder if it exists, because miniconda returns an error otherwise
            if (Path.Exists(installDirectory))
            {
                _logger.LogInformation("Removing existing installation directory: {InstallDirectory}",
                    installDirectory);
                Directory.Delete(installDirectory, true);
            }

            using var process = new Process();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                process.StartInfo.FileName = installerPath;
                process.StartInfo.Arguments = $"/InstallationType=JustMe /RegisterPython=0 /S /D={installDirectory}";
                _logger.LogDebug("Windows installer command: {FileName} {Arguments}", process.StartInfo.FileName,
                    process.StartInfo.Arguments);
            }
            else
            {
                process.StartInfo.FileName = "bash";
                process.StartInfo.Arguments =
                    $"\"{installerPath}\" -b -p \"{Path.GetFullPath(installDirectory)}\"";
                _logger.LogDebug("Unix installer command: {FileName} {Arguments}", process.StartInfo.FileName,
                    process.StartInfo.Arguments);
            }

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            var stopwatch = Stopwatch.StartNew();
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            stopwatch.Stop();

            _logger.LogDebug("Miniconda installation completed in {ElapsedMs}ms with exit code: {ExitCode}",
                stopwatch.ElapsedMilliseconds, process.ExitCode);

            if (!string.IsNullOrEmpty(output))
                _logger.LogTrace("Installation output: {Output}", output.Trim());

            if (!string.IsNullOrEmpty(error))
                _logger.LogTrace("Installation error output: {Error}", error.Trim());

            var success = process.ExitCode == 0 && IsInstalled;

            if (success)
                _logger.LogInformation("Miniconda installation successful");
            else
                _logger.LogError("Miniconda installation failed. Exit code: {ExitCode}, IsInstalled: {IsInstalled}",
                    process.ExitCode, IsInstalled);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Miniconda installation");
            return false;
        }
    }
}