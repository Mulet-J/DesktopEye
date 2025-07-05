using System;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopEye.Common.Application.ViewModels.Base;
using DesktopEye.Common.Application.Views.ScreenCapture;
using DesktopEye.Common.Infrastructure.Services.ScreenCapture;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopEye.Common.Application.ViewModels.ScreenCapture;

public partial class ScreenCaptureViewModel : ViewModelBase, IDisposable
{
    private readonly IScreenCaptureService _captureService;
    private readonly IServiceProvider _services;
    private readonly Bugsnag.IClient _bugsnag;
    [ObservableProperty] private Bitmap? _bitmap;
    [ObservableProperty] private Rect? _selection;

    public ScreenCaptureViewModel(IServiceProvider services, IScreenCaptureService captureService, Bugsnag.IClient bugsnag)
    {
        _services = services;
        _captureService = captureService;
        _bugsnag = bugsnag;
        GetScreenBitmap();
    }

    public void Dispose()
    {
        Bitmap?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void GetScreenBitmap()
    {
        Bitmap = _captureService.CaptureScreen();
    }

    [RelayCommand]
    private void ProcessSelection()
    {
        try
        {
            if (!Selection.HasValue) return;
            if (Bitmap is null) return;
            var croppedBitmap = CropBitmap(Selection.Value.TopLeft, Selection.Value.BottomRight);
            var dataContext = _services.GetService<Application.ViewModels.ScreenCapture.ScreenCaptureActionsViewModel>();
            if (dataContext == null) throw new Exception();
            dataContext.SetBitmap(croppedBitmap);
            var window = new ScreenCaptureActionsWindow
            {
                DataContext = dataContext
            };

            window.Show();
        }
        catch (Exception e)
        {
            // Log the exception using Bugsnag
            _bugsnag.Notify(e);
        }
        
    }
    private WriteableBitmap CropBitmap(Point startPoint, Point endPoint)
    {
        if (Bitmap is null)
            throw new InvalidOperationException("Aucun bitmap source n'est disponible.");

        // Calculer les dimensions du rectangle de découpe
        var x = Math.Min(startPoint.X, endPoint.X);
        var y = Math.Min(startPoint.Y, endPoint.Y);
        var width = Math.Abs(endPoint.X - startPoint.X);
        var height = Math.Abs(endPoint.Y - startPoint.Y);

        // Valider les bornes de découpe
        if (width <= 0 || height <= 0)
            throw new ArgumentException("La zone de découpe doit avoir une largeur et une hauteur positives.");

        if (x < 0 || y < 0 || x + width > Bitmap.PixelSize.Width ||
            y + height > Bitmap.PixelSize.Height)
            throw new ArgumentException("La zone de découpe dépasse les limites du bitmap.");

        // Créer le rectangle source
        var sourceRect = new PixelRect((int)x, (int)y, (int)width, (int)height);

        // Créer un nouveau WriteableBitmap pour la zone découpée
        var croppedBitmap = new WriteableBitmap(
            new PixelSize((int)width, (int)height),
            Bitmap.Dpi,
            PixelFormat.Bgra8888,
            AlphaFormat.Premul
        );

        using var context = croppedBitmap.Lock();
        // Copier les données de pixels du rectangle source vers le nouveau bitmap
        Bitmap.CopyPixels(sourceRect, context.Address, context.RowBytes * context.Size.Height,
            context.RowBytes);

        return croppedBitmap;
    }
}