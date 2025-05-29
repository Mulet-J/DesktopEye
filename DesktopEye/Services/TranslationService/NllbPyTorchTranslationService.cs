using System;
using System.IO;
using Python.Runtime;

namespace DesktopEye.Services.TranslationService;

public class NllbPyTorchTranslationService : ITranslationService
{
    public static string Translate(string input, string sourceLanguage, string targetLanguage)
    {
        Runtime.PythonDLL = "C:\\Users\\Rémi\\AppData\\Local\\Programs\\Python\\Python313\\python313.dll";
        // var pathToVirtualEnv = @"C:\Users\Rémi\RiderProjects\DesktopEye\.venv";

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
            dynamic os = Py.Import("os");
            dynamic sys = Py.Import("sys");
            sys.path.append(
                "C:\\Users\\Rémi\\RiderProjects\\DesktopEye\\DesktopEye\\Services\\TranslationService\\"); // directory containing nllb_wrapper.py

            string pythonBinary = sys.executable.ToString();
            string venvPatha = os.environ.get("VIRTUAL_ENV", "Not in a virtual environment").ToString();

            System.Console.WriteLine("Python binary path: " + pythonBinary);
            System.Console.WriteLine("Virtual environment path: " + venvPatha);

            dynamic nllb = Py.Import("nllb");
            result = nllb.translate(input, sourceLanguage, targetLanguage).ToString();

            Console.WriteLine($"Translation: {result}");
        }

        PythonEngine.Shutdown();
        return result;
    }
}