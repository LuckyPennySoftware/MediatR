# Migration guide: 3.x to 4.0

The 4.0 release includes breaking changes in the API:

 - The request handler interfaces are consolidated into one, `IRequestHandler`
 - The notification handler interfaces are consolidated into one, `INotificationHandler`
 - The behavior pipeline interface now has a cancellation token
 - The request pre-processor interface now has a cancellation token

The main motivation behind these changes were to remove the ambiguity around handler resolution at runtime. The 3.x releases used a `try...catch` to find the correct handler implementation. The 4.0 release removes the multiple interfaces, making both configuration and resolution simpler.

To ease the transition, the following find/replace values (using regular expressions) can quickly migrate a codebase from 3.x to 4.0. Run these in order:

| Find | Replace |
| ---- | ------- |
| `IRequestHandler` | `RequestHandler` |
| `IAsyncRequestHandler` | `AsyncRequestHandler` |
| `ICancellableAsyncRequestHandler` | `IRequestHandler` |
| `INotificationHandler` | `NotificationHandler` |
| `IAsyncNotificationHandler` | `AsyncNotificationHandler` |
| `ICancellableNotificationRequestHandler` | `INotificationHandler` |
| `public async Task<([a-zA-Z0-9_<>]*)> Handle\(([a-zA-Z0-9_<>]*) ([a-zA-Z0-9_<>]*)\)` | `protected override async Task<$1> HandleCore($2 $3)` |
| `public Task<([a-zA-Z0-9_<>]*)> Handle\(([a-zA-Z0-9_<>]*) ([a-zA-Z0-9_<>]*)\)` | `protected override Task<$1> HandleCore($2 $3)` |
| `public async Task Handle\(([a-zA-Z0-9_<>]*) ([a-zA-Z0-9_<>]*)\)` | `protected override async Task HandleCore($1 $2)` |
| `public Task Handle\(([a-zA-Z0-9_<>]*) ([a-zA-Z0-9_<>]*)\)` | `protected override Task HandleCore($1 $2)` |
| `public void Handle\(([a-zA-Z0-9_<>]*) ([a-zA-Z0-9_<>]*)\)` | `protected override void HandleCore($1 $2)` |
| `public ([a-zA-Z0-9_<>]*) Handle\(([a-zA-Z0-9_<>]*) ([a-zA-Z0-9_<>]*)\)` | `protected override $1 HandleCore($2 $3)` |

Pipeline and pre-processors will need to be changed manually, adding the new `CancellationToken` parameter.
