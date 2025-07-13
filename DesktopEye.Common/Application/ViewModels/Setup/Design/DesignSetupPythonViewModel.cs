using DesktopEye.Common.Infrastructure.Services.Conda;
using Moq;

namespace DesktopEye.Common.Application.ViewModels.Setup.Design;

public class DesignSetupPythonViewModel : SetupPythonViewModel
{
    public DesignSetupPythonViewModel() : base(CreateMockCondaService())
    {
    }

    private static ICondaService CreateMockCondaService()
    {
        var mockService = new Mock<ICondaService>();
        mockService.Setup(s => s.IsInstalled).Returns(false);

        return mockService.Object;
    }
}