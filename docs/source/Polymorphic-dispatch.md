# Polymorphic dispatch

Handler interfaces are contravariant:

```csharp
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest message, CancellationToken cancellationToken);
}

public interface INotificationHandler<in TNotification>
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
```

Containers that support generic variance will dispatch accordingly. For example, you can have an `INotificationHandler<INotification>` to handle all notifications.
