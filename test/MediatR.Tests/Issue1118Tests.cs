using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace MediatR.Tests;

/// <summary>
/// Tests for GitHub Issue #1118: Notification handler called twice for same event
/// https://github.com/jbogard/MediatR/issues/1118
/// </summary>
public class Issue1118Tests
{
    public class E1 : INotification { }

    public class E2 : E1 { }

    public class C1 : INotificationHandler<E1>
    {
        public static readonly List<string> HandledEvents = new();

        public Task Handle(E1 notification, CancellationToken cancellationToken)
        {
            HandledEvents.Add($"C1 handling {notification.GetType().Name}");
            return Task.CompletedTask;
        }
    }

    public class C2 : INotificationHandler<E2>
    {
        public static readonly List<string> HandledEvents = new();

        public Task Handle(E2 notification, CancellationToken cancellationToken)
        {
            HandledEvents.Add($"C2 handling {notification.GetType().Name}");
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Should_Call_C1_Once_For_E1()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(Issue1118Tests).Assembly);
        });
        var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IPublisher>();

        C1.HandledEvents.Clear();
        C2.HandledEvents.Clear();

        // Act
        await publisher.Publish(new E1());

        // Assert
        C1.HandledEvents.Count.ShouldBe(1);
        C1.HandledEvents[0].ShouldBe("C1 handling E1");
        C2.HandledEvents.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Call_C1_Once_And_C2_Once_For_E2()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(Issue1118Tests).Assembly);
        });
        var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IPublisher>();

        C1.HandledEvents.Clear();
        C2.HandledEvents.Clear();

        // Act
        await publisher.Publish(new E2());

        // Assert
        // C1 should handle E2 once (through contravariance - this is expected)
        C1.HandledEvents.Count.ShouldBe(1, "C1 should handle E2 once (not twice!)");
        C1.HandledEvents[0].ShouldBe("C1 handling E2");

        // C2 should handle E2 once
        C2.HandledEvents.Count.ShouldBe(1);
        C2.HandledEvents[0].ShouldBe("C2 handling E2");
    }

    [Fact]
    public void Should_Not_Register_C1_For_INotificationHandler_Of_E2()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(Issue1118Tests).Assembly);
        });

        // Act - Get all registrations for INotificationHandler<E2>
        var handlersForE2 = services
            .Where(sd => sd.ServiceType == typeof(INotificationHandler<E2>))
            .ToList();

        var c1RegisteredForE2 = handlersForE2
            .Where(sd => sd.ImplementationType == typeof(C1))
            .ToList();

        var c2RegisteredForE2 = handlersForE2
            .Where(sd => sd.ImplementationType == typeof(C2))
            .ToList();

        // Assert
        // Only C2 should be registered for INotificationHandler<E2>
        c2RegisteredForE2.Count.ShouldBe(1, "C2 should be registered for INotificationHandler<E2>");
        
        // C1 should NOT be registered for INotificationHandler<E2>
        // Even though INotificationHandler<in T> is contravariant and C1 could handle E2,
        // we should only register exact implementations to avoid duplicate calls
        c1RegisteredForE2.Count.ShouldBe(0, 
            "C1 should NOT be registered for INotificationHandler<E2> - this was the bug in issue #1118");
    }
}
