using System.Net.Http;
using DesktopEye.Common.Services.ApplicationPath;
using DesktopEye.Common.Services.Conda;
using DesktopEye.Common.Services.Download;
using DesktopEye.Common.Services.OCR;
using DesktopEye.Common.Services.Python;
using DesktopEye.Common.Services.TextClassifier;
using DesktopEye.Common.Services.Translation;
using DesktopEye.Common.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScreenCaptureActionsViewModel = DesktopEye.Common.ViewModels.ScreenCapture.ScreenCaptureActionsViewModel;
using ScreenCaptureViewModel = DesktopEye.Common.ViewModels.ScreenCapture.ScreenCaptureViewModel;

namespace DesktopEye.Common.Services.Core;

public static class ServiceCollectionExtensions
{
    // https://docs.avaloniaui.net/docs/guides/implementation-guides/how-to-implement-dependency-injection
    public static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        services.AddLogging(config => { config.AddConsole(); });
        //Singleton services
        // services.AddSingleton<IOcrService, TesseractOcrService>();
        services.AddSingleton<IPathService, PathService>();
        services.AddSingleton<ICondaService, CondaService>();

        services.AddSingleton<IPythonRuntimeManager, PythonRuntimeManager>();

        services.AddSingleton<IOcrManager, OcrManager>();
        services.AddSingleton<ITextClassifierManager, TextClassifierManager>();
        services.AddSingleton<ITranslationManager, TranslationManager>();
        services.AddSingleton<ServicesWarmer>();
        // Transient services
        // Ocr
        services.AddTransient<TesseractOcrService>();
        // Classifier
        services.AddTransient<FastTextClassifierService>();
        services.AddTransient<NTextCatClassifierService>();
        // Translator
        services.AddTransient<NllbPyTorchTranslationService>();
        // Scoped services
        services.AddHttpClient("DesktopEyeClient", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "DesktopEye/1.0");
        });
        services.AddScoped<IDownloadService, DownloadService>();

        AddViewModels(services);
        // AddViews(services);

        return services;
    }

    private static void AddViewModels(this IServiceCollection services)
    {
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<ScreenCaptureViewModel>();
        services.AddTransient<ScreenCaptureActionsViewModel>();
    }

    // private static void AddViews(this IServiceCollection services)
    // {
    //     
    // }
}