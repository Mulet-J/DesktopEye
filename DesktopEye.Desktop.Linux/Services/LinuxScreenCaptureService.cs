using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using DesktopEye.Common.Infrastructure.Services.ScreenCapture;
using Tmds.DBus;

namespace DesktopEye.Desktop.Linux.Services;

public class LinuxScreenCaptureService : IScreenCaptureService
{
    public Bitmap CaptureScreen()
    {
        var a = CaptureWorkspaceFromXdgPortalAsync().GetAwaiter().GetResult();
        var bitmap = LoadBitmapFromUri(a);
        // PortalProgram.Main();
        return bitmap;
    }

    /// <summary>
    ///     Returns a Bitmap of the user's workspace using the XDG portal's desktop screenshot method.
    /// </summary>
    private async Task<Uri> CaptureWorkspaceFromXdgPortalAsync()
    {
        var connection = new Connection(Address.Session);
        await connection.ConnectAsync().ConfigureAwait(false);
        var screenshotPortal = connection.CreateProxy<IScreenshot>(
            "org.freedesktop.portal.Desktop",
            "/org/freedesktop/portal/desktop"
        );

        var handleToken = Guid.NewGuid().ToString("N");
        var options = new Dictionary<string, object>
        {
            { "handle_token", handleToken },
            { "interactive", false }
        };

        var requestPath = await screenshotPortal.ScreenshotAsync("", options);

        var requestProxy = connection.CreateProxy<IRequest>(
            "org.freedesktop.portal.Request",
            requestPath
        );

        var tcs = new TaskCompletionSource<string>();

        await requestProxy.WatchResponseAsync(tuple =>
        {
            if (tuple.results.TryGetValue("uri", out var obj))
                tcs.TrySetResult(obj.ToString() ?? string.Empty);
            else
                tcs.TrySetResult(string.Empty);
        }, error => throw new Exception(error.Message));

        var a = await tcs.Task;

        var uri = new Uri(a);

        return uri;
    }

    private Bitmap LoadBitmapFromUri(Uri uri)
    {
        var bitmap = new Bitmap(uri.AbsolutePath);
        // remove file once properly loaded
        File.Delete(uri.LocalPath);
        return bitmap;
    }
}

[DBusInterface("org.freedesktop.portal.Screenshot")]
public interface IScreenshot : IDBusObject
{
    Task<ObjectPath> ScreenshotAsync(string parentWindow, IDictionary<string, object> options);
    // Task<ObjectPath> PickColorAsync(string ParentWindow, IDictionary<string, object> Options);
    // Task<T> GetAsync<T>(string prop);
    // Task<ScreenshotProperties> GetAllAsync();
    // Task SetAsync(string prop, object val);
    // Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}

[DBusInterface("org.freedesktop.portal.Request")]
public interface IRequest : IDBusObject
{
    Task CloseAsync();

    Task<IDisposable> WatchResponseAsync(Action<(uint response, IDictionary<string, object> results)> handler,
        Action<Exception>? onError = null);
}

// [Dictionary]
// public class ScreenshotProperties
// {
//     public uint Version { get; set; } = default;
// }