namespace DesktopEye.Common.Infrastructure.Services.PathValidation;

public interface IPathValidationService
{
    PathValidationResult ValidateAppDataPath(string? path);
}