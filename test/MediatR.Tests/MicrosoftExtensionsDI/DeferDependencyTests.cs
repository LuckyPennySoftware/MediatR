using MediatR.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.Extensions.Microsoft.DependencyInjection.Tests;

using System;
using System.Linq;
using Shouldly;
using Xunit;

public class DeferDependencyTests
{
    private readonly IServiceCollection _services;

    public DeferDependencyTests()
    {
        _services = new ServiceCollection();
        _services.AddSingleton(new Logger());
        _services.AddSingleton<PingedHandler>();

    }

    [Fact]
    public void ShouldHaveDuplicateRegistrationOfPingedHandler()
    {
        _services.Count(x => x.ImplementationType == typeof(PingedHandler)).ShouldBe(1);

        AddingMediatR(withDeferral: false);

        _services.Count(x => x.ImplementationType == typeof(PingedHandler) && x.Lifetime == ServiceLifetime.Transient).ShouldBe(1);
        _services.Count(x => x.ImplementationType == typeof(PingedHandler) && x.Lifetime == ServiceLifetime.Singleton).ShouldBe(1);

        using var provider = BuildingServiceProvider();

        var a = provider.GetServices<INotificationHandler<Pinged>>().Single(x => x is PingedHandler);
        var b = provider.GetServices<INotificationHandler<Pinged>>().Single(x => x is PingedHandler);
        a.ShouldNotBeSameAs(b);
    }

    [Fact]
    public void ShouldHaveSingletonRegistrationOfPingedHandler()
    {
        _services.Count(x => x.ImplementationType == typeof(PingedHandler)).ShouldBe(1);

        AddingMediatR(withDeferral: true);

        _services.Count(x => x.ImplementationType == typeof(PingedHandler)).ShouldBe(1);

        using var provider = BuildingServiceProvider();

        var a = provider.GetServices<INotificationHandler<Pinged>>().Single(x => x is PingedHandler);
        var b = provider.GetServices<INotificationHandler<Pinged>>().Single(x => x is PingedHandler);
        a.ShouldBeSameAs(b);
    }

    private void AddingMediatR(bool withDeferral) =>
        _services.AddMediatR(cfg =>
            {
                cfg.DeferToExistingRegistrations = withDeferral;
                cfg.RegisterServicesFromAssemblies(typeof(Ping).Assembly);
            }
        );

    private ServiceProvider BuildingServiceProvider() => _services.BuildServiceProvider();
}