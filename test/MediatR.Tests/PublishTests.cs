using System.Threading;

namespace MediatR.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using Lamar;
using Xunit;

public class PublishTests
{
    public class Ping : INotification
    {
        public string? Message { get; set; }
    }

    public class PongHandler : INotificationHandler<Ping>
    {
        private readonly TextWriter _writer;

        public PongHandler(TextWriter writer)
        {
            _writer = writer;
        }

        public Task Handle(Ping notification, CancellationToken cancellationToken)
        {
            return _writer.WriteLineAsync(notification.Message + " Pong");
        }
    }

    public class PungHandler : INotificationHandler<Ping>
    {
        private readonly TextWriter _writer;

        public PungHandler(TextWriter writer)
        {
            _writer = writer;
        }

        public Task Handle(Ping notification, CancellationToken cancellationToken)
        {
            return _writer.WriteLineAsync(notification.Message + " Pung");
        }
    }

    [Fact]
    public async Task Should_resolve_main_handler()
    {
        var builder = new StringBuilder();
        var writer = new StringWriter(builder);

        var container = TestContainer.Create(cfg =>
        {
            cfg.Scan(scanner =>
            {
                scanner.AssemblyContainingType(typeof(PublishTests));
                scanner.IncludeNamespaceContainingType<Ping>();
                scanner.WithDefaultConventions();
                scanner.AddAllTypesOf(typeof (INotificationHandler<>));
            });
            cfg.For<TextWriter>().Use(writer);
            cfg.For<IMediator>().Use<Mediator>();
        });

        var mediator = container.GetInstance<IMediator>();

        await mediator.Publish(new Ping { Message = "Ping" });

        var result = builder.ToString().Split(new [] {Environment.NewLine}, StringSplitOptions.None);
        result.ShouldContain("Ping Pong");
        result.ShouldContain("Ping Pung");
    }

    [Fact]
    public async Task Should_resolve_main_handler_when_object_is_passed()
    {
        var builder = new StringBuilder();
        var writer = new StringWriter(builder);

        var container = TestContainer.Create(cfg =>
        {
            cfg.Scan(scanner =>
            {
                scanner.AssemblyContainingType(typeof(PublishTests));
                scanner.IncludeNamespaceContainingType<Ping>();
                scanner.WithDefaultConventions();
                scanner.AddAllTypesOf(typeof (INotificationHandler<>));
            });
            cfg.For<TextWriter>().Use(writer);
            cfg.For<IMediator>().Use<Mediator>();
        });

        var mediator = container.GetInstance<IMediator>();

        object message = new Ping { Message = "Ping" };
        await mediator.Publish(message);

        var result = builder.ToString().Split(new [] {Environment.NewLine}, StringSplitOptions.None);
        result.ShouldContain("Ping Pong");
        result.ShouldContain("Ping Pung");
    }

    public class SequentialMediator : Mediator
    {
        public SequentialMediator(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        protected override async Task PublishCore(IEnumerable<NotificationHandlerExecutor> allHandlers, INotification notification, CancellationToken cancellationToken)
        {
            foreach (var handler in allHandlers)
            {
                await handler.HandlerCallback(notification, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public class SequentialPublisher : INotificationPublisher
    {
        public int CallCount { get; set; }

        public async Task Publish(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification, CancellationToken cancellationToken)
        {
            foreach (var handler in handlerExecutors)
            {
                await handler.HandlerCallback(notification, cancellationToken).ConfigureAwait(false);
                CallCount++;
            }
        }
    }

    [Fact]
    public async Task Should_override_with_sequential_firing()
    {
        var builder = new StringBuilder();
        var writer = new StringWriter(builder);

        var container = TestContainer.Create(cfg =>
        {
            cfg.Scan(scanner =>
            {
                scanner.AssemblyContainingType(typeof(PublishTests));
                scanner.IncludeNamespaceContainingType<Ping>();
                scanner.WithDefaultConventions();
                scanner.AddAllTypesOf(typeof(INotificationHandler<>));
            });
            cfg.For<TextWriter>().Use(writer);
            cfg.For<IMediator>().Use<SequentialMediator>();
        });

        var mediator = container.GetInstance<IMediator>();

        await mediator.Publish(new Ping { Message = "Ping" });

        var result = builder.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        result.ShouldContain("Ping Pong");
        result.ShouldContain("Ping Pung");
    }

    [Fact]
    public async Task Should_override_with_sequential_firing_through_injection()
    {
        var builder = new StringBuilder();
        var writer = new StringWriter(builder);
        var publisher = new SequentialPublisher();

        var container = TestContainer.Create(cfg =>
        {
            cfg.Scan(scanner =>
            {
                scanner.AssemblyContainingType(typeof(PublishTests));
                scanner.IncludeNamespaceContainingType<Ping>();
                scanner.WithDefaultConventions();
                scanner.AddAllTypesOf(typeof(INotificationHandler<>));
            });
            cfg.For<TextWriter>().Use(writer);
            cfg.For<INotificationPublisher>().Use(publisher);
            cfg.For<IMediator>().Use<Mediator>();
        });

        var mediator = container.GetInstance<IMediator>();

        await mediator.Publish(new Ping { Message = "Ping" });

        var result = builder.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        result.ShouldContain("Ping Pong");
        result.ShouldContain("Ping Pung");
        publisher.CallCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_resolve_handlers_given_interface()
    {
        var builder = new StringBuilder();
        var writer = new StringWriter(builder);

        var container = TestContainer.Create(cfg =>
        {
            cfg.Scan(scanner =>
            {
                scanner.AssemblyContainingType(typeof(PublishTests));
                scanner.IncludeNamespaceContainingType<Ping>();
                scanner.WithDefaultConventions();
                scanner.AddAllTypesOf(typeof(INotificationHandler<>));
            });
            cfg.For<TextWriter>().Use(writer);
            cfg.For<IMediator>().Use<SequentialMediator>();
        });

        var mediator = container.GetInstance<IMediator>();

        // wrap notifications in an array, so this test won't break on a 'replace with var' refactoring
        var notifications = new INotification[] { new Ping { Message = "Ping" } };
        await mediator.Publish(notifications[0]);

        var result = builder.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        result.ShouldContain("Ping Pong");
        result.ShouldContain("Ping Pung");
    }

    [Fact]
    public async Task Should_resolve_main_handler_by_specific_interface()
    {
        var builder = new StringBuilder();
        var writer = new StringWriter(builder);

        var container = TestContainer.Create(cfg =>
        {
            cfg.Scan(scanner =>
            {
                scanner.AssemblyContainingType(typeof(PublishTests));
                scanner.IncludeNamespaceContainingType<Ping>();
                scanner.WithDefaultConventions();
                scanner.AddAllTypesOf(typeof (INotificationHandler<>));
            });
            cfg.For<TextWriter>().Use(writer);
            cfg.For<IPublisher>().Use<Mediator>();
        });

        var mediator = container.GetInstance<IPublisher>();

        await mediator.Publish(new Ping { Message = "Ping" });

        var result = builder.ToString().Split(new [] {Environment.NewLine}, StringSplitOptions.None);
        result.ShouldContain("Ping Pong");
        result.ShouldContain("Ping Pung");
    }
}