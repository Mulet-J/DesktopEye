using System.Threading.Tasks;
using DesktopEye.Common.Domain.Models.Dictionary;

namespace DesktopEye.Common.Infrastructure.Services.Dictionary;

/// <summary>
/// Helper class for formatting Wiktionary dictionary responses into readable text.
/// </summary>
public interface IWiktionaryService
{
    /// <summary>
    /// Gets dictionary definitions for the specified term.
    /// </summary>
    /// <param name="term">The term to look up</param>
    /// <returns>A response containing definitions grouped by language</returns>
    public Task<WiktionaryResponse> GetDefinitionsAsync(string term);
}