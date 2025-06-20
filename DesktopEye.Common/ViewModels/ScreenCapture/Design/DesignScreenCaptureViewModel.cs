using System;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DesktopEye.Common.Services.ScreenCapture;
using Moq;

namespace DesktopEye.Common.ViewModels.ScreenCapture.Design;

public class DesignScreenCaptureViewModel : ScreenCaptureViewModel
{
    public DesignScreenCaptureViewModel() : base(
        new Mock<IServiceProvider>().Object,
        new Mock<IScreenCaptureService>().Object
    )
    {
        Bitmap = CreateMockBitmap();
    }

    private Bitmap CreateMockBitmap()
    {
        var writeableBitmap = new WriteableBitmap(
            new PixelSize(200, 100),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        return writeableBitmap;
    }
}