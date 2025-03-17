using System;
using System.Runtime.InteropServices;
using DesktopEye.Services;
using Microsoft.Extensions.DependencyInjection;


namespace DesktopEye.Core;

public static class ServiceCollectionExtensions
{
    // https://docs.avaloniaui.net/docs/guides/implementation-guides/how-to-implement-dependency-injection
    public static void AddCommonServices(this IServiceCollection collection)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            collection.AddSingleton<IScreenCaptureService, LinuxScreenCaptureService>();
        }
        else
        {
            throw new PlatformNotSupportedException();
        }
    }
}