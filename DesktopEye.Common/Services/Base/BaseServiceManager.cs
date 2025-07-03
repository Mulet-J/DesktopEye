using System;
using System.Threading;
using System.Threading.Tasks;
using DesktopEye.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DesktopEye.Common.Services.Base;

/// <summary>
///     Base class for service managers that handle switching between different service implementations
/// </summary>
/// <typeparam name="TService">The service interface type</typeparam>
/// <typeparam name="TServiceType">The enum type representing different service implementations</typeparam>
public abstract class BaseServiceManager<TService, TServiceType> : IBaseServiceManager<TService, TServiceType>,
    ILoadable
    where TService : class
    where TServiceType : struct, Enum
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly IServiceProvider _services;
    private readonly Bugsnag.IClient _bugsnag;
    protected readonly ILogger? Logger;

    private TService? _currentService;
    private bool _disposed;

    protected BaseServiceManager(IServiceProvider services, Bugsnag.IClient bugsnag, ILogger? logger = null)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _bugsnag = bugsnag ?? throw new ArgumentNullException(nameof(bugsnag));
        Logger = logger;

        Logger?.LogInformation("Initializing {ManagerType}", GetType().Name);

        // Initialize with default service but don't preload yet
        // ReSharper disable once VirtualMemberCallInConstructor
        var defaultServiceType = GetDefaultServiceType();
        SwitchToPrivate(defaultServiceType);
    }

    /// <summary>
    ///     Gets the current service instance
    /// </summary>
    private TService? CurrentService
    {
        get
        {
            ThrowIfDisposed();
            return _currentService;
        }
        set => _currentService = value;
    }

    /// <summary>
    ///     Gets the current service type
    /// </summary>
    public TServiceType CurrentServiceType { get; private set; }

    public virtual void Dispose()
    {
        if (_disposed) return;
        Logger?.LogDebug("Disposing {ManagerType}", GetType().Name);
        try
        {
            if (CurrentService != null)
            {
                if (CurrentService is IDisposable disposable) disposable.Dispose();
                CurrentService = null;
            }

            _semaphore.Dispose();
            _disposed = true;

            Logger?.LogInformation("{ManagerType} disposed successfully", GetType().Name);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error occurred while disposing {ManagerType}", GetType().Name);
            _bugsnag.Notify(ex);
        }
        GC.SuppressFinalize(this);
    }


    /// <summary>
    ///     Asynchronously load the current's model required
    /// </summary>
    /// <param name="model">Model name to preload (currently unused)</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> LoadRequiredAsync(string? model = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (CurrentService is not ILoadable loadableService) return true;
            Logger?.LogDebug("Loading model for service type: {ServiceType}", CurrentServiceType);
            var loadResult = await loadableService.LoadRequiredAsync(cancellationToken: cancellationToken);
            if (!loadResult)
                Logger?.LogWarning("Failed to load model for service type: {ServiceType}", CurrentServiceType);
            else
                Logger?.LogDebug("Successfully loaded model for service type: {ServiceType}", CurrentServiceType);
            return loadResult;
        }
        catch (Exception ex)
        {
            Logger?.LogError("Encountered an error when trying to load service type: {ServiceType}",
                CurrentServiceType);
            _bugsnag.Notify(ex);
            return false;
        }
    }


    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <summary>
    ///     Creates a service instance of the specified type
    /// </summary>
    /// <param name="serviceType">The type of service to create</param>
    /// <returns>The created service instance</returns>
    private TService CreateService(TServiceType serviceType)
    {
        var service = _services.GetKeyedService<TService>(serviceType);
        if (service == null)
            throw new InvalidOperationException($"Failed to inject service of type {serviceType}. " +
                                                "Ensure the service is registered in the dependency injection container.");
        return service;
    }

    /// <summary>
    ///     Gets the default service type to use during initialization
    /// </summary>
    /// <returns>The default service type</returns>
    protected abstract TServiceType GetDefaultServiceType();

    #region ExecuteWith

    /// <summary>
    ///     Executes an operation with the current service under semaphore protection
    /// </summary>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The operation result</returns>
    protected async Task<TResult> ExecuteWithServiceAsync<TResult>(
        Func<TService, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (CurrentService == null)
            {
                Logger?.LogError("Cannot execute operation - no service is currently selected");
                throw new InvalidOperationException("No service is currently selected");
            }

            return await operation(CurrentService, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Operation failed using {ServiceType}", CurrentServiceType);
            _bugsnag.Notify(ex);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    ///     Executes a synchronous operation with the current service under lock protection
    /// </summary>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <returns>The operation result</returns>
    protected TResult ExecuteWithService<TResult>(Func<TService, TResult> operation)
    {
        ThrowIfDisposed();

        _semaphore.Wait();
        try
        {
            if (CurrentService == null)
            {
                Logger?.LogError("Cannot execute operation - no service is currently selected");
                throw new InvalidOperationException("No service is currently selected");
            }

            try
            {
                return operation(CurrentService);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Operation failed using {ServiceType}", CurrentServiceType);
                _bugsnag.Notify(ex);
                throw;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion


    #region Switch

    /// <summary>
    ///     Switches to a different service type asynchronously
    /// </summary>
    /// <param name="serviceType">The type of service to switch to</param>
    /// <param name="loadModel">If the model should be loaded in the process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the switch was successful</returns>
    public async Task<bool> SwitchToAsync(TServiceType serviceType, bool loadModel = true,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            Logger?.LogDebug("Switching to service type: {ServiceType}", serviceType);

            if (CurrentServiceType.Equals(serviceType) && CurrentService != null)
            {
                Logger?.LogDebug("Already using service type: {ServiceType}", serviceType);
                return true;
            }

            // Dispose the current service
            if (CurrentService != null)
            {
                Logger?.LogDebug("Disposing current service: {CurrentType}", CurrentServiceType);
                if (CurrentService is IDisposable disposable) disposable.Dispose();
                CurrentService = null;
            }

            // Create new service
            CurrentService = CreateService(serviceType);
            CurrentServiceType = serviceType;

            Logger?.LogInformation("Switched to service type: {ServiceType}", serviceType);

            // Check if loadModel is true, if CurrentService is of type ILoadable, and execute LoadCurrentModelAsync
            // if both are true
            if (loadModel) await LoadRequiredAsync(null, cancellationToken);

            // Perform any additional initialization
            await OnServiceSwitchedAsync(serviceType, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to switch to service type: {ServiceType}", serviceType);
            _bugsnag.Notify(ex);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    ///     Synchronous method to switch service types (used during initialization)
    /// </summary>
    /// <param name="serviceType">The type of service to switch to</param>
    /// <param name="loadModel">If the model should be automatically loaded</param>
    private void SwitchToPrivate(TServiceType serviceType, bool loadModel = true)
    {
        Logger?.LogDebug("Synchronously switching to service type: {ServiceType}", serviceType);

        if (CurrentServiceType.Equals(serviceType) && CurrentService != null)
            return;

        if (CurrentService != null && CurrentService is IDisposable disposable) disposable.Dispose();

        CurrentService = CreateService(serviceType);
        CurrentServiceType = serviceType;

        // Fire and forget model loading
        if (loadModel)
            _ = Task.Run(async () =>
            {
                try
                {
                    await LoadRequiredAsync();
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex.ToString());
                    _bugsnag.Notify(ex);
                }
            });
    }

    /// <summary>
    ///     Called after a service has been successfully switched to allow for additional initialization
    /// </summary>
    /// <param name="serviceType">The service type that was switched to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected virtual Task OnServiceSwitchedAsync(TServiceType serviceType,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    #endregion
}