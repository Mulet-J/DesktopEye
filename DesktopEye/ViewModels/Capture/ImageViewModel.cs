using System;
using Avalonia;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopEye.Helpers;
using DesktopEye.Views;

namespace DesktopEye.ViewModels;

public partial class ImageViewModel : ViewModelBase
{
    [ObservableProperty] private Bitmap? _bitmap;
    [ObservableProperty] private Rect? _selection;

    public ImageViewModel(Bitmap bitmap)
    {
        Bitmap = bitmap;
    }

    [RelayCommand]
    private void ProcessSelection()
    {
        if (!Selection.HasValue) return;
        var cropedBitmap = Bitmap.CropBitmap(Selection.Value.TopLeft, Selection.Value.BottomRight);
        var window = new InteractionWindow
        {
            DataContext = new InteractionViewModel(cropedBitmap)
        };
        window.Show();
    }

    [RelayCommand]
    private void Test()
    {
        Console.WriteLine("aze");
    }
}