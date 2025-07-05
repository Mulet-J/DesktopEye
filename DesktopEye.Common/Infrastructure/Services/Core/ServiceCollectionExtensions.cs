using Bugsnag.AspNet.Core;
using DesktopEye.Common.Application.ViewModels;
using DesktopEye.Common.Domain.Features.OpticalCharacterRecognition;
using DesktopEye.Common.Domain.Features.OpticalCharacterRecognition.Interfaces;
using DesktopEye.Common.Domain.Features.TextClassification;
using DesktopEye.Common.Domain.Features.TextClassification.Interfaces;
using DesktopEye.Common.Domain.Features.TextTranslation;
using DesktopEye.Common.Domain.Features.TextTranslation.Interfaces;
using DesktopEye.Common.Domain.Models.OpticalCharacterRecognition;
using DesktopEye.Common.Domain.Models.TextClassification;
using DesktopEye.Common.Domain.Models.TextTranslation;
using DesktopEye.Common.Infrastructure.Services.ApplicationPath;
using DesktopEye.Common.Infrastructure.Services.Conda;
using DesktopEye.Common.Infrastructure.Services.Download;
using DesktopEye.Common.Infrastructure.Services.Python;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MainViewModel = DesktopEye.Common.Application.ViewModels.MainViewModel;
using ScreenCaptureActionsViewModel = DesktopEye.Common.Application.ViewModels.ScreenCapture.ScreenCaptureActionsViewModel;
using ScreenCaptureViewModel = DesktopEye.Common.Application.ViewModels.ScreenCapture.ScreenCaptureViewModel;

namespace DesktopEye.Common.Infrastructure.Services.Core;

public static class ServiceCollectionExtensions
{
    // https://docs.avaloniaui.net/docs/guides/implementation-guides/how-to-implement-dependency-injection
    public static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        services.AddLogging(config =>
        {
            config.AddConsole();
            config.SetMinimumLevel(LogLevel.Trace);
        });
        //Singleton services
        // services.AddSingleton<IOcrService, TesseractOcrService>();
        services.AddSingleton<IPathService, PathService>();
        services.AddSingleton<ICondaService, CondaService>();

        services.AddSingleton<IPythonRuntimeManager, PythonRuntimeManager>();

        services.AddSingleton<IOcrManager, OcrManager>();
        services.AddSingleton<ITextClassifierManager, TextClassifierManager>();
        services.AddSingleton<ITranslationManager, TranslationManager>();
        services.AddSingleton<ServicesPreloader>();
        // Transient services
        // Ocr
        services.AddKeyedTransient<IOcrService, TesseractOcrService>(OcrType.Tesseract);
        // Classifier
        services.AddKeyedTransient<ITextClassifierService, FastTextClassifierService>(ClassifierType.FastText);
        services.AddKeyedTransient<ITextClassifierService, NTextCatClassifierService>(ClassifierType.NTextCat);
        // Translator
        services.AddKeyedTransient<ITranslationService, NllbPyTorchTranslationService>(TranslationType.Nllb);
        // Scoped services
        services.AddHttpClient("DesktopEyeClient",
            client => { client.DefaultRequestHeaders.Add("User-Agent", "DesktopEye/1.0"); });
        services.AddScoped<IDownloadService, DownloadService>();
        
        AddExternalServices(services);
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
    
    private static void AddExternalServices(this IServiceCollection services)
    {
        // Add external services here if needed
        // Industrialisation - bugsnag - bug and issue reporting
        services.AddBugsnag(configuration =>
        {
            configuration.ApiKey = "80808d682d0824d1e39b970ba69feffd";
            configuration.ReleaseStage = "production";
        });
    }
}