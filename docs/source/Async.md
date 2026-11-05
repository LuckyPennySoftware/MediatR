# Async

Send/publish are asynchronous from the `IMediator` side, and request and notification handlers return a `Task`.

Your handlers can use the async/await keywords as long as the work is awaitable:

```csharp
public class PingHandler : IRequestHandler<Ping, Pong>
{
    public async Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
    {
        return await DoPong(); // Whatever DoPong does
    }
}
```

These handlers are registered the same way as any other handler.
