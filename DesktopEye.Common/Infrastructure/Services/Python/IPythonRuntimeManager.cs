using System;
using System.Threading.Tasks;

namespace DesktopEye.Common.Infrastructure.Services.Python;

public interface IPythonRuntimeManager : IDisposable
{
    /// <summary>
    ///     Gets the number of classes currently depending on the Python runtime.
    /// </summary>
    int DependentClassCount { get; }

    /// <summary>
    ///     Gets whether the Python runtime is currently initialized.
    /// </summary>
    bool IsRuntimeInitialized { get; }

    /// <summary>
    ///     Starts the Python runtime if needed and registers the calling class as a dependent.
    /// </summary>
    /// <param name="caller">The class instance that needs the Python runtime</param>
    void StartRuntime(object caller);

    /// <summary>
    ///     Removes the calling class from dependents and shuts down the Python runtime if no classes depend on it.
    /// </summary>
    /// <param name="caller">The class instance that no longer needs the Python runtime</param>
    void StopRuntime(object caller);

    /// <summary>
    ///     Forces shutdown of the Python runtime regardless of dependent classes.
    ///     Use with caution as this may cause issues for classes still using Python.
    /// </summary>
    void ForceShutdown();

    /// <summary>
    ///     Executes a function with Python GIL (Global Interpreter Lock) protection
    /// </summary>
    /// <param name="func">The function to execute</param>
    void ExecuteWithGil(Action func);

    /// <summary>
    ///     Executes a function with Python GIL (Global Interpreter Lock) protection (async version)
    /// </summary>
    /// <param name="func">The function to execute</param>
    Task ExecuteWithGilAsync(Action func);

    /// <summary>
    ///     Executes a function with Python GIL (Global Interpreter Lock) protection and returns a result
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="func">The function to execute</param>
    /// <returns>The result of the function</returns>
    T ExecuteWithGil<T>(Func<T> func);

    /// <summary>
    ///     Executes a function with Python GIL (Global Interpreter Lock) protection and returns a result (async version)
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="func">The function to execute</param>
    /// <returns>The result of the function</returns>
    Task<T> ExecuteWithGilAsync<T>(Func<T> func);
}