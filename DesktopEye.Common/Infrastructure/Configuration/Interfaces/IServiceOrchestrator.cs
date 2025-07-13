using System;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopEye.Common.Infrastructure.Configuration.Interfaces;

public interface IServiceOrchestrator<TService, TServiceType> : IDisposable
{
    TServiceType CurrentServiceType { get; }

    /// <summary>
    ///     Switches to a different service type asynchronously
    /// </summary>
    /// <param name="serviceType">The type of service to switch to</param>
    /// <param name="loadModel">If the model should be loaded in the process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the switch was successful</returns>
    Task<bool> SwitchToAsync(TServiceType serviceType, bool loadModel = true,
        CancellationToken cancellationToken = default);
}