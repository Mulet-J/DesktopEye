using System.Collections.Generic;
using DesktopEye.Common.Infrastructure.Models;
using TesseractOCR.Enums;

namespace DesktopEye.Common.Infrastructure.Configuration;

public interface IModelProvider
{
    public List<Model> Process(List<Model> userCustomModels, List<Language> userSelectedOcrLanguages);
}