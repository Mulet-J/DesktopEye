using System.Collections.Generic;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Infrastructure.Models;

namespace DesktopEye.Common.Infrastructure.Configuration;

public interface IModelProvider
{
    public List<Model> Process(List<Model> userCustomModels, List<Language> userSelectedOcrLanguages);
}