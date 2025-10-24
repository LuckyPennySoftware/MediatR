using LightInject;
using LightInject.Microsoft.DependencyInjection;
using MediatR.DependencyInjectionTests.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.DependencyInjectionTests.Providers;

public class LightInjectServiceProviderFixture : BaseServiceProviderFixture
{
    public override IServiceProvider Provider
    {
        get
        {
            var services = new ServiceCollection();
            services.AddFakeLogging();
            services.AddMediatR(x => x.RegisterServicesFromAssemblyContaining(typeof(Pong)));

            var container = new ServiceContainer(new ContainerOptions()
            {
                EnableMicrosoftCompatibility = true
            });
            return container.CreateServiceProvider(services);
        }
    }
}