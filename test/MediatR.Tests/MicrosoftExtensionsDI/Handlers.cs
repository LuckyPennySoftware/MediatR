using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Deluxe.MediatR.Tests.MicrosoftExtensionsDI;

public class Ping : IRequest<Pong>
{
    public string? Message { get; init; }
    public Action<Ping>? ThrowAction { get; init; }
}

public class DerivedPing : Ping
{
}

public class Pong
{
    public string? Message { get; init; }
}

public class Zing : IRequest<Zong>
{
    public string? Message { get; init; }
}

public class Zong
{
    public string? Message { get; init; }
}

public class Ding : IRequest
{
    public string? Message { get; init; }
}

public class Pinged : INotification
{

}

class InternalPing : IRequest { }

public class StreamPing : IStreamRequest<Pong>
{
    public string? Message { get; init; }
}

public class GenericHandler : INotificationHandler<INotification>
{
    public Task Handle(INotification notification, CancellationToken cancellationToken)
    {
        return Task.FromResult(0);
    }
}

public class DingAsyncHandler : IRequestHandler<Ding>
{
    public Task Handle(Ding message, CancellationToken cancellationToken) => Task.CompletedTask;
}

public class PingedHandler : INotificationHandler<Pinged>
{
    public Task Handle(Pinged notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class PingedAlsoHandler : INotificationHandler<Pinged>
{
    public Task Handle(Pinged notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class PingedAlsoOpenHandler<TNotification> : INotificationHandler<TNotification>
    where TNotification : Pinged
{
    public Task Handle(TNotification notification, CancellationToken cancellationToken) => Task.CompletedTask;
}

public class Logger
{
    public IList<string> Messages { get; } = new List<string>();
}

public class PingHandler : IRequestHandler<Ping, Pong>
{
    private readonly Logger _logger;

    public PingHandler(Logger logger)
    {
        _logger = logger;
    }
    public Task<Pong> Handle(Ping message, CancellationToken cancellationToken)
    {
        _logger.Messages.Add("Handler");

        message.ThrowAction?.Invoke(message);

        return Task.FromResult(new Pong { Message = message.Message + " Pong" });
    }
}

public class DerivedPingHandler : IRequestHandler<DerivedPing, Pong>
{
    private readonly Logger _logger;

    public DerivedPingHandler(Logger logger)
    {
        _logger = logger;
    }
    public Task<Pong> Handle(DerivedPing message, CancellationToken cancellationToken)
    {
        _logger.Messages.Add("Handler");
        return Task.FromResult(new Pong { Message = $"Derived{message.Message} Pong" });
    }
}

public class ZingHandler : IRequestHandler<Zing, Zong>
{
    private readonly Logger _output;

    public ZingHandler(Logger output)
    {
        _output = output;
    }
    public Task<Zong> Handle(Zing message, CancellationToken cancellationToken)
    {
        _output.Messages.Add("Handler");
        return Task.FromResult(new Zong { Message = message.Message + " Zong" });
    }
}

public class PingStreamHandler : IStreamRequestHandler<StreamPing, Pong>
{
    private readonly Logger _output;

    public PingStreamHandler(Logger output)
    {
        _output = output;
    }
    public async IAsyncEnumerable<Pong> Handle(StreamPing request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _output.Messages.Add("Handler");
        yield return await Task.Run(() => new Pong { Message = request.Message + " Pang" }, cancellationToken);
    }
}


public class DuplicateTest : IRequest<string> { }
public class DuplicateHandler1 : IRequestHandler<DuplicateTest, string>
{
    public Task<string> Handle(DuplicateTest message, CancellationToken cancellationToken)
    {
        return Task.FromResult(nameof(DuplicateHandler1));
    }
}

public class DuplicateHandler2 : IRequestHandler<DuplicateTest, string>
{
    public Task<string> Handle(DuplicateTest message, CancellationToken cancellationToken)
    {
        return Task.FromResult(nameof(DuplicateHandler2));
    }
}

class InternalPingHandler : IRequestHandler<InternalPing>
{
    public Task Handle(InternalPing request, CancellationToken cancellationToken) => Task.CompletedTask;
}

class MyCustomMediator : IMediator
{
    public Task<object?> Send(object request, CancellationToken cancellationToken = new())
    {
        throw new System.NotImplementedException();
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = new())
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = new())
    {
        throw new NotImplementedException();
    }

    public Task Publish(object notification, CancellationToken cancellationToken = new())
    {
        throw new System.NotImplementedException();
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        throw new System.NotImplementedException();
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }
    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        throw new System.NotImplementedException();
    }
}

interface ITypeArgument { }
class ConcreteTypeArgument : ITypeArgument { }
class OpenGenericVoidRequest<T> : IRequest
    where T : class, ITypeArgument
{ }
class OpenGenericVoidRequestHandler<T> : IRequestHandler<OpenGenericVoidRequest<T>>
    where T : class, ITypeArgument
{
    public Task Handle(OpenGenericVoidRequest<T> request, CancellationToken cancellationToken) => Task.CompletedTask;
}
class OpenGenericReturnTypeRequest<T> : IRequest<string>
    where T : class, ITypeArgument
{ }
class OpenGenericReturnTypeRequestHandler<T> : IRequestHandler<OpenGenericReturnTypeRequest<T>, string>
    where T : class, ITypeArgument
{
    public Task<string> Handle(OpenGenericReturnTypeRequest<T> request, CancellationToken cancellationToken) => Task.FromResult(nameof(request));
}

public class GenericPing<T> : IRequest<T>
    where T : Pong
{
    public T? Pong { get; set; }
}

public class GenericPingHandler<T> : IRequestHandler<GenericPing<T>, T>
    where T : Pong
{
    public Task<T> Handle(GenericPing<T> request, CancellationToken cancellationToken) => Task.FromResult(request.Pong!);
}

public class VoidGenericPing<T> : IRequest
    where T : Pong
{ }

public class VoidGenericPingHandler<T> : IRequestHandler<VoidGenericPing<T>>
    where T : Pong
{
    public Task Handle(VoidGenericPing<T> request, CancellationToken cancellationToken) => Task.CompletedTask;
}

public class Foo : IRequest<Bar>
{
    public string? Message { get; init; }
    public Action<Foo>? ThrowAction { get; init; }
}

public class Bar
{
    public string? Message { get; init; }
}

public class FooHandler : IRequestHandler<Foo, Bar>
{
    private readonly Logger _logger;

    public FooHandler(Logger logger)
    {
        _logger = logger;
    }
    public Task<Bar> Handle(Foo message, CancellationToken cancellationToken)
    {
        _logger.Messages.Add("Handler");

        message.ThrowAction?.Invoke(message);

        return Task.FromResult(new Bar { Message = message.Message + " Bar" });
    }
}