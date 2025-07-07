using System;
using DesktopEye.Common.Infrastructure.Configuration.Interfaces;
using Microsoft.Extensions.Logging;

namespace DesktopEye.Common.Infrastructure.Configuration;

/// <summary>
/// Handles initialization and preloading of application services
/// </summary>
public class ServicesLoader
{
    private readonly ILogger<ServicesLoader> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Bugsnag.IClient _bugsnag;

    public ServicesLoader(IServiceProvider serviceProvider, ILogger<ServicesLoader> logger, Bugsnag.IClient bugsnag)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _bugsnag = bugsnag ?? throw new ArgumentNullException(nameof(bugsnag));
    }

    /// <summary>
    /// Initializes the service loader and preloads all registered services
    /// </summary>
    /// <param name="serviceTypes">
    /// An array of service types to be preloaded. Each type should implement the `ILoadable` interface
    /// if it requires asynchronous loading.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the `serviceTypes` parameter is null.
    /// </exception>
    public void PreloadServices(params Type[] serviceTypes)
    {
        if (serviceTypes == null) throw new ArgumentNullException(nameof(serviceTypes));

        foreach (var type in serviceTypes)
            try
            {
                var service = _serviceProvider.GetService(type);
                if (service is ILoadable loadableService) loadableService.LoadRequiredAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to preload service of type {type.FullName}: {ex.Message}");
                _bugsnag.Notify(ex);
            }
    }
}