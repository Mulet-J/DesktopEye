using Avalonia;
using DesktopEye.Common.Application.Views.Controls;
using DesktopEye.Common.Infrastructure.Services.ScreenCapture;

namespace DesktopEye.Desktop.Linux.Services;

public class LinuxScaleFactoringService : IScaleFactoringService
{
    public double GetOsScaleFactor(AreaSelectionControl areaSelectionControl)
    {
        return 1.0; // Linux does not have a standard way to get the OS scale factor like Windows or macOS.
    }
}