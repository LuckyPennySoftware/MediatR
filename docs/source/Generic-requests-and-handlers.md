# Generic requests and handlers

## Configuration

```csharp
 /// <summary>
 /// Configure the maximum number of type parameters that a generic request handler can have. To Disable this constraint, set the value to 0.
 /// </summary>
 public int MaxGenericTypeParameters { get; set; } = 10;

 /// <summary>
 /// Configure the maximum number of types that can close a generic request type parameter constraint.  To Disable this constraint, set the value to 0.
 /// </summary>
 public int MaxTypesClosing { get; set; } = 100;

 /// <summary>
 /// Configure the Maximum Amount of Generic RequestHandler Types MediatR will try to register.  To Disable this constraint, set the value to 0.
 /// </summary>
 public int MaxGenericTypeRegistrations { get; set; } = 125000;

 /// <summary>
 /// Configure the Timeout in Milliseconds that the GenericHandler Registration Process will exit with error.  To Disable this constraint, set the value to 0.
 /// </summary>
 public int RegistrationTimeout { get; set; } = 15000;

 /// <summary>
 /// Flag that controlls whether MediatR will attempt to register handlers that containg generic type parameters.
 /// </summary>
 public bool RegisterGenericHandlers { get; set; } = false;
```
## Implementation

Requests can be made generic such that work done to or for specific type arguments can be written once towards an abstraction.

```csharp
//you can also configure other values shown above here as well
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
* To use primitives as generic types in requests and handlers you will need to include the assembly in which they are defined I.E. `typeof(int).Assembly`.
