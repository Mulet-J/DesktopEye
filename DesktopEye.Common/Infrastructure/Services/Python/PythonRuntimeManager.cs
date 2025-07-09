using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DesktopEye.Common.Infrastructure.Services.ApplicationPath;
using DesktopEye.Common.Infrastructure.Services.Conda;
using Microsoft.Extensions.Logging;
using Python.Runtime;
using PythonException = DesktopEye.Common.Infrastructure.Exceptions.PythonException;

namespace DesktopEye.Common.Infrastructure.Services.Python;

public class PythonRuntimeManager : IPythonRuntimeManager
{
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ICondaService _condaService;
    private readonly HashSet<object> _dependentClasses;
    private readonly object _lock = new();
    private readonly ILogger<PythonRuntimeManager> _logger;
    private readonly IPathService _pathService;
    private bool _isDisposed;

    public PythonRuntimeManager(IPathService pathService, ICondaService condaService,
        ILogger<PythonRuntimeManager> logger)
    {
        _pathService = pathService;
        _condaService = condaService;
        _logger = logger;
        _dependentClasses = new HashSet<object>();
    }

    /// <summary>
    ///     Starts the Python runtime if needed and registers the calling class as a dependent.
    /// </summary>
    /// <param name="caller">The class instance that needs the Python runtime</param>
    /// <exception cref="ArgumentNullException">Thrown when caller is null</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the manager has been disposed</exception>
    /// <exception cref="InvalidOperationException">Thrown when Python runtime initialization fails</exception>
    public void StartRuntime(object caller)
    {
        if (caller == null)
            throw new PythonException(nameof(caller));

        lock (_lock)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(PythonRuntimeManager));

            try
            {
                // Initialize Python runtime if not already done
                if (!IsRuntimeInitialized)
                {
                    _logger.LogInformation("Initializing Python runtime");
                    var pathSeparator = Path.PathSeparator;
                    var pathToVirtualEnv = _pathService.CondaDirectory;

                    var path = Environment.GetEnvironmentVariable("PATH")?.TrimEnd(pathSeparator);
                    path = string.IsNullOrEmpty(path)
                        ? pathToVirtualEnv
                        : pathToVirtualEnv + pathSeparator +
                          path;
                    Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);

                    Runtime.PythonDLL = _condaService.PythonDllPath;
                    PythonEngine.PythonHome = pathToVirtualEnv;
                    PythonEngine.Initialize();
                    // Required to allow async functions to use the GIL
                    PythonEngine.BeginAllowThreads();
                    _logger.LogInformation("Python runtime initialized successfully");
                }

                // Add the caller to the set of dependent classes
                var wasAdded = _dependentClasses.Add(caller);
                if (wasAdded)
                    _logger.LogDebug("Added dependent class: {CallerType}. Total dependents: {Count}",
                        caller.GetType().Name, _dependentClasses.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Python runtime");
                throw new InvalidOperationException("Failed to initialize Python runtime", ex);
            }
        }
    }

    /// <summary>
    ///     Removes the calling class from dependents and shuts down the Python runtime if no classes depend on it.
    /// </summary>
    /// <param name="caller">The class instance that no longer needs the Python runtime</param>
    /// <exception cref="ArgumentNullException">Thrown when caller is null</exception>
    public void StopRuntime(object caller)
    {
        ArgumentNullException.ThrowIfNull(caller);

        lock (_lock)
        {
            if (_isDisposed)
                return; // Already disposed, nothing to do

            // Remove the caller from dependent classes
            var wasRemoved = _dependentClasses.Remove(caller);
            if (wasRemoved)
                _logger.LogDebug("Removed dependent class: {CallerType}. Remaining dependents: {Count}",
                    caller.GetType().Name, _dependentClasses.Count);

            // If no classes depend on the runtime, shut it down
            if (_dependentClasses.Count != 0 || !IsRuntimeInitialized) return;
            try
            {
                _logger.LogInformation("Shutting down Python runtime - no more dependents");
                PythonEngine.Shutdown();
                _logger.LogInformation("Python runtime shutdown completed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception during Python runtime shutdown");
            }
        }
    }

    /// <summary>
    ///     Gets the number of classes currently depending on the Python runtime.
    /// </summary>
    public int DependentClassCount
    {
        get
        {
            lock (_lock)
            {
                return _dependentClasses.Count;
            }
        }
    }

    /// <summary>
    ///     Gets whether the Python runtime is currently initialized.
    /// </summary>
    public bool IsRuntimeInitialized => PythonEngine.IsInitialized;

    /// <summary>
    ///     Forces shutdown of the Python runtime regardless of dependent classes.
    ///     Use with caution as this may cause issues for classes still using Python.
    /// </summary>
    public void ForceShutdown()
    {
        lock (_lock)
        {
            if (IsRuntimeInitialized)
                try
                {
                    _logger.LogWarning("Force shutting down Python runtime with {Count} remaining dependents",
                        _dependentClasses.Count);
                    PythonEngine.Shutdown();
                    _dependentClasses.Clear();
                    _logger.LogInformation("Forced Python runtime shutdown completed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception during forced Python runtime shutdown");
                }
        }
    }

    /// <summary>
    ///     Disposes the manager and shuts down the Python runtime.
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            if (!_isDisposed)
            {
                _logger.LogInformation("Disposing PythonRuntimeManager");
                ForceShutdown();
                _isDisposed = true;
            }
        }
    }

    #region ExecuteWithGil

    /// <summary>
    ///     Executes a function with Python GIL (Global Interpreter Lock) protection
    /// </summary>
    /// <param name="func">The function to execute</param>
    public void ExecuteWithGil(Action func)
    {
        _semaphore.Wait();
        try
        {
            using (Py.GIL())
            {
                func();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    ///     Executes a function with Python GIL (Global Interpreter Lock) protection (async version)
    /// </summary>
    /// <param name="func">The function to execute</param>
    public async Task ExecuteWithGilAsync(Action func)
    {
        await _semaphore.WaitAsync();
        try
        {
            using (Py.GIL())
            {
                func();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    ///     Executes a function with Python GIL (Global Interpreter Lock) protection and returns a result
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="func">The function to execute</param>
    /// <returns>The result of the function</returns>
    public T ExecuteWithGil<T>(Func<T> func)
    {
        _semaphore.Wait();
        try
        {
            using (Py.GIL())
            {
                return func();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    ///     Executes a function with Python GIL (Global Interpreter Lock) protection and returns a result (async version)
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="func">The function to execute</param>
    /// <returns>The result of the function</returns>
    public async Task<T> ExecuteWithGilAsync<T>(Func<T> func)
    {
        await _semaphore.WaitAsync();
        try
        {
            using (Py.GIL())
            {
                return func();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion
}