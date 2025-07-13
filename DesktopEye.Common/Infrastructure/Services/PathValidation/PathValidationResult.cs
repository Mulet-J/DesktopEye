using System.Collections.Generic;
using System.Linq;

namespace DesktopEye.Common.Infrastructure.Services.PathValidation;

public class PathValidationResult
{
    public List<string> Errors { get; } = new();
    public bool IsValid => !Errors.Any();

    public void AddError(string error)
    {
        Errors.Add(error);
    }
}