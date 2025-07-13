using DesktopEye.Common.Infrastructure.Configuration;
using Moq;

namespace DesktopEye.Common.Application.ViewModels.Setup.Design;

public class DesignSetupModelsViewModel : SetupModelsViewModel
{
    public DesignSetupModelsViewModel() : base(CreateMockModelDownloadService())
    {
    }

    private static IModelProvider CreateMockModelDownloadService()
    {
        var mockService = new Mock<IModelProvider>();

        return mockService.Object;
    }
}