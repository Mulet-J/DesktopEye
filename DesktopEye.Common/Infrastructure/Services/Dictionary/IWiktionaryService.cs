using System.Threading.Tasks;
using DesktopEye.Common.Domain.Models.Dictionary;

namespace DesktopEye.Common.Infrastructure.Services.Dictionary;

public interface IWiktionaryService
{
    public Task<WiktionaryResponse> GetDefinitionsAsync(string term);
}