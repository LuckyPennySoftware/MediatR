using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using MediatR.DependencyInjectionTests.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.DependencyInjectionTests.Providers;

public class DryIocServiceProviderFixture : BaseServiceProviderFixture
{
    public override IServiceProvider Provider
    {
        get
        {
            var services = new ServiceCollection();
            services.AddFakeLogging();
            services.AddMediatR(x => x.RegisterServicesFromAssemblyContaining(typeof(Pong)));

            var container = new Container(Rules.MicrosoftDependencyInjectionRules);
            container.WithDependencyInjectionAdapter(services);
            return container.BuildServiceProvider();
        }
    }
}