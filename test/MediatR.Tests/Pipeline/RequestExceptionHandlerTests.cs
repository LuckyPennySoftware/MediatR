namespace MediatR.Tests.Pipeline;

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR.Pipeline;
using Shouldly;
using Xunit;

public class RequestExceptionHandlerTests
{
    public class Ping : IRequest<Pong>
    {
        public string? Message { get; set; }
    }

    public class Pong
    {
        public string? Message { get; set; }
    }

    public class PingException : Exception
    {
        public PingException(string? message) : base(message + " Thrown")
        {
        }
    }

    public class PingHandler : IRequestHandler<Ping, Pong>
    {
        public Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
        {
            throw new PingException(request.Message);
        }
    }

    public class GenericPingExceptionHandler : IRequestExceptionHandler<Ping, Pong, Exception>
    {
        public int ExecutionCount { get; private set; }

        public Task Handle(Ping request, Exception exception, RequestExceptionHandlerState<Pong> state, CancellationToken cancellationToken)
        {
            ExecutionCount++;
            return Task.CompletedTask;
        }
    }

    public class PingPongExceptionHandlerForType : IRequestExceptionHandler<Ping, Pong, PingException>
    {
        public Task Handle(Ping request, PingException exception, RequestExceptionHandlerState<Pong> state, CancellationToken cancellationToken)
        {
            state.SetHandled(new Pong() { Message = exception.Message + " Handled by Type" });

            return Task.CompletedTask;
        }
    }

    public class PingPongExceptionHandler : IRequestExceptionHandler<Ping, Pong, Exception>
    {
        public Task Handle(Ping request, Exception exception, RequestExceptionHandlerState<Pong> state, CancellationToken token)
        {
            state.SetHandled(new Pong() { Message = exception.Message + " Handled"});
            
            return Task.CompletedTask;
        }
    }

    public class PingPongExceptionHandlerNotHandled : IRequestExceptionHandler<Ping, Pong, Exception>
    {
        public Task Handle(Ping request, Exception exception, RequestExceptionHandlerState<Pong> state, CancellationToken token)
        {
            request.Message = exception.Message + " Not Handled";
            
            return Task.CompletedTask;
        }
    }

    public class PingPongThrowingExceptionHandler : IRequestExceptionHandler<Ping, Pong, Exception>
    {
        public Task Handle(Ping request, Exception exception, RequestExceptionHandlerState<Pong> state, CancellationToken token)
        {
            throw new ApplicationException("Surprise!");
        }
    }

    [Fact]
    public async Task Should_run_exception_handler_and_allow_for_exception_not_to_throw()
    {
        var container = TestContainer.Create(cfg =>
        {
            cfg.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();
            cfg.AddTransient<IRequestExceptionHandler<Ping, Pong, Exception>, PingPongExceptionHandler>();
            cfg.AddTransient<IRequestExceptionHandler<Ping, Pong, PingException>, PingPongExceptionHandlerForType>();
            cfg.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestExceptionProcessorBehavior<,>));
            cfg.AddTransient<IMediator, Mediator>();
        });

        var mediator = container.GetRequiredService<IMediator>();

        var response = await mediator.Send(new Ping { Message = "Ping" });

        response.Message.ShouldBe("Ping Thrown Handled by Type");
    }

    [Fact]
    public async Task Should_run_exception_handler_and_allow_for_exception_to_be_still_thrown()
    {
        var container = TestContainer.Create(cfg =>
        {
            cfg.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();
            cfg.AddTransient<IRequestExceptionHandler<Ping, Pong, Exception>, PingPongExceptionHandlerNotHandled>();
            cfg.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestExceptionProcessorBehavior<,>));
            cfg.AddTransient<IMediator, Mediator>();
        });

        var mediator = container.GetRequiredService<IMediator>();

        var request = new Ping { Message = "Ping" };
        await Should.ThrowAsync<PingException>(async () =>
        {
            await mediator.Send(request);
        });

        request.Message.ShouldBe("Ping Thrown Not Handled");
    }

    [Fact]
    public async Task Should_run_exception_handler_and_unwrap_expections_thrown_in_the_handler()
    {
        var container = TestContainer.Create(cfg =>
        {
            cfg.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();
            cfg.AddTransient<IRequestExceptionHandler<Ping, Pong, Exception>, PingPongThrowingExceptionHandler>();
            cfg.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestExceptionProcessorBehavior<,>));
            cfg.AddTransient<IMediator, Mediator>();
        });

        var mediator = container.GetRequiredService<IMediator>();

        var request = new Ping { Message = "Ping" };
        await Should.ThrowAsync<ApplicationException>(async () =>
        {
            await mediator.Send(request);
        });
    }

    [Fact]
    public async Task Should_run_matching_exception_handlers_only_once()
    {
        var genericPingExceptionHandler = new GenericPingExceptionHandler();
        var container = TestContainer.Create(cfg =>
        {
            cfg.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();
            cfg.AddSingleton<IRequestExceptionHandler<Ping, Pong, Exception>>(genericPingExceptionHandler);
            cfg.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestExceptionProcessorBehavior<,>));
            cfg.AddTransient<IMediator, Mediator>();
        });

        var mediator = container.GetRequiredService<IMediator>();

        var request = new Ping { Message = "Ping" };
        await Should.ThrowAsync<PingException>(async () =>
        {
            await mediator.Send(request);
        });

        genericPingExceptionHandler.ExecutionCount.ShouldBe(1);
    }

}