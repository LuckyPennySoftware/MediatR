# Generic requests and handlers

## Configuration

These options on the `AddMediatR` configuration control generic handler registration:

| Option | Default | Description |
| --- | --- | --- |
| `RegisterGenericHandlers` | `false` | Whether MediatR registers handlers that have generic type parameters. |
| `MaxGenericTypeParameters` | `10` | The maximum number of type parameters a generic request handler can have. Set to `0` to remove the limit. |
| `MaxTypesClosing` | `100` | The maximum number of types that can close a generic request type parameter. Set to `0` to remove the limit. |
| `MaxGenericTypeRegistrations` | `125000` | The maximum number of generic handler registrations MediatR will create. Set to `0` to remove the limit. |
| `RegistrationTimeout` | `15000` | The time in milliseconds before registration fails with a `TimeoutException`. Set to `0` for no timeout. |

In MediatR 14.3 and earlier, setting `MaxGenericTypeRegistrations` or `RegistrationTimeout` to `0` makes registration fail instead of removing the limit. On those versions, use a larger `MaxGenericTypeRegistrations` value, and `-1` for no `RegistrationTimeout`.

## Implementation

Requests can be made generic such that work done to or for specific type arguments can be written once towards an abstraction.

```csharp
//you can also configure the other options shown above here as well
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterGenericHandlers = true;
    cfg.RegisterServicesFromAssembly(typeof(GenericPing<>).Assembly);
});
```

```csharp
public interface IPong
{
    string? Message { get; }
}

public class Pong : IPong
{
    public string? Message { get; set; }
}

//generic request definition
public class GenericPing<T> : IRequest<T>
    where T : class, IPong
{
    public T? Pong { get; set; }
}

//generic request handler
public class GenericPingHandler<T> : IRequestHandler<GenericPing<T>, T>
    where T : class, IPong
{
    public Task<T> Handle(GenericPing<T> request, CancellationToken cancellationToken) => Task.FromResult(request.Pong!);
}

//usage
var pong = await _mediator.Send(new GenericPing<Pong>{ Pong = new() { Message = "Ping Pong" } });
Console.WriteLine(pong.Message); //would output "Ping Pong"
```
### Notes
* All assemblies from which your generic handlers, requests, or type parameters must be added to be registered.
* Only non-abstract classes are used to close generic type parameters. Value types such as `int` are never used, even if their assembly is registered, so register handlers closed over value types yourself.
