using System.Reflection;

namespace DesktopEye.Common.Tests.TestHelpers;

public static class PathHelper
{
    private const string AssetsFolderName = "Assets";
    private const string OutputFolderName = "TestResults";

    public static string GetProjectDirectory()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var directory = new DirectoryInfo(Path.GetDirectoryName(assemblyLocation));

        // Navigate up to find the project root (look for .csproj file)
        while (directory != null && !directory.GetFiles("*.csproj").Any()) directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not find project directory");
    }

    public static string GetAssetsPath()
    {
        var projectDir = GetProjectDirectory();
        var testResultsPath = Path.Combine(projectDir, AssetsFolderName);

        // Create the directory if it doesn't exist
        Directory.CreateDirectory(testResultsPath);

        return testResultsPath;
    }

    public static string GetTestResultsPath()
    {
        var projectDir = GetProjectDirectory();
        var testResultsPath = Path.Combine(projectDir, OutputFolderName);

        // Create the directory if it doesn't exist
        Directory.CreateDirectory(testResultsPath);

        return testResultsPath;
    }
}