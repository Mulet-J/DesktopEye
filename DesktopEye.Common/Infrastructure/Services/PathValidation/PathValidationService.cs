using System;
using System.IO;

namespace DesktopEye.Common.Infrastructure.Services.PathValidation;

public class PathValidationService : IPathValidationService
{
    public PathValidationResult ValidateAppDataPath(string? path)
    {
        var result = new PathValidationResult();

        if (string.IsNullOrWhiteSpace(path))
        {
            result.AddError("Path cannot be empty");
            return result;
        }

        if (path.Contains(' ')) result.AddError("Path cannot contain spaces");

        try
        {
            var dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists) dirInfo.Create();

            // Test write permissions
            var testFile = Path.Combine(path, "test_write.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
        }
        catch (Exception ex)
        {
            result.AddError("Cannot create or write to path.");
        }

        return result;
    }
}