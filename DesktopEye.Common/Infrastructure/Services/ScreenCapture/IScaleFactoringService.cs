using Avalonia;
using DesktopEye.Common.Application.Views.Controls;

namespace DesktopEye.Common.Infrastructure.Services.ScreenCapture;

public interface IScaleFactoringService
{
    public double GetOsScaleFactor(AreaSelectionControl areaSelectionControl);
}