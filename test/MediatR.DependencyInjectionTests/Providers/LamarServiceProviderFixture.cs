using Lamar;
using MediatR.DependencyInjectionTests.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.DependencyInjectionTests.Providers;

public class LamarServiceProviderFixture : BaseServiceProviderFixture
{
    public override IServiceProvider Provider
    {
        get
        {
            var services = new ServiceCollection();
            services.AddFakeLogging();
            services.AddMediatR(x => x.RegisterServicesFromAssemblyContaining(typeof(Pong)));
            var c = new Container(services);
            return c.ServiceProvider;
        }
    }
}