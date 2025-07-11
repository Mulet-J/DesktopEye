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
using DesktopEye.Common.Infrastructure.Services.Dialog;
using DesktopEye.Common.Infrastructure.Services.Dictionary;
using DesktopEye.Common.Infrastructure.Services.Download;
using DesktopEye.Common.Infrastructure.Services.Python;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MainViewModel = DesktopEye.Common.Application.ViewModels.MainViewModel;
using ScreenCaptureActionsViewModel =
    DesktopEye.Common.Application.ViewModels.ScreenCapture.ScreenCaptureActionsViewModel;
using ScreenCaptureViewModel = DesktopEye.Common.Application.ViewModels.ScreenCapture.ScreenCaptureViewModel;

namespace DesktopEye.Common.Infrastructure.Configuration;

public static class ServicesRegistration
{
    /// <summary>
    /// Registers all application services for dependency injection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The configured service collection</returns>
    public static void InjectApplicationServices(this IServiceCollection services)
    {
        RegisterLogging(services);
        RegisterInfrastructureServices(services);
        RegisterDomainServices(services);
        RegisterExternalServices(services);
        RegisterViewModels(services);
    }

    /// <summary>
    /// Registers logging configuration
    /// </summary>
    private static void RegisterLogging(IServiceCollection services)
    {
        services.AddLogging(config =>
        {
            config.AddConsole();
            config.SetMinimumLevel(LogLevel.Trace);
        });
    }

    /// <summary>
    /// Registers infrastructure layer services
    /// </summary>
    private static void RegisterInfrastructureServices(IServiceCollection services)
    {
        // Singleton infrastructure services
        services.AddSingleton<IPathService, PathService>();
        services.AddSingleton<ICondaService, CondaService>();
        services.AddSingleton<IPythonRuntimeManager, PythonRuntimeManager>();
        services.AddSingleton<ServicesLoader>();
        services.AddSingleton<IDialogService, DialogService>();
        // Scoped infrastructure services
        services.AddHttpClient("DesktopEyeClient",
            client => { client.DefaultRequestHeaders.Add("User-Agent", "DesktopEye/1.0"); });
        services.AddScoped<IDownloadService, DownloadService>();
    }

    /// <summary>
    /// Registers domain layer services and managers
    /// </summary>
    private static void RegisterDomainServices(IServiceCollection services)
    {
        // Domain managers (singletons)
        services.AddSingleton<IOcrOrchestrator, OcrOrchestrator>();
        services.AddSingleton<ITextClassifierOrchestrator, TextClassifierOrchestrator>();
        services.AddSingleton<ITranslationOrchestrator, TranslationOrchestrator>();

        // Domain services by type (transient)
        RegisterOcrServices(services);
        RegisterTextClassificationServices(services);
        RegisterTranslationServices(services);
    }

    /// <summary>
    /// Registers OCR services with their respective keys
    /// </summary>
    private static void RegisterOcrServices(IServiceCollection services)
    {
        services.AddKeyedTransient<IOcrService, TesseractOcrService>(OcrType.Tesseract);
    }

    /// <summary>
    /// Registers text classification services with their respective keys
    /// </summary>
    private static void RegisterTextClassificationServices(IServiceCollection services)
    {
        services.AddKeyedTransient<ITextClassifierService, FastTextClassifierService>(ClassifierType.FastText);
        services.AddKeyedTransient<ITextClassifierService, NTextCatClassifierService>(ClassifierType.NTextCat);
    }

    /// <summary>
    /// Registers translation services with their respective keys
    /// </summary>
    private static void RegisterTranslationServices(IServiceCollection services)
    {
        services.AddKeyedTransient<ITranslationService, NllbPyTorchTranslationService>(TranslationType.Nllb);
    }

    /// <summary>
    /// Registers external services and monitoring tools
    /// </summary>
    private static void RegisterExternalServices(IServiceCollection services)
    {
        // Bug tracking and monitoring
        services.AddBugsnag(configuration =>
        {
            configuration.ApiKey = "80808d682d0824d1e39b970ba69feffd";
            configuration.ReleaseStage = "production";
        });

        services.AddSingleton<IWiktionaryService, WiktionaryService>();
    }

    /// <summary>
    /// Registers application ViewModels
    /// </summary>
    private static void RegisterViewModels(IServiceCollection services)
    {
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<ScreenCaptureViewModel>();
        services.AddTransient<ScreenCaptureActionsViewModel>();
    }

}