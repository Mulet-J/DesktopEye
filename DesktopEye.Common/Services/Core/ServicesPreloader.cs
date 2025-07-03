using System;
using DesktopEye.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace DesktopEye.Common.Services.Core;

public class ServicesPreloader
{
    private readonly ILogger<ServicesPreloader> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Bugsnag.IClient _bugsnag;

    public ServicesPreloader(IServiceProvider serviceProvider, ILogger<ServicesPreloader> logger, Bugsnag.IClient bugsnag)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _bugsnag = bugsnag ?? throw new ArgumentNullException(nameof(bugsnag));
    }

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