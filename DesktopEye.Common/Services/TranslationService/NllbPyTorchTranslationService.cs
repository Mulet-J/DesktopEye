using Python.Runtime;

namespace DesktopEye.Common.Services.TranslationService;

public class NllbPyTorchTranslationService : ITranslationService
{
    public static string Translate(string input, string sourceLanguage, string targetLanguage)
    {
        Runtime.PythonDLL = "C:\\Users\\Rémi\\AppData\\Local\\Programs\\Python\\Python313\\python313.dll";
        // var pathToVirtualEnv = @"C:\Users\Rémi\RiderProjects\DesktopEye.Common\.venv";

        // be sure not to overwrite your existing "PATH" environmental variable.
        // var path = Environment.GetEnvironmentVariable("PATH").TrimEnd(';');
        // path = string.IsNullOrEmpty(path) ? pathToVirtualEnv : path + ";" + pathToVirtualEnv;
        // Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);
        // Environment.SetEnvironmentVariable("PATH", pathToVirtualEnv, EnvironmentVariableTarget.Process);

        // Environment.SetEnvironmentVariable("PYTHONHOME", pathToVirtualEnv, EnvironmentVariableTarget.Process);
        // Environment.SetEnvironmentVariable("PYTHONPATH",
        //     $"{pathToVirtualEnv}\\Lib\\site-packages;{pathToVirtualEnv}\\Lib", EnvironmentVariableTarget.Process);

        PythonEngine.Initialize();

        // PythonEngine.PythonHome = pathToVirtualEnv;
        // PythonEngine.PythonPath = Environment.GetEnvironmentVariable("PYTHONPATH", EnvironmentVariableTarget.Process);
        string result;

        using (Py.GIL())
        {
            dynamic sys = Py.Import("sys");
            sys.path.append(
                "C:\\Users\\Rémi\\RiderProjects\\DesktopEye.Common\\DesktopEye.Common\\Services\\TranslationService\\"); // directory containing nllb_wrapper.py

            dynamic nllb = Py.Import("nllb");
            result = nllb.translate(input, sourceLanguage, targetLanguage).ToString();
        }

        PythonEngine.Shutdown();
        return result;
    }
}