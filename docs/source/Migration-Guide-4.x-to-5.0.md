# Migration guide: 4.x to 5.0

The 5.0 release includes breaking changes to the API:

 - The `SingleInstanceFactory` was renamed to `ServiceFactory`
 - The `MultiInstanceFactory` is removed. The `ServiceFactory` now resolves both single and multiple instances.
 - The `Mediator` class now only depends on a `ServiceFactory`
 - The void-based `Send` method on `Mediator` is gone. For void-based request handlers, the `Send` method will just return `Task<Unit>`. This change was made to simplify the pipeline.
 - The `IRequestHandler` interface for void-based handlers now inherits directly `IRequestHandler<in TRequest> : IRequestHandler<TRequest, Unit>`

## Migrating registration

With the two delegates collapsed into one, for most containers you can remove the `MultiInstanceFactory` registration, and rename the `SingleInstanceFactory` registration to `ServiceFactory`.

Some of the older containers cannot automatically handler `IEnumerable<T>`, check the samples folder where all of the registrations now reflect the new single delegate.

## Migrating handlers

The major change is the consolidation of handlers to center around `IRequestHandler<TRequest, TResponse>`, removing the void-based `IRequest` and `IRequestHandler` interfaces. For void-based requests, these instead return the `Unit` type, representing a void return (C# does not allow void as a type, unfortunately).

If you implement the `IRequestHandler<TRequest>` interface directly, you'll need to return `Task<Unit>`:

```c#
public class Ping : IRequest { }

public class PingHandler : IRequestHandler<Ping> {
    public Task<Unit> Handle(Ping request, CancellationToken cancellationToken) {
        // Work
        return Unit.Value; // for async/await
        return Unit.Task; // for pure Task-based methods
    }
}
```

If you still want the `Unit.Value` hiding, then you can inherit from the `AsyncRequestHandler` class and change your interface handler method to:

```c#
protected override Task Handle(...)
```

To use Find and Replace, with regular expressions, run:

| Find | Replace |
| ---- | ------- |
| `: AsyncRequestHandler<(.*), (.*)>` | `: IRequestHandler<$1, $2>` |
| `protected override async Task<(.*)> HandleCore\((.*)\)` | `public async Task<$1> Handle($2, CancellationToken cancellationToken)` |
| `protected override Task<(.*)> HandleCore\((.*)\)` | `public Task<$1> Handle($2, CancellationToken cancellationToken)` |
| `protected override async Task HandleCore\((.*)\)` | `protected override async Task Handle($1, CancellationToken cancellationToken)` |
| `protected override (.*) HandleCore\((.*)\)` | `protected override $1 Handle($2)` |
| `: AsyncNotificationHandler<(.*)>` | `: INotificationHandler<$1>` |

## Why did `IRequestHandler<T>` change?

In 4.x, `IRequestHandler<T>` was its own separate interface. This caused problems with many containers when combining with pipeline behaviors, pre-processors, or post-processors. The pipeline stuff expects `IRequestHandler<T, U>` and the split in the interface caused problems.

To mitigate this, `IRequestHandler<T>` now directly inherits `IRequestHandler<T, U>`, with some helper base classes for wrapping the `Unit.Value` return.

It's a bit unfortunate, but since `void` is not a first-class type in C# (unlike the `Unit` type in F#), it necessitated this change.
