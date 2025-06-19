using Avalonia;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopEye.Common.Helpers;
using DesktopEye.Common.Views.ScreenCapture;

namespace DesktopEye.Common.ViewModels;

public partial class ScreenCaptureViewModel : ViewModelBase
{
    [ObservableProperty] private Bitmap? _bitmap;
    [ObservableProperty] private Rect? _selection;

    public ScreenCaptureViewModel(Bitmap bitmap)
    {
        Bitmap = bitmap;
    }

    [RelayCommand]
    private void ProcessSelection()
    {
        if (!Selection.HasValue) return;
        if (Bitmap is null) return;
        var cropedBitmap = Bitmap.CropBitmap(Selection.Value.TopLeft, Selection.Value.BottomRight);
        var window = new ScreenCaptureActionsWindow
        {
            DataContext = new ScreenCaptureActionsViewModel(cropedBitmap)
        };
        window.Show();
    }
}