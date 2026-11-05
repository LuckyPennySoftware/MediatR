# Setup

Install the package via NuGet first:

```
Install-Package MediatR
```

Or with the .NET CLI:

```
dotnet add package MediatR
```

MediatR directly references `Microsoft.Extensions.DependencyInjection.Abstractions` leveraging `IServiceProvider`. Typical usage is to use `IServiceCollection` directly:

```csharp
services.AddMediatR(cfg => {
    cfg.LicenseKey = "<License Key here>";
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});
```

This method registers the known MediatR types:

- `IMediator` as transient
- `ISender` as transient
- `IPublisher` as transient

For each assembly registered, the `AddMediatR` method will scan those assemblies for MediatR types (excluding behaviors):

- `IRequestHandler<,>` concrete implementations as transient
- `IRequestHandler<>` concrete implementations as transient
- `INotificationHandler<>` concrete implementations as transient
- `IStreamRequestHandler<,>` concrete implementations as transient
- `IRequestExceptionHandler<,,>` concrete implementations as transient
- `IRequestExceptionAction<,>` concrete implementations as transient

Behaviors and pre/post processors must be registered explicitly through the `AddXyz` methods.
