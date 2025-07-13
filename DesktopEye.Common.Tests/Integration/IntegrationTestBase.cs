using Avalonia;
using Avalonia.Media.Imaging;
using Bugsnag;
using Bugsnag.Payload;
using DesktopEye.Common.Domain.Features.OpticalCharacterRecognition;
using DesktopEye.Common.Domain.Features.OpticalCharacterRecognition.Interfaces;
using DesktopEye.Common.Domain.Features.TextClassification;
using DesktopEye.Common.Domain.Features.TextClassification.Interfaces;
using DesktopEye.Common.Domain.Features.TextTranslation;
using DesktopEye.Common.Domain.Features.TextTranslation.Interfaces;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Infrastructure.Models;
using DesktopEye.Common.Infrastructure.Services.ApplicationPath;
using DesktopEye.Common.Infrastructure.Services.Conda;
using DesktopEye.Common.Infrastructure.Services.Download;
using DesktopEye.Common.Infrastructure.Services.Python;
using DesktopEye.Common.Infrastructure.Services.TrainedModel;
using DesktopEye.Common.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using Xunit.Abstractions;
using Exception = System.Exception;

namespace DesktopEye.Common.Tests.Integration;

/// <summary>
/// Classe de base pour les tests d'intégration avec configuration complète des services
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private ITestOutputHelper Output { get; }
    private string TestDataDirectory { get; }

    protected IntegrationTestBase(ITestOutputHelper output)
    {
        Output = output;
        TestDataDirectory = Path.Combine(PathHelper.GetTestResultsPath(), "IntegrationTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(TestDataDirectory);
    }

    public virtual async Task InitializeAsync()
    {
        // Initialiser Avalonia pour les tests de bitmap
        InitializeAvalonia();

        // Configuration des services
        var services = new ServiceCollection();
        ConfigureLogging(services);
        ConfigureInfrastructureServices(services);
        ConfigureOrchestrators(services);
        RegisterDomainServices(services);

        services.BuildServiceProvider();
        
        await SetupTestDataAsync();
    }

    public virtual async Task DisposeAsync()
    {
        CleanupTestDirectory();
        await Task.CompletedTask;
    }

    #region Configuration Methods

    private static void InitializeAvalonia()
    {
        try
        {
            AppBuilder.Configure<Avalonia.Application>()
                .UsePlatformDetect()
                .WithInterFont()
                .SetupWithoutStarting();
        } catch (Exception ex)
        {
            ;
        }
       
    }

    private void ConfigureLogging(IServiceCollection services)
    {
        services.AddLogging(config =>
        {
            config.AddConsole();
            config.SetMinimumLevel(LogLevel.Debug);
        });
    }

    private void ConfigureInfrastructureServices(IServiceCollection services)
    {
        // Services d'infrastructure
        services.AddSingleton<IPathService>(_ => new TestPathService(TestDataDirectory));
        services.AddSingleton<ICondaService, MockCondaService>();
        services.AddSingleton<IPythonRuntimeManager, MockPythonRuntimeManager>();
        services.AddSingleton<IModelDownloadService, MockModelDownloadService>();
        services.AddSingleton<IModelStorageService, MockModelStorageService>();
        services.AddSingleton<IClient, MockBugsnagClient>();

        // Services configurables par les classes dérivées
        ConfigureSpecificServices(services);
    }

    private void ConfigureOrchestrators(IServiceCollection services)
    {
        // Orchestrateurs réels - c'est ce qu'on teste en intégration
        services.AddSingleton<IOcrOrchestrator, OcrOrchestrator>();
        services.AddSingleton<ITextClassifierOrchestrator, TextClassifierOrchestrator>();
        services.AddSingleton<ITranslationOrchestrator, TranslationOrchestrator>();
    }

    /// <summary>
    /// Configure les services spécifiques aux tests (à surcharger dans les classes dérivées)
    /// </summary>
    protected virtual void ConfigureSpecificServices(IServiceCollection services)
    {
        // Default: mock download service
        services.AddSingleton<IDownloadService, MockDownloadService>();
    }

    /// <summary>
    /// Enregistre les services de domaine (à implémenter dans les classes dérivées)
    /// </summary>
    protected abstract void RegisterDomainServices(IServiceCollection services);

    #endregion

    #region Test Data Setup

    protected virtual async Task SetupTestDataAsync()
    {
        await CreateTestImagesAsync();
        await CreateTestModelsAsync();
    }

    private async Task CreateTestImagesAsync()
    {
        var imagesDirectory = Path.Combine(TestDataDirectory, "Images");
        Directory.CreateDirectory(imagesDirectory);

        var testImages = new[]
        {
            ("english_text.png", "Hello World", Language.English),
            ("french_text.png", "Bonjour le monde", Language.French),
            ("german_text.png", "Hallo Welt", Language.German),
            ("spanish_text.png", "Hola mundo", Language.Spanish),
            ("long_english.png", "This is a longer text with multiple words that should test the OCR capabilities more thoroughly.", Language.English),
            ("mixed_text.png", "Hello Bonjour Hola", Language.English),
            ("empty.png", "", Language.English)
        };

        foreach (var (filename, text, language) in testImages)
        {
            await CreateTestImageAsync(imagesDirectory, filename, text, language);
        }
    }

    private async Task CreateTestImageAsync(string directory, string filename, string text, Language language)
    {
        var path = Path.Combine(directory, filename);
        
        if (string.IsNullOrEmpty(text))
        {
            var emptyBitmap = TestImageHelper.CreateEmptyBitmap();
            await using var stream = File.Create(path);
            emptyBitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
        }
        else
        {
            var bitmap = TestImageHelper.CreateBitmapWithText(text, language);
            await using var stream = File.Create(path);
            bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
        }
        
        await Task.CompletedTask;
    }

    private async Task CreateTestModelsAsync()
    {
        var modelsDirectory = Path.Combine(TestDataDirectory, "Models");
        Directory.CreateDirectory(modelsDirectory);
        
        // Créer des fichiers de modèles factices
        var modelDirs = new[] { "tesseract", "ntextcat", "fasttext", "nllb-pytorch" };
        foreach (var modelDir in modelDirs)
        {
            var dir = Path.Combine(modelsDirectory, modelDir);
            Directory.CreateDirectory(dir);
            
            // Créer un fichier de modèle factice
            var modelFile = Path.Combine(dir, "model.bin");
            await File.WriteAllTextAsync(modelFile, "fake model data");
        }
    }

    #endregion

    #region Helper Methods

    protected Bitmap LoadTestImage(string filename)
    {
        var path = Path.Combine(TestDataDirectory, "Images", filename);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Test image not found: {path}");
            
        return new Bitmap(path);
    }

    protected void LogTestStep(string step, string details = "")
    {
        Output.WriteLine($"[STEP] {step}");
        if (!string.IsNullOrEmpty(details))
            Output.WriteLine($"  {details}");
    }

    protected void LogTestResult(string operation, object result)
    {
        Output.WriteLine($"[RESULT] {operation}: {result}");
    }

    protected void LogPerformance(string operation, TimeSpan elapsed)
    {
        Output.WriteLine($"[PERF] {operation} took {elapsed.TotalMilliseconds:F2}ms");
    }

    private void CleanupTestDirectory()
    {
        try
        {
            if (Directory.Exists(TestDataDirectory))
                Directory.Delete(TestDataDirectory, true);
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    #endregion
}

#region Test Infrastructure Services

/// <summary>
/// Service de chemins personnalisé pour les tests
/// </summary>
public class TestPathService : IPathService
{
    public TestPathService(string baseDirectory)
    {
        AppDataDirectory = Path.Combine(baseDirectory, "AppData");
        LocalAppDataDirectory = Path.Combine(baseDirectory, "LocalAppData");
        ModelsDirectory = Path.Combine(baseDirectory, "Models");
        CacheDirectory = Path.Combine(baseDirectory, "Cache");
        CondaDirectory = Path.Combine(baseDirectory, "Conda");
        DownloadsDirectory = Path.Combine(baseDirectory, "Downloads");
        
        // Créer les répertoires
        var directories = new[] { AppDataDirectory, LocalAppDataDirectory, ModelsDirectory, CacheDirectory, CondaDirectory, DownloadsDirectory };
        foreach (var dir in directories)
        {
            Directory.CreateDirectory(dir);
        }
    }

    public string AppDataDirectory { get; }
    public string LocalAppDataDirectory { get; }
    public string CondaDirectory { get; }
    public string ModelsDirectory { get; }
    public string CacheDirectory { get; }
    public string DownloadsDirectory { get; }
}

/// <summary>
/// Helper pour créer des images de test
/// </summary>
public static class TestImageHelper
{
    public static SKBitmap CreateBitmapWithText(string text, Language language)
    {
        var width = 400 + text.Length * 5; // Ajuster la largeur selon le texte
        var height = 100;
        var bitmap = new SKBitmap(width, height);
        
        using var canvas = new SKCanvas(bitmap);
        
        // Fond blanc
        canvas.Clear(SKColors.White);
        
        if (string.IsNullOrEmpty(text))
            return bitmap;
        
        // Configuration du texte selon la langue
        var fontFamily = GetFontFamilyForLanguage(language);
        var fontSize = 20f;
        
        using var paint = new SKPaint();
        paint.Color = SKColors.Black;
        paint.TextSize = fontSize;
        paint.IsAntialias = true;
        paint.Typeface = SKTypeface.FromFamilyName(fontFamily);

        // Centrer le texte
        var textBounds = new SKRect();
        paint.MeasureText(text, ref textBounds);
        
        var x = (width - textBounds.Width) / 2;
        var y = (height - textBounds.Height) / 2 + textBounds.Height;
        
        canvas.DrawText(text, x, y, paint);
        
        return bitmap;
    }

    public static SKBitmap CreateEmptyBitmap()
    {
        var bitmap = new SKBitmap(200, 100);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        return bitmap;
    }

    public static SKBitmap CreateNoisyBitmap(string text)
    {
        var bitmap = CreateBitmapWithText(text, Language.English);
        
        using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint();
        paint.Color = SKColors.LightGray;

        // Ajouter du bruit
        var random = new Random(42); // Seed fixe pour la reproductibilité
        for (int i = 0; i < 50; i++)
        {
            var x = random.Next(bitmap.Width);
            var y = random.Next(bitmap.Height);
            canvas.DrawCircle(x, y, 1, paint);
        }
        
        return bitmap;
    }

    private static string GetFontFamilyForLanguage(Language language)
    {
        return language switch
        {
            Language.Japanese => "Noto Sans CJK JP",
            Language.Chinese => "Noto Sans CJK SC",
            Language.Korean => "Noto Sans CJK KR",
            _ => "Arial"
        };
    }
}

#endregion

#region Basic Mock Services

/// <summary>
/// Services mockés de base pour l'infrastructure
/// </summary>
public class MockCondaService : ICondaService
{
    public bool IsInstalled => true;
    public string CondaExecutablePath => "/mock/conda";
    public string PythonDllPath => "/mock/python.dll";
    
    public Task<bool> InstallMinicondaAsync() => Task.FromResult(true);
    public Task<bool> InstallPackageUsingCondaAsync(CondaInstallInstruction instruction, string? environmentName = null) => Task.FromResult(true);
    public Task<bool> InstallPackageUsingCondaAsync(List<CondaInstallInstruction> instruction, string? environmentName = null) => Task.FromResult(true);
    public Task<bool> InstallPackageUsingPipAsync(List<string> packages, string? environmentName = null) => Task.FromResult(true);
    public Task<string> ExecuteCondaCommandAsync(string command, string? environmentName = null) => Task.FromResult("Mock conda output");
}

public class MockPythonRuntimeManager : IPythonRuntimeManager
{
    public int DependentClassCount => 0;
    public bool IsRuntimeInitialized => true;
    
    public void StartRuntime(object caller) { }
    public void StopRuntime(object caller) { }
    public void ForceShutdown() { }
    public void ExecuteWithGil(Action func) => func();
    public async Task ExecuteWithGilAsync(Action func) => await Task.Run(func);
    public T ExecuteWithGil<T>(Func<T> func) => func();
    public async Task<T> ExecuteWithGilAsync<T>(Func<T> func) => await Task.Run(func);
    public void Dispose() { }
}

public class MockDownloadService : IDownloadService
{
    public Task<bool> DownloadFileAsync(string? url, string destinationPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.WriteAllText(destinationPath, $"Mock downloaded content from {url}");
        return Task.FromResult(true);
    }
}

public class MockModelDownloadService : IModelDownloadService
{
    public Task<bool> DownloadModelAsync(Model model) => Task.FromResult(true);
}

public class MockModelStorageService : IModelStorageService
{
    public bool IsModelAvailable(Model model) => true;
}
public class MockBugsnagClient : IClient
{
    public MockBugsnagClient(IBreadcrumbs breadcrumbs, ISessionTracker sessionTracking, IConfiguration configuration)
    {
        Breadcrumbs = breadcrumbs;
        SessionTracking = sessionTracking;
        Configuration = configuration;
    }

    public void Notify(Exception exception)
    {
        ;
    }

    public void Notify(Exception exception, Middleware callback)
    {
        ;
    }

    public void Notify(Exception exception, Severity severity)
    {
        ;
    }

    public void Notify(Exception exception, Severity severity, Middleware callback)
    {
        ;
    }

    public void Notify(Exception exception, HandledState handledState)
    {
        ;
    }

    public void Notify(Exception exception, HandledState handledState, Middleware callback)
    {
        ;
    }

    public void Notify(Report report, Middleware callback)
    {
        ;
    }

    public void BeforeNotify(Middleware middleware)
    {
        // No-op for testing
    }

    public IBreadcrumbs Breadcrumbs { get; }
    public ISessionTracker SessionTracking { get; }
    public IConfiguration Configuration { get; }
}

#endregion