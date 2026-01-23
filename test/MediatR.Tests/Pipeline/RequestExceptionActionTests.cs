using MediatR.Pipeline;

namespace MediatR.Tests.Pipeline;

public class RequestExceptionActionTests
{
    public class Ping : IRequest<Pong>
    {
        public string? Message { get; set; }
    }

    public class Pong
    {
        public string? Message { get; set; }
    }

    public abstract class PingPongException : Exception
    {
        protected PingPongException(string? message) : base(message + " Thrown")
        {
        }
    }

    public class PingException : PingPongException
    {
        public PingException(string? message) : base(message)
        {
        }
    }

    public class PongException : PingPongException
    {
        public PongException(string message) : base(message)
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

    public class GenericExceptionAction<TRequest> : IRequestExceptionAction<TRequest, Exception> where TRequest : notnull
    {
        public int ExecutionCount { get; private set; }

        public Task Execute(TRequest request, Exception exception, CancellationToken cancellationToken)
        {
            ExecutionCount++;
            return Task.CompletedTask;
        }
    }

    public class PingPongExceptionAction<TRequest> : IRequestExceptionAction<TRequest, PingPongException> where TRequest : notnull
    {
        public bool Executed { get; private set; }

        public Task Execute(TRequest request, PingPongException exception, CancellationToken cancellationToken)
        {
            Executed = true;
            return Task.CompletedTask;
        }
    }

    public class PingExceptionAction : IRequestExceptionAction<Ping, PingException>
    {
        public bool Executed { get; private set; }

        public Task Execute(Ping request, PingException exception, CancellationToken cancellationToken)
        {
            Executed = true;
            return Task.CompletedTask;
        }
    }

    public class PongExceptionAction : IRequestExceptionAction<Ping, PongException>
    {
        public bool Executed { get; private set; }

        public Task Execute(Ping request, PongException exception, CancellationToken cancellationToken)
        {
            Executed = true;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Should_run_all_exception_actions_that_match_base_type()
    {
        var pingExceptionAction = new PingExceptionAction();
        var pongExceptionAction = new PongExceptionAction();
        var pingPongExceptionAction = new PingPongExceptionAction<Ping>();
        
        var container = TestContainer.Create(cfg =>
        {
            cfg.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();
            cfg.AddTransient<IRequestExceptionAction<Ping, PingException>>(_ => pingExceptionAction);
            cfg.AddTransient<IRequestExceptionAction<Ping, PingPongException>>(_ => pingPongExceptionAction);
            cfg.AddTransient<IRequestExceptionAction<Ping, PongException>>(_ => pongExceptionAction);
            cfg.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestExceptionActionProcessorBehavior<,>));
            cfg.AddTransient<IMediator, Mediator>();
        });

        var mediator = container.GetRequiredService<IMediator>();

        var request = new Ping { Message = "Ping!" };
        await Assert.ThrowsAsync<PingException>(() => mediator.Send(request));

        pingExceptionAction.Executed.ShouldBeTrue();
        pingPongExceptionAction.Executed.ShouldBeTrue();
        pongExceptionAction.Executed.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_run_matching_exception_actions_only_once()
    {
        var genericExceptionAction = new GenericExceptionAction<Ping>();
        var container = TestContainer.Create(cfg =>
        {
            cfg.AddTransient<IRequestHandler<Ping, Pong>, PingHandler>();
            cfg.AddTransient<IRequestExceptionAction<Ping, Exception>>(_ => genericExceptionAction);
            cfg.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestExceptionActionProcessorBehavior<,>));
            cfg.AddTransient<IMediator, Mediator>();
        });

        var mediator = container.GetRequiredService<IMediator>();

        var request = new Ping { Message = "Ping!" };
        await Assert.ThrowsAsync<PingException>(() => mediator.Send(request));

        genericExceptionAction.ExecutionCount.ShouldBe(1);
    }
}