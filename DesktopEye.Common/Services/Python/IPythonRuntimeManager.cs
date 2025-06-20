using System;

namespace DesktopEye.Common.Services.Python;

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
}