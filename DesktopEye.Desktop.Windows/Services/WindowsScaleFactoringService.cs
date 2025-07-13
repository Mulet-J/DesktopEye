using Avalonia;
using Avalonia.Controls;
using DesktopEye.Common.Application.Views.Controls;
using DesktopEye.Common.Infrastructure.Services.ScreenCapture;

namespace DesktopEye.Desktop.Windows.Services;

public class WindowsScaleFactoringService : IScaleFactoringService 
{
   public double GetOsScaleFactor(AreaSelectionControl areaSelectionControl)
    {
        if (TopLevel.GetTopLevel(areaSelectionControl) is { } topLevel)
        {
            var screen = topLevel.Screens!.ScreenFromVisual(areaSelectionControl);
            return screen?.Scaling ?? 1.0;
        }
    
        return 1.0;
    }
}