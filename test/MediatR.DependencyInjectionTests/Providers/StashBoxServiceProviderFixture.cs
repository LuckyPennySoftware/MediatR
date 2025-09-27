using MediatR.DependencyInjectionTests.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Stashbox;

namespace MediatR.DependencyInjectionTests.Providers;

public class StashBoxServiceProviderFixture : BaseServiceProviderFixture
{
    public override IServiceProvider Provider
    {
        get
        {

            var services = new ServiceCollection();
            services.AddFakeLogging();
            services.AddMediatR(x => x.RegisterServicesFromAssemblyContaining(typeof(Pong)));

            var container = new StashboxContainer();
            services.UseStashbox(container);
            container.Validate();

            return services.BuildServiceProvider();
        }
    }
}