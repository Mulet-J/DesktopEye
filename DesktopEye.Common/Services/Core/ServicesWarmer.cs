using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DesktopEye.Common.Services.Core;

// TODO find a better way if possible
public class ServicesWarmer
{
    private readonly ILogger<ServicesWarmer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<Func<IServiceProvider, Task>> _warmupTasks;

    public ServicesWarmer(IServiceProvider serviceProvider, ILogger<ServicesWarmer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _warmupTasks = new List<Func<IServiceProvider, Task>>();
    }

    /// <summary>
    ///     Gets the count of registered services
    /// </summary>
    public int RegisteredServicesCount => _warmupTasks.Count;

    public ServicesWarmer AddService<T>(Func<T, Task> initializeAction) where T : class
    {
        _warmupTasks.Add(async serviceProvider =>
        {
            var service = serviceProvider.GetRequiredService<T>();
            var serviceName = typeof(T).Name;

            _logger.LogInformation("Warming up service: {ServiceName}", serviceName);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await initializeAction(service);
                stopwatch.Stop();
                _logger.LogInformation("Service {ServiceName} warmed up in {ElapsedMs}ms",
                    serviceName, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to warm up service {ServiceName} after {ElapsedMs}ms",
                    serviceName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        });

        return this;
    }

    /// <summary>
    ///     Registers a service type for warming up (assumes the service has a standard initialization method)
    /// </summary>
    public ServicesWarmer AddService<T>() where T : class
    {
        _warmupTasks.Add(async serviceProvider =>
        {
            var service = serviceProvider.GetRequiredService<T>();
            var serviceName = typeof(T).Name;

            _logger.LogInformation("Instantiating service: {ServiceName}", serviceName);

            // Just instantiate the service to trigger any constructor logic
            await Task.CompletedTask;
        });

        return this;
    }

    /// <summary>
    ///     Executes all registered warmup tasks
    /// </summary>
    public async Task WarmUpAsync(bool runInParallel = true)
    {
        if (_warmupTasks.Count == 0)
        {
            _logger.LogInformation("No services registered for warmup");
            return;
        }

        _logger.LogInformation("Starting warmup of {ServiceCount} services...", _warmupTasks.Count);
        var totalStopwatch = Stopwatch.StartNew();

        try
        {
            if (runInParallel)
            {
                var tasks = _warmupTasks.Select(task => task(_serviceProvider));
                await Task.WhenAll(tasks);
            }
            else
            {
                foreach (var task in _warmupTasks) await task(_serviceProvider);
            }

            totalStopwatch.Stop();
            _logger.LogInformation("All services warmed up successfully in {TotalElapsedMs}ms",
                totalStopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            totalStopwatch.Stop();
            _logger.LogError(ex, "Service warmup failed after {TotalElapsedMs}ms",
                totalStopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}