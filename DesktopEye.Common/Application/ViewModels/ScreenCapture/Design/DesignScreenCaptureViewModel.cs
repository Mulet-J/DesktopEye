using System;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DesktopEye.Common.Infrastructure.Services.ScreenCapture;
using Moq;

namespace DesktopEye.Common.Application.ViewModels.ScreenCapture.Design;

public class DesignScreenCaptureViewModel : ScreenCaptureViewModel
{
    public DesignScreenCaptureViewModel() : base(
        new Mock<IServiceProvider>().Object,
        new Mock<IScreenCaptureService>().Object,
        new Mock<Bugsnag.IClient>().Object
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