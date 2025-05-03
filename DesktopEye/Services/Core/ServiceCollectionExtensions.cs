using DesktopEye.Services.OCRService;
using DesktopEye.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopEye.Services.Core;

public static class ServiceCollectionExtensions
{
    // https://docs.avaloniaui.net/docs/guides/implementation-guides/how-to-implement-dependency-injection
    public static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        services.AddSingleton<IOcrService, TesseractOcrService>();

        services.AddTransient<ImageViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<InteractionViewModel>();
        return services;
    }
}