using System.Linq;
using Deluxe.MediatR.MicrosoftExtensionsDI;

namespace Deluxe.MediatR.Tests.MicrosoftExtensionsDI;

public class DuplicateAssemblyResolutionTests
{
    private readonly IServiceProvider _provider;

    public DuplicateAssemblyResolutionTests()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddFakeLogging();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(Ping).Assembly, typeof(Ping).Assembly));
        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public void ShouldResolveNotificationHandlersOnlyOnce()
    {
        _provider.GetServices<INotificationHandler<Pinged>>().Count().ShouldBe(4);
    }
}