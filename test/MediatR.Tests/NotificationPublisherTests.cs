using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR.NotificationPublishers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace MediatR.Tests;

public class NotificationPublisherTests
{
    private readonly ITestOutputHelper _output;

    public NotificationPublisherTests(ITestOutputHelper output) => _output = output;

    public class Notification : INotification
    {
    }

    public class FirstHandler : INotificationHandler<Notification>
    {
        public async Task Handle(Notification notification, CancellationToken cancellationToken) 
            => await Task.Delay(500, cancellationToken);
    }
    public class SecondHandler : INotificationHandler<Notification>
    {
        public async Task Handle(Notification notification, CancellationToken cancellationToken) 
            => await Task.Delay(250, cancellationToken);
    }

    [Fact]
    public async Task Should_handle_sequentially_by_default()
    {
        var services = new ServiceCollection();
        services.AddFakeLogging();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<Notification>();
        });
        var serviceProvider = services.BuildServiceProvider();

        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var timer = new Stopwatch();
        timer.Start();

        await mediator.Publish(new Notification());

        timer.Stop();
        
        var sequentialElapsed = timer.ElapsedMilliseconds;

        services = new ServiceCollection();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<Notification>();
            cfg.NotificationPublisherType = typeof(TaskWhenAllPublisher);
        });
        serviceProvider = services.BuildServiceProvider();

        mediator = serviceProvider.GetRequiredService<IMediator>();

        timer.Restart();

        await mediator.Publish(new Notification());

        timer.Stop();
        
        var parallelElapsed = timer.ElapsedMilliseconds;

        sequentialElapsed.ShouldBeGreaterThan(parallelElapsed);
    }
}