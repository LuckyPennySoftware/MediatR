using Microsoft.Extensions.DependencyInjection;

namespace MediatR.Extensions.Microsoft.DependencyInjection.Tests;

using System;
using System.Linq;
using Shouldly;
using Xunit;

public class CustomMediatorTests
{
    private readonly IServiceProvider _provider;

    public CustomMediatorTests()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddFakeLogging();
        services.AddSingleton(new Logger());
        services.AddMediatR(cfg =>
        {
            cfg.MediatorImplementationType = typeof(MyCustomMediator);
            cfg.RegisterServicesFromAssemblyContaining(typeof(CustomMediatorTests));
        });
        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public void ShouldResolveMediator()
    {
        _provider.GetService<IMediator>().ShouldNotBeNull();
        _provider.GetRequiredService<IMediator>().GetType().ShouldBe(typeof(MyCustomMediator));
    }

    [Fact]
    public void ShouldResolveRequestHandler()
    {
        _provider.GetService<IRequestHandler<Ping, Pong>>().ShouldNotBeNull();
    }

    [Fact]
    public void ShouldResolveNotificationHandlers()
    {
        _provider.GetServices<INotificationHandler<Pinged>>().Count().ShouldBe(4);
    }

    [Fact]
    public void Can_Call_AddMediatr_multiple_times()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddFakeLogging();
        services.AddMediatR(cfg =>
        {
            cfg.MediatorImplementationType = typeof(MyCustomMediator);
            cfg.RegisterServicesFromAssemblyContaining(typeof(CustomMediatorTests));
        });
            
        // Call AddMediatr again, this should NOT override our custom mediatr (With MS DI, last registration wins)
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining(typeof(CustomMediatorTests)));

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        mediator.GetType().ShouldBe(typeof(MyCustomMediator));
    }
}