# Notifications

For notifications, first create your notification message:

```csharp
public class Ping : INotification { }
```

Next, create zero or more handlers for your notification:

```csharp
public class Pong1 : INotificationHandler<Ping>
{
    public Task Handle(Ping notification, CancellationToken cancellationToken)
    {
        Debug.WriteLine("Pong 1");
        return Task.CompletedTask;
    }
}

public class Pong2 : INotificationHandler<Ping>
{
    public Task Handle(Ping notification, CancellationToken cancellationToken)
    {
        Debug.WriteLine("Pong 2");
        return Task.CompletedTask;
    }
}
```

Finally, publish your message via the mediator:

```csharp
await mediator.Publish(new Ping());
```

## Custom notification publishers

MediatR (starting with version 12) supports custom publish strategies:

```csharp
public interface INotificationPublisher
{
    Task Publish(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification,
        CancellationToken cancellationToken);
}
```

Custom publish strategies are injected into the `Mediator` class, and are registered with `AddMediatR`. Either supply an instance, which is registered as a singleton:

```csharp
services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.NotificationPublisher = new MyCustomPublisher();
});
```

Or supply a type, which is registered with the configured `Lifetime` (transient by default):

```csharp
services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.NotificationPublisherType = typeof(MyCustomPublisher);
});
```

If both are set, `NotificationPublisherType` is used.

There are two built-in notification publishers:

- `ForeachAwaitPublisher` - the default, existing implementation
- `TaskWhenAllPublisher` - awaits `Task.WhenAll` for all notification handler tasks. Does NOT use `Task.Run`

Custom notification publishers can also use the handler instance to do custom logic like ordering, skipping handlers, etc. The handler instance is on the `NotificationHandlerExecutor` class, as well as the delegate to call the handler instance itself.
