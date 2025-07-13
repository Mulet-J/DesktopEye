using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using DesktopEye.Common.Infrastructure.Services.PathValidation;

namespace DesktopEye.Common.Tests.Services.PathValidation;

public class PathValidationServiceTests : IDisposable
{
    private readonly List<string> _createdDirectories;
    private readonly PathValidationService _service;

    public PathValidationServiceTests()
    {
        _service = new PathValidationService();
        _createdDirectories = new List<string>();
    }

    public void Dispose()
    {
        // Clean up all created directories
        foreach (var dir in _createdDirectories)
            try
            {
                if (Directory.Exists(dir))
                {
                    // Try to restore permissions first
                    CleanupReadOnlyDirectory(dir);
                    Directory.Delete(dir, true);
                }
            }
            catch
            {
                // Ignore cleanup errors - temp directories will be cleaned up by the OS
            }
    }

    [Fact]
    public void ValidateAppDataPath_PathWithSpace_ShouldFail()
    {
        // Arrange
        var pathWithSpace = Path.Combine(Path.GetTempPath(), "test folder with space");

        // Act
        var result = _service.ValidateAppDataPath(pathWithSpace);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("Path cannot contain spaces"));
    }

    [Fact]
    public void ValidateAppDataPath_ReadOnlyPath_ShouldFail()
    {
        // Arrange
        var readOnlyPath = CreateReadOnlyDirectory();

        // Act
        var result = _service.ValidateAppDataPath(readOnlyPath);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("Cannot create or write to path"));

        // Cleanup
        CleanupReadOnlyDirectory(readOnlyPath);
    }

    [Fact]
    public void ValidateAppDataPath_ValidWritablePath_ShouldPass()
    {
        // Arrange
        var validPath = Path.Combine(Path.GetTempPath(), "validtestpath_" + Guid.NewGuid().ToString("N")[..8]);
        _createdDirectories.Add(validPath);

        // Act
        var result = _service.ValidateAppDataPath(validPath);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.True(Directory.Exists(validPath)); // Should have been created
    }

    private string CreateReadOnlyDirectory()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "readonly_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempPath);
        _createdDirectories.Add(tempPath);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: Remove write permissions using DirectorySecurity
            var dirInfo = new DirectoryInfo(tempPath);
            var security = dirInfo.GetAccessControl();
            var userIdentity = WindowsIdentity.GetCurrent();
            var rule = new FileSystemAccessRule(
                userIdentity.User,
                FileSystemRights.Write | FileSystemRights.CreateFiles | FileSystemRights.CreateDirectories,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Deny);
            security.SetAccessRule(rule);
            dirInfo.SetAccessControl(security);
        }
        else
        {
            // Linux/macOS: Use chmod to make directory read-only
            // Remove write permissions (chmod 555)
            var process = new Process();
            process.StartInfo.FileName = "chmod";
            process.StartInfo.Arguments = $"555 \"{tempPath}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
        }

        return tempPath;
    }

    private void CleanupReadOnlyDirectory(string path)
    {
        if (!Directory.Exists(path)) return;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: Restore write permissions before deletion
                var dirInfo = new DirectoryInfo(path);
                var security = dirInfo.GetAccessControl();
                var userIdentity = WindowsIdentity.GetCurrent();
                var rule = new FileSystemAccessRule(
                    userIdentity.User,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow);
                security.SetAccessRule(rule);
                dirInfo.SetAccessControl(security);
            }
            else
            {
                // Linux/macOS: Restore write permissions
                var process = new Process();
                process.StartInfo.FileName = "chmod";
                process.StartInfo.Arguments = $"755 \"{path}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();
            }
        }
        catch
        {
            // Ignore errors during cleanup permission restoration
        }
    }
}

// Mock classes for the test (you'll need to replace these with your actual implementations)
public class PathValidationResult
{
    public List<string> Errors { get; } = new();
    public bool IsValid => Errors.Count == 0;

    public void AddError(string error)
    {
        Errors.Add(error);
    }
}

public interface IPathValidationService
{
    PathValidationResult ValidateAppDataPath(string? path);
}