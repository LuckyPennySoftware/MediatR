# Behaviors

New in MediatR 3.0 are behaviors, which allow you to build your own pipeline directly inside of MediatR without resolving to using decorators around your handlers. It's a more natural way to enhance your handlers with behavior and better supported in containers.

A pipeline behavior is an implementation of `IPipelineBehavior<TRequest, TResponse>`. It represents a similar pattern to filters in ASP.NET MVC/Web API or pipeline behaviors in NServiceBus. Your pipeline behavior needs to implement one method:

```csharp
Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
```

The `request` parameter is the `request` object passed in through `IMediator.Send`, while the `next` parameter is an async continuation for the next action in the behavior chain. In your behavior, you'll need to await or return the invocation of the `next` delegate. A simple example:

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Handling {typeof(TRequest).Name}");
        var response = await next();
        _logger.LogInformation($"Handled {typeof(TResponse).Name}");

        return response;
    }
}
```

Because the `next` delegate does not accept the `TRequest` as a parameter, you can mutate the incoming request but cannot replace it.

## Compatibility of pipeline behaviors
Pipeline behaviors wrap request handlers: both `IRequestHandler<TRequest,TResponse>` and `IRequestHandler<TRequest>`, where `TResponse` is `Unit`. They can't be used with `INotificationHandler<TNotification>`.

## Registering pipeline behaviors

MediatR supports registering pipeline behaviors through `AddMediatR` for closed generic behaviors and open generic behaviors:

```csharp
services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssemblyContaining<Ping>();
    cfg.AddBehavior<IPipelineBehavior<Ping, Pong>, PingPongBehavior>();
    cfg.AddOpenBehavior(typeof(OuterBehavior<,>));
    cfg.AddOpenBehavior(typeof(InnerBehavior<,>));
    cfg.AddOpenBehavior(typeof(ConstrainedBehavior<,>));
});
```

Just register the behaviors in the order you would like them to be called. For void handlers, the type of `TResponse` will be `Unit`.

## Void Requests

For void requests, the `TResponse` type will still be `Unit`. There are not separate pipelines/processors for void requests, instead MediatR wraps your handler with the `Unit` type return value. Your behavior will still need to return the value of executing the delegate, even if your handler is `void`.

## Built-in behaviors

To ease development, two built-in behaviors exist:

- `RequestPreProcessorBehavior` will execute `IRequestPreProcessor` implementations before any handlers are called
- `RequestPostProcessorBehavior` will execute `IRequestPostProcessor` implementations after all handlers are called

You can find these in the `MediatR.Pipeline` namespace, and will be registered if in `AddMediatR` you register any pre/post processors.

## Stream Pipeline Behaviors

New with MediatR 10.0 is stream requests to have a request return an `IAsyncEnumerable<TResponse>`. Stream behaviors execute around the entire request, not each individual yielded response.

To create a stream pipeline behavior, implement the `IStreamPipelineBehavior` interface similar to a normal request behavior, returning an `IAsyncEnumerable<TResponse>`.
