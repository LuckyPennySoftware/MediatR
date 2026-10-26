MediatR
=======

![CI](https://github.com/LuckyPennySoftware/MediatR/workflows/CI/badge.svg)
[![NuGet](https://img.shields.io/nuget/dt/mediatr.svg)](https://www.nuget.org/packages/mediatr) 
[![NuGet](https://img.shields.io/nuget/vpre/mediatr.svg)](https://www.nuget.org/packages/mediatr)
[![MyGet (dev)](https://img.shields.io/myget/mediatr-ci/v/MediatR.svg)](https://myget.org/gallery/mediatr-ci)

Simple mediator implementation in .NET

In-process messaging with no dependencies.

Supports request/response, commands, queries, notifications and events, synchronous and async with intelligent dispatching via C# generic variance.

Examples in the [wiki](https://github.com/LuckyPennySoftware/MediatR/wiki).

### Installing MediatR

You should install [MediatR with NuGet](https://www.nuget.org/packages/MediatR):

    Install-Package MediatR
    
Or via the .NET Core command line interface:

    dotnet add package MediatR

Either commands, from Package Manager Console or .NET Core CLI, will download and install MediatR and all required dependencies.

### Using Contracts-Only Package

To reference only the contracts for MediatR, which includes:

- `IRequest` (including generic variants)
- `INotification`
- `IStreamRequest`

Add a package reference to [MediatR.Contracts](https://www.nuget.org/packages/MediatR.Contracts)

This package is useful in scenarios where your MediatR contracts are in a separate assembly/project from handlers. Example scenarios include:
- API contracts
- GRPC contracts
- Blazor

### Registering with `IServiceCollection`

MediatR supports `Microsoft.Extensions.DependencyInjection.Abstractions` directly. To register various MediatR services and handlers:

```
services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Startup>());
```

or with an assembly:

```
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Startup).Assembly));
```

This registers:

- `IMediator` as transient
- `ISender` as transient
- `IPublisher` as transient
- `IRequestHandler<,>` concrete implementations as transient
- `IRequestHandler<>` concrete implementations as transient
- `INotificationHandler<>` concrete implementations as transient
- `IStreamRequestHandler<>` concrete implementations as transient
- `IRequestExceptionHandler<,,>` concrete implementations as transient
- `IRequestExceptionAction<,>)` concrete implementations as transient

This also registers open generic implementations for:

- `INotificationHandler<>`
- `IRequestExceptionHandler<,,>`
- `IRequestExceptionAction<,>`

To register behaviors, stream behaviors, pre/post processors:

```csharp
services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(Startup).Assembly);
    cfg.AddBehavior<PingPongBehavior>();
    cfg.AddStreamBehavior<PingPongStreamBehavior>();
    cfg.AddRequestPreProcessor<PingPreProcessor>();
    cfg.AddRequestPostProcessor<PingPongPostProcessor>();
    cfg.AddOpenBehavior(typeof(GenericBehavior<,>));
    });
```

With additional methods for open generics and overloads for explicit service types.

### Setting the license key

You can set the license key when registering MediatR:

```csharp
services.AddMediatR(cfg => 
{
    cfg.LicenseKey = "<license key here>";
})
```

Or if not using Microsoft.Extensions.DependencyInjection:

```csharp
Mediator.LicenseKey = "<license key here>";
```

> [!TIP]
> The license key does not need to be set on client applications (such as Blazor WASM).
> Turn off the license warning by configuring logging in your logging start configuration:
> `builder.Logging.AddFilter("LuckyPennySoftware.MediatR.License", LogLevel.None);`

#### Auto-discovery via environment variables

If no license key is set in code, MediatR looks for one in environment variables. This is convenient for containerized and cloud environments, and for enterprises that share a single key across many services without code changes:

- `MEDIATR_LICENSE_KEY` – the MediatR-specific license key.
- `LUCKYPENNY_LICENSE_KEY` – a shared key usable across Lucky Penny products (for example, [AutoMapper](https://github.com/LuckyPennySoftware/AutoMapper) reads the same variable). Because it is shared, the key must be for a license that includes MediatR (a `Bundle` or MediatR edition); an AutoMapper-only license will not validate here.

The license key is resolved in the following order of precedence, using the first value found:

1. An explicit value set in code (`cfg.LicenseKey` or `Mediator.LicenseKey`).
2. The `MEDIATR_LICENSE_KEY` environment variable.
3. The `LUCKYPENNY_LICENSE_KEY` environment variable.

No code change is required when using an environment variable—just register MediatR as usual without setting the license key.

You can register for your license key at [MediatR.io](https://mediatr.io)

#### Renewal grace period

License keys include a 30-day grace period after the end of their term. When your subscription renews, you receive a new license key, and the previous key keeps logging only a WARNING (not an ERROR) for 30 days after its term ends. This gives you time to deploy the renewed key. Your current license key is always available on your [account page](https://luckypennysoftware.com/account).
