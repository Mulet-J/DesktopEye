using DesktopEye.Common.Infrastructure.Configuration.Classes;
using DesktopEye.Common.Infrastructure.Configuration.Interfaces;
using DesktopEye.Common.Infrastructure.Services.PathValidation;
using Moq;

namespace DesktopEye.Common.Application.ViewModels.Setup.Design;

public class DesignSetupPathViewModel : SetupPathViewModel
{
    public DesignSetupPathViewModel() : base(CreateMockAppConfigService(), new PathValidationService())
    {
        LocalAppDataFolder = "/some/editable/path";
    }

    private static IAppConfigService CreateMockAppConfigService()
    {
        var config = new AppConfig
        {
            LocalAppDataDirectory = "/default/design/path"
        };

        var mockService = new Mock<IAppConfigService>();
        mockService.Setup(s => s.Config).Returns(config);

        return mockService.Object;
    }
}