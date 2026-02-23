using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace MediatR.Extensions.Microsoft.DependencyInjection.Tests;

/// <summary>
/// Regression tests for https://github.com/LuckyPennySoftware/MediatR/issues/1118
///
/// When E2 derives from E1, a handler for E1 (C1) would be called twice when publishing E2
/// because some DI containers that support variance (e.g. DryIoc) return C1 both from
/// the explicit INotificationHandler&lt;E2&gt; registration AND from their own contravariant
/// resolution of INotificationHandler&lt;E1&gt;.
/// </summary>
public class Issue1118Tests
{
    public class CallLog
    {
        public List<string> Entries { get; } = new();
    }

    public class BaseEvent : INotification { }

    public class DerivedEvent : BaseEvent { }

    // IServiceProvider is always injectable, so CallLog becomes an optional soft dependency.
    // This prevents PipelineTests (which uses ValidateOnBuild=true on the same assembly) from
    // failing due to CallLog not being registered in its container.
    public class BaseEventHandler : INotificationHandler<BaseEvent>
    {
        private readonly IServiceProvider _serviceProvider;

        public BaseEventHandler(IServiceProvider serviceProvider)
            => _serviceProvider = serviceProvider;

        public Task Handle(BaseEvent notification, CancellationToken cancellationToken)
        {
            _serviceProvider.GetService<CallLog>()?.Entries.Add($"BaseEventHandler:{notification.GetType().Name}");
            return Task.CompletedTask;
        }
    }

    public class DerivedEventHandler : INotificationHandler<DerivedEvent>
    {
        private readonly IServiceProvider _serviceProvider;

        public DerivedEventHandler(IServiceProvider serviceProvider)
            => _serviceProvider = serviceProvider;

        public Task Handle(DerivedEvent notification, CancellationToken cancellationToken)
        {
            _serviceProvider.GetService<CallLog>()?.Entries.Add($"DerivedEventHandler:{notification.GetType().Name}");
            return Task.CompletedTask;
        }
    }

    private static IServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddFakeLogging();
        services.AddSingleton<CallLog>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Issue1118Tests).Assembly));
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Publishing_BaseEvent_Should_Call_BaseEventHandler_Once()
    {
        var provider = BuildProvider();
        var log = provider.GetRequiredService<CallLog>();

        await provider.GetRequiredService<IPublisher>().Publish(new BaseEvent());

        log.Entries.Count(e => e.StartsWith("BaseEventHandler")).ShouldBe(1);
    }

    [Fact]
    public async Task Publishing_DerivedEvent_Should_Call_BaseEventHandler_ExactlyOnce()
    {
        var provider = BuildProvider();
        var log = provider.GetRequiredService<CallLog>();

        await provider.GetRequiredService<IPublisher>().Publish(new DerivedEvent());

        log.Entries.Count(e => e.StartsWith("BaseEventHandler")).ShouldBe(1,
            "BaseEventHandler should be called exactly once for DerivedEvent, not duplicated");
    }

    [Fact]
    public async Task Publishing_DerivedEvent_Should_Call_DerivedEventHandler_Once()
    {
        var provider = BuildProvider();
        var log = provider.GetRequiredService<CallLog>();

        await provider.GetRequiredService<IPublisher>().Publish(new DerivedEvent());

        log.Entries.Count(e => e.StartsWith("DerivedEventHandler")).ShouldBe(1);
    }
}
