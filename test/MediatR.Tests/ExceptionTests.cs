using System.Threading;

namespace MediatR.Tests;

using System;
using System.Threading.Tasks;
using Shouldly;
using Lamar;
using Xunit;
using Lamar.IoC;

public class ExceptionTests
{
    private readonly IMediator _mediator;

    public class Ping : IRequest<Pong>
    {
    }

    public class Pong
    {
    }

    public class VoidPing : IRequest
    {
    }

    public class Pinged : INotification
    {
    }

    public class AsyncPing : IRequest<Pong>
    {
    }

    public class AsyncVoidPing : IRequest
    {
    }

    public class AsyncPinged : INotification
    {
    }

    public class NullPing : IRequest<Pong>
    {
    }

    public class VoidNullPing : IRequest
    {
    }

    public class NullPinged : INotification
    {
    }

    public class NullPingHandler : IRequestHandler<NullPing, Pong>
    {
        public Task<Pong> Handle(NullPing request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Pong());
        }
    }

    public class VoidNullPingHandler : IRequestHandler<VoidNullPing>
    {
        public Task Handle(VoidNullPing request, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    public ExceptionTests()
    {
        var container = TestContainer.Create(cfg =>
        {
            cfg.For<IMediator>().Use<Mediator>();
        });
        _mediator = container.GetInstance<IMediator>();
    }

    [Fact]
    public async Task Should_throw_for_send()
    {
        await Should.ThrowAsync<LamarMissingRegistrationException>(async () => await _mediator.Send(new Ping()));
    }

    [Fact]
    public async Task Should_throw_for_void_send()
    {
        await Should.ThrowAsync<LamarMissingRegistrationException>(async () => await _mediator.Send(new VoidPing()));
    }

    [Fact]
    public async Task Should_not_throw_for_publish()
    {
        Exception ex = null!;
        try
        {
            await _mediator.Publish(new Pinged());
        }
        catch (Exception e)
        {
            ex = e;
        }
        ex.ShouldBeNull();
    }

    [Fact]
    public async Task Should_throw_for_async_send()
    {
        await Should.ThrowAsync<LamarMissingRegistrationException>(async () => await _mediator.Send(new AsyncPing()));
    }

    [Fact]
    public async Task Should_throw_for_async_void_send()
    {
        await Should.ThrowAsync<LamarMissingRegistrationException>(async () => await _mediator.Send(new AsyncVoidPing()));
    }

    [Fact]
    public async Task Should_not_throw_for_async_publish()
    {
        Exception ex = null!;
        try
        {
            await _mediator.Publish(new AsyncPinged());
        }
        catch (Exception e)
        {
            ex = e;
        }
        ex.ShouldBeNull();
    }

    [Fact]
    public async Task Should_throw_argument_exception_for_send_when_request_is_null()
    {
        var container = TestContainer.Create(cfg =>
        {
            cfg.Scan(scanner =>
            {
                scanner.AssemblyContainingType(typeof(NullPing));
                scanner.IncludeNamespaceContainingType<Ping>();
                scanner.WithDefaultConventions();
                scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
            });
            cfg.For<IMediator>().Use<Mediator>();
        });
        var mediator = container.GetInstance<IMediator>();

        NullPing request = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await mediator.Send(request));
    }

    [Fact]
    public async Task Should_throw_argument_exception_for_void_send_when_request_is_null()
    {
        var container = TestContainer.Create(cfg =>
        {
            cfg.Scan(scanner =>
            {
                scanner.AssemblyContainingType(typeof(VoidNullPing));
                scanner.IncludeNamespaceContainingType<Ping>();
                scanner.WithDefaultConventions();
                scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
            });
            cfg.For<IMediator>().Use<Mediator>();
        });
        var mediator = container.GetInstance<IMediator>();

        VoidNullPing request = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await mediator.Send(request));
    }

    [Fact]
    public async Task Should_throw_argument_exception_for_publish_when_request_is_null()
    {
        var container = TestContainer.Create(cfg =>
        {
            cfg.Scan(scanner =>
            {
                scanner.AssemblyContainingType(typeof(NullPinged));
                scanner.IncludeNamespaceContainingType<Ping>();
                scanner.WithDefaultConventions();
                scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
            });
            cfg.For<IMediator>().Use<Mediator>();
        });
        var mediator = container.GetInstance<IMediator>();

        NullPinged notification = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await mediator.Publish(notification));
    }

    [Fact]
    public async Task Should_throw_argument_exception_for_publish_when_request_is_null_object()
    {
        var container = TestContainer.Create(cfg =>
        {
            cfg.Scan(scanner =>
            {
                scanner.AssemblyContainingType(typeof(NullPinged));
                scanner.IncludeNamespaceContainingType<Ping>();
                scanner.WithDefaultConventions();
                scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
            });
            cfg.For<IMediator>().Use<Mediator>();
        });
        var mediator = container.GetInstance<IMediator>();

        object notification = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await mediator.Publish(notification));
    }

    [Fact]
    public async Task Should_throw_argument_exception_for_publish_when_request_is_not_notification()
    {
        var container = TestContainer.Create(cfg =>
        {
            cfg.Scan(scanner =>
            {
                scanner.AssemblyContainingType(typeof(NullPinged));
                scanner.IncludeNamespaceContainingType<Ping>();
                scanner.WithDefaultConventions();
                scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
            });
            cfg.For<IMediator>().Use<Mediator>();
        });
        var mediator = container.GetInstance<IMediator>();

        object notification = "totally not notification";

        await Should.ThrowAsync<ArgumentException>(async () => await mediator.Publish(notification));
    }

    public class PingException : IRequest
    {

    }

    public class PingExceptionHandler : IRequestHandler<PingException>
    {
        public Task Handle(PingException request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public async Task Should_throw_exception_for_non_generic_send_when_exception_occurs()
    {
        var container = TestContainer.Create(cfg =>
        {
            cfg.Scan(scanner =>
            {
                scanner.AssemblyContainingType(typeof(NullPinged));
                scanner.IncludeNamespaceContainingType<Ping>();
                scanner.WithDefaultConventions();
                scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
                scanner.AddAllTypesOf(typeof(IRequestHandler<>));
            });
            cfg.For<IMediator>().Use<Mediator>();
        });
        var mediator = container.GetInstance<IMediator>();

        object pingException = new PingException();

        await Should.ThrowAsync<NotImplementedException>(async () => await mediator.Send(pingException));
    }

    [Fact]
    public async Task Should_throw_exception_for_non_request_send()
    {
        var container = TestContainer.Create(cfg =>
        {
            cfg.Scan(scanner =>
            {
                scanner.AssemblyContainingType(typeof(NullPinged));
                scanner.IncludeNamespaceContainingType<Ping>();
                scanner.WithDefaultConventions();
                scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
            });
            cfg.For<IMediator>().Use<Mediator>();
        });
        var mediator = container.GetInstance<IMediator>();

        object nonRequest = new NonRequest();

        var argumentException = await Should.ThrowAsync<ArgumentException>(async () => await mediator.Send(nonRequest));
        Assert.StartsWith("NonRequest does not implement IRequest", argumentException.Message);
    }

    public class NonRequest
    {

    }

    [Fact]
    public async Task Should_throw_exception_for_generic_send_when_exception_occurs()
    {
        var container = TestContainer.Create(cfg =>
        {
            cfg.Scan(scanner =>
            {
                scanner.AssemblyContainingType(typeof(NullPinged));
                scanner.IncludeNamespaceContainingType<Ping>();
                scanner.WithDefaultConventions();
                scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
                scanner.AddAllTypesOf(typeof(IRequestHandler<>));
            });
            cfg.For<IMediator>().Use<Mediator>();
        });
        var mediator = container.GetInstance<IMediator>();

        PingException pingException = new PingException();

        await Should.ThrowAsync<NotImplementedException>(async () => await mediator.Send(pingException));
    }
}