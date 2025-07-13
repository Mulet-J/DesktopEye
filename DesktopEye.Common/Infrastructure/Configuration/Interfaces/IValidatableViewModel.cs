using System.Collections.Generic;

namespace DesktopEye.Common.Infrastructure.Configuration.Interfaces;

public interface IValidatableViewModel
{
    List<string> ValidationErrors { get; }
    void ValidateValues();
}