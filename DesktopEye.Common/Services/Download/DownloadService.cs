using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DesktopEye.Common.Services.Download;

public class DownloadService : IDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DownloadService> _logger;
    private readonly Bugsnag.IClient _bugsnag;

    public DownloadService(IHttpClientFactory httpClientFactory, ILogger<DownloadService> logger, Bugsnag.IClient bugsnag)
    {
        _httpClient = httpClientFactory.CreateClient("DesktopEyeClient");
        _logger = logger;
        _bugsnag = bugsnag;
    }

    public async Task<bool> DownloadFileAsync(string url, string destinationPath)
    {
        _logger.LogInformation("Starting download from {Url} to {DestinationPath}", url, destinationPath);

        try
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.LogError("Download failed: URL cannot be null or empty");
                throw new ArgumentException("URL cannot be null or empty", nameof(url));
            }

            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                _logger.LogError("Download failed: Destination path cannot be null or empty");
                throw new ArgumentException("Destination path cannot be null or empty", nameof(destinationPath));
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                _logger.LogDebug("Creating directory: {Directory}", directory);
                Directory.CreateDirectory(directory);
                _logger.LogDebug("Directory created successfully: {Directory}", directory);
            }

            _logger.LogDebug("Initiating HTTP GET request to {Url}", url);
            using var response = await _httpClient.GetAsync(url);

            _logger.LogDebug("HTTP response received. Status: {StatusCode}, Content-Length: {ContentLength}",
                response.StatusCode, response.Content.Headers.ContentLength);

            response.EnsureSuccessStatusCode();

            _logger.LogDebug("Reading response content stream");
            await using var contentStream = await response.Content.ReadAsStreamAsync();

            _logger.LogDebug("Creating file stream for {DestinationPath}", destinationPath);
            await using var fileStream =
                new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

            _logger.LogDebug("Copying content to file");
            await contentStream.CopyToAsync(fileStream);

            _logger.LogInformation("File downloaded successfully to {DestinationPath}", destinationPath);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _logger.LogDebug("Non-Windows platform detected. Setting executable permissions for {FilePath}",
                    destinationPath);
                await SetExecutablePermissionsAsync(destinationPath);
            }
            else
            {
                _logger.LogDebug("Windows platform detected. Skipping executable permissions setup");
            }

            _logger.LogInformation("Download completed successfully from {Url} to {DestinationPath}", url,
                destinationPath);
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "HTTP error occurred while downloading from {Url}. Status: {StatusCode}, Message: {Message}", url,
                ex.Data["StatusCode"], ex.Message);
            _bugsnag.Notify(ex);
            return false;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error occurred while saving file to {Path}. Message: {Message}", destinationPath,
                ex.Message);
            _bugsnag.Notify(ex);
            return false;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument provided. Parameter: {ParamName}, Message: {Message}", ex.ParamName,
                ex.Message);
            _bugsnag.Notify(ex);
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied while writing to {Path}. Message: {Message}", destinationPath,
                ex.Message);
            _bugsnag.Notify(ex);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error occurred while downloading from {Url} to {DestinationPath}. Exception type: {ExceptionType}, Message: {Message}",
                url, destinationPath, ex.GetType().Name, ex.Message);
            _bugsnag.Notify(ex);
            return false;
        }
    }

    private async Task SetExecutablePermissionsAsync(string filePath)
    {
        _logger.LogDebug("Attempting to set executable permissions for {FilePath}", filePath);

        try
        {
            // Method 1: Using chmod command (most reliable)
            _logger.LogDebug("Method 1: Attempting to use chmod command for {FilePath}", filePath);

            using var process = new Process();
            process.StartInfo.FileName = "chmod";
            process.StartInfo.Arguments = $"+x \"{filePath}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            _logger.LogDebug("Starting chmod process with arguments: {Arguments}", process.StartInfo.Arguments);
            process.Start();
            await process.WaitForExitAsync();

            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();

            if (process.ExitCode == 0)
            {
                _logger.LogDebug("Successfully set executable permissions for {FilePath} using chmod", filePath);
                if (!string.IsNullOrEmpty(stdout)) _logger.LogTrace("chmod stdout: {Output}", stdout);
                return;
            }

            _logger.LogWarning("chmod command failed with exit code {ExitCode} for {FilePath}. Stderr: {Error}",
                process.ExitCode, filePath, stderr);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to set executable permissions using chmod for {FilePath}. Exception: {ExceptionType}, Message: {Message}",
                filePath, ex.GetType().Name, ex.Message);
            _bugsnag.Notify(ex);
        }

        // Method 2: Fallback using .NET's UnixFileMode (available in .NET 6+)
        try
        {
            _logger.LogDebug("Method 2: Attempting to use UnixFileMode for {FilePath}", filePath);

            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                // Get current permissions
                var currentMode = File.GetUnixFileMode(filePath);

                _logger.LogDebug("Current file mode for {FilePath}: {CurrentMode}", filePath, currentMode);

                // Add execute permissions for owner, group, and others
                var newMode = currentMode |
                              UnixFileMode.UserExecute |
                              UnixFileMode.GroupExecute |
                              UnixFileMode.OtherExecute;

                _logger.LogDebug("Setting new file mode for {FilePath}: {NewMode}", filePath, newMode);
                File.SetUnixFileMode(filePath, newMode);

                _logger.LogDebug("Successfully set executable permissions using UnixFileMode for {FilePath}", filePath);
            }
            else
            {
                _logger.LogWarning("UnixFileMode method called on unsupported platform for {FilePath}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to set executable permissions using UnixFileMode for {FilePath}. Exception: {ExceptionType}, Message: {Message}",
                filePath, ex.GetType().Name, ex.Message);
            _bugsnag.Notify(ex);
        }

        _logger.LogWarning("All methods to set executable permissions failed for {FilePath}", filePath);
    }
}