using MediatR.DependencyInjectionTests.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.DependencyInjectionTests.Providers;

public class MicrosoftServiceProviderFixture : BaseServiceProviderFixture
{
    public override IServiceProvider Provider => new ServiceCollection()
        .AddFakeLogging()
        .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(PublicPing).Assembly))
        .BuildServiceProvider();
}