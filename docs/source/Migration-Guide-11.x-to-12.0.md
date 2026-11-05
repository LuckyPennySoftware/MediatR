# Migration guide: 11.x to 12.0

This release contains major breaking changes to MediatR. Namely:
- Depending directly on `IServiceProvider` to resolve services
- Making "void" request handlers return `Task` instead of `Unit`
- `IRequest` does not inherit `IRequest<Unit>` instead `IBaseRequest`
- Consolidating the `MediatR.Extensions.Microsoft.DependencyInjection` package into this one
- Rolling back stricter generic constraints in various behavior interfaces
- Various behavior registrations now inside of the `IServiceCollection` extension
- Other minor breaking changes, detailed below

## Leveraging `Microsoft.Extensions.DependencyInjection.Abstractions` directly

The Mediator class now references `IServiceProvider` directly, instead of the previous custom delegate:

```diff
-    public Mediator(ServiceFactory serviceFactory)
-        => _serviceFactory = serviceFactory;
+    public Mediator(IServiceProvider serviceProvider)
+        : this(serviceProvider, new ForeachAwaitPublisher()) { }
+    public Mediator(IServiceProvider serviceProvider, INotificationPublisher publisher)
+    {
+        _serviceProvider = serviceProvider;
+        _publisher = publisher;
+    }
```

The `ServiceFactory` delegate was removed:

```diff
-public delegate object ServiceFactory(Type serviceType);
```

The functionality of `MediatR.Extensions.DependencyInjection.Abstractions` is now folded in directly to `MediatR` itself, so you can remove the reference to the extensions package and call the `MediatR` package directly:

```csharp
services.AddMediatR(/* registration */);
```

For codebases not using the extension package, you may either implement `IServiceProvider` directly, or for many containers, they already directly support `IServiceProvider` directly.

## Mediator constructor breaking changes

In addition to derived `Mediator` classes, the `Mediator` class also supports injecting a custom or built-in `INotificationPublisher` instance. This required an additional parameter to the constructor. As seen in the previous section, the existing single-argument constructor is preserved with a default implementation

## Void handlers (`IRequest` and `IRequestHandler`)

The `MediatR.Contracts` package was updated to change the `IRequest` interface to not inherit `IRequest<Unit>`, to support void handlers better:

```diff
-public interface IRequest : IRequest<Unit> { }
+public interface IRequest : IBaseRequest { }
```

The contracts package is now version 2.0 with this breaking change.

Handlers changed for this as well:

```diff
- public interface IRequestHandler<in TRequest> : IRequestHandler<TRequest, Unit>
-     where TRequest : IRequest<Unit>
+ public interface IRequestHandler<in TRequest>
+    where TRequest : IRequest {
+     Task Handle(TRequest request, CancellationToken cancellationToken);
+ }
```

Modify your `IRequest` handlers to remove the `Unit` from the result:

```diff
public class VoidRequest : IRequest { }

public class VoidRequestHandler : IRequestHandler<VoidRequest>
{
-    public Task<Unit> Handle(VoidRequest request, CancellationToken cancellationToken)
+    public Task Handle(VoidRequest request, CancellationToken cancellationToken)
    {
-        return Unit.Task;
+        return Task.CompletedTask;
    }
}
```

or async:

```diff
public class VoidRequest : IRequest { }

public class VoidRequestHandler : IRequestHandler<VoidRequest>
{
-    public async Task<Unit> Handle(VoidRequest request, CancellationToken cancellationToken)
+    public async Task Handle(VoidRequest request, CancellationToken cancellationToken)
    {
          await SomeThing();
-        return Unit.Value;
+        return;
    }
}
```

This also added a new method to `Mediator` class:

```diff
public interface IMediator {
+    Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
+        where TRequest : IRequest;
}
```

**If you also have open generic behaviors, processors, etc. you MUST correct the generic constraints as described below.**

For void requests, the `TResponse` type will still be `Unit`. There are not separate pipelines/processors for void requests, instead MediatR wraps your handler with the `Unit` type return value.

## Rolling back generic constraints

The generic constraints added by 10.0 were a bit too strict for some situations and didn't add much value, so those are rolled back:

```diff
public class GenericPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
-    where TRequest : IRequest<TResponse>
+    where TRequest : notnull
{
```
- Modification of generic constraint on `IPipelineBehavior` from `where TRequest : IRequest<TResponse>` to `where TRequest notnull`
- Modification of generic constraint on `IStreamPipelineBehavior` from `where TRequest : IStreamRequest<TResponse>` to `where TRequest notnull`
- Modification of generic constraint on `IRequestExceptionHandler` from `where TRequest : IRequest<TResponse>` to `where TRequest notnull`
- Modification of generic constraint on `IRequestPostProcessor` from `where TRequest : IRequest<TResponse>` to `where TRequest notnull`

For open generic behaviors, processors etc. replace the generic constraint to `notnull`.

## Removal of helper classes

With the addition of the void handlers, these classes are removed:

- `public abstract class AsyncRequestHandler<TRequest>`
- `public abstract class RequestHandler<TRequest, TResponse>`
- `public abstract class RequestHandler<TRequest>`

The void-based helpers are no longer needed. The "sync" versions of handlers made testing a challenge, so these were removed. Implement the base interface directly and return `Task.CompletedTask` or `Task.FromResult` for non-async business logic in handlers.

## Service registration through configuration object exclusively

Service registration through `IServiceCollection` previously had overloads for various options. These are now consolidated into the single `MediatrServiceConfiguration` object. Overloads that passed through the assemblies to scan need to register using methods on this configuration object instead:

```diff
-services.AddMediatR(typeof(Ping));
+services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Ping).Assembly));
```

There are various overloads of `RegisterServicesFromAssembly` to match the overloads originally in `AddMediatR`.
