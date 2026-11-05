# Requests and responses

The request/response interface handles both command and query scenarios. First, create a message:
```csharp
public class Ping : IRequest<string> { }
```
Next, create a handler:
```csharp
public class PingHandler : IRequestHandler<Ping, string>
{
    public Task<string> Handle(Ping request, CancellationToken cancellationToken)
    {
        return Task.FromResult("Pong");
    }
}
```

Finally, send a message through the mediator:

```csharp
var response = await mediator.Send(new Ping());
Debug.WriteLine(response); // "Pong"
```
In the case your message does not require a response, implement the non-generic `IRequest` interface and subsequent handler:
```csharp
public class OneWay : IRequest { }
public class OneWayHandler : IRequestHandler<OneWay>
{
    public Task Handle(OneWay request, CancellationToken cancellationToken)
    {
        // do work
        return Task.CompletedTask;
    }
}
```

## Request types

There are two flavors of requests in MediatR - ones that return a value, and ones that do not:

 - `IRequest<TResponse>` - the request returns a value
 - `IRequest` - the request does not return a value

Each request type has its own handler interface:

 - `IRequestHandler<TRequest, TResponse>` - implement this and return `Task<TResponse>`

Then for requests without return values:

 - `IRequestHandler<TRequest>` - implement this and you will return `Task`.
