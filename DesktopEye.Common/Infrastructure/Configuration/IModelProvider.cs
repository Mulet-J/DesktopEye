using System.Collections.Generic;
using System.Threading.Tasks;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Infrastructure.Models;

namespace DesktopEye.Common.Infrastructure.Configuration;

public interface IModelProvider
{
    public Task<bool> Process(List<Model> userCustomModels, List<Language> userSelectedOcrLanguages);
}