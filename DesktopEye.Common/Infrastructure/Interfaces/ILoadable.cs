using System.Threading;
using System.Threading.Tasks;

namespace DesktopEye.Common.Infrastructure.Interfaces;

/// <summary>Provides a mechanism for loading resources.</summary>
public interface ILoadable
{
    public Task<bool> LoadRequiredAsync(string? modelName = null,
        CancellationToken cancellationToken = default);
}