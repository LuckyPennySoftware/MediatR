using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using MediatR.Licensing;
using MediatR.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MediatR.Tests;

[CollectionDefinition(nameof(ServiceFactoryCollectionBehavior), DisableParallelization = true)]
public class ServiceFactoryCollectionBehavior {}

[Collection(nameof(ServiceFactoryCollectionBehavior))]
public class ServiceFactoryTests
{
    public class Ping : IRequest<Pong>
    {

    }

    public class Pong
    {
        public string? Message { get; set; }
    }

    [Fact]
    public async Task Should_throw_given_no_handler()
    {
        MediatRServiceCollectionExtensions.LicenseChecked = false;
      
        var serviceCollection = new ServiceCollection();
        ServiceRegistrar.AddRequiredServices(serviceCollection, new MediatRServiceConfiguration());
        serviceCollection.AddFakeLogging();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        
        var mediator = new Mediator(serviceProvider);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.Send(new Ping())
        );
    }

    [Fact]
    public void Should_not_throw_with_manual_registration()
    {
        MediatRServiceCollectionExtensions.LicenseChecked = false;
      
        var services = new ServiceCollection();
        services.AddFakeLogging();
        services.AddTransient<IMediator, Mediator>();
        services.AddSingleton(new MediatRServiceConfiguration());

        var container = services.BuildServiceProvider();

        Should.NotThrow(() => container.GetRequiredService<IMediator>());
    }
    
    [Fact]
    public void Should_throw_when_missing_required_configuration()
    {
        MediatRServiceCollectionExtensions.LicenseChecked = false;
        
        var services = new ServiceCollection();
        services.AddFakeLogging();
        services.AddTransient<IMediator, Mediator>();

        var container = services.BuildServiceProvider();

        Should.Throw<InvalidOperationException>(() => container.GetRequiredService<IMediator>());
    }
}