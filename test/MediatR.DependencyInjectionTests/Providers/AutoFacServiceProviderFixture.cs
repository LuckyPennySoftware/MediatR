using Autofac;
using Autofac.Extensions.DependencyInjection;
using MediatR.DependencyInjectionTests.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.DependencyInjectionTests.Providers;

public class AutoFacServiceProviderFixture : BaseServiceProviderFixture
{
    public override IServiceProvider Provider
    {
        get
        {
            var services = new ServiceCollection();
            services.AddMediatR(x => x.RegisterServicesFromAssemblyContaining(typeof(Pong)));

            var builder = new ContainerBuilder();
            builder.Populate(services);

            var container = builder.Build();
            return new AutofacServiceProvider(container);
        }
    }
}