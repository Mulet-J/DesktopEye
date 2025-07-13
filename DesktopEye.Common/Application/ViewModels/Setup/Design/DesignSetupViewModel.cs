using System;
using DesktopEye.Common.Infrastructure.Configuration.Interfaces;
using Moq;

namespace DesktopEye.Common.Application.ViewModels.Setup.Design;

public class DesignSetupViewModel : SetupViewModel
{
    public DesignSetupViewModel() : base(CreateMockServiceProvider(), CreateMockAppConfigService())
    {
    }

    private static IServiceProvider CreateMockServiceProvider()
    {
        var mockService = new Mock<IServiceProvider>();

        return mockService.Object;
    }

    private static IAppConfigService CreateMockAppConfigService()
    {
        var mockService = new Mock<IAppConfigService>();

        return mockService.Object;
    }
}