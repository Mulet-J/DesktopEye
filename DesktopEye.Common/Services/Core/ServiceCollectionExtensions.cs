using DesktopEye.Common.Services.OCRService;
using DesktopEye.Common.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopEye.Common.Services.Core;

public static class ServiceCollectionExtensions
{
    // https://docs.avaloniaui.net/docs/guides/implementation-guides/how-to-implement-dependency-injection
    public static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        //Singleton services
        services.AddSingleton<IOcrService, TesseractOcrService>();
        // Transient services
        services.AddTransient<ScreenCaptureViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<ScreenCaptureActionsViewModel>();
        // return the service collection to allow for method chaining
        return services;
    }
}