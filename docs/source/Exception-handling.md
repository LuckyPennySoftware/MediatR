# Exception handling

## Exception handler pipeline step

Exception handler implemented by using `IPipelineBehavior` concept. It requires to add the `RequestExceptionProcessorBehavior` to the request execution Pipeline. This behavior is not registered unless `AddMediatR` finds request exception behaviors.

```cs
namespace MediatR.Pipeline;

/// <summary>
/// Behavior for executing all <see cref="IRequestExceptionHandler{TRequest,TResponse,TException}"/> instances
///     after an exception is thrown by the following pipeline steps
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class RequestExceptionProcessorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
```

## Exception action pipeline step

Exception action implemented by using `IPipelineBehavior` concept. It requires to add the `RequestExceptionActionProcessorBehavior` to the request execution Pipeline.
If place `RequestExceptionActionProcessorBehavior` before `RequestExceptionProcessorBehavior`, actions will be called only for unhandled exceptions. The order `AddMediatR` uses is set by `RequestExceptionActionProcessorStrategy`, which defaults to `ApplyForUnhandledExceptions`.

```cs
namespace MediatR.Pipeline;

/// <summary>
/// Behavior for executing all <see cref="IRequestExceptionAction{TRequest,TException}"/> instances
///     after an exception is thrown by the following pipeline steps
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class RequestExceptionActionProcessorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
```

## Handlers and actions priority execution

All available handlers/actions will be sorted by applying next rules:

 - The handler/action has a higher priority if it belongs to the current assembly (same assembly with request) and the other is not. If none of the objects belong to the current assembly, they can be considered equal. If both objects belong to the current assembly, they can't be compared only by this criterion - compare by next rule;
 - The handler/action has a higher priority if it belongs to the current/child request namespace and the other is not. If both objects belong to the current/child request namespace, they can be considered equal. If none of the objects belong to the current/child request namespace, they can't be compared by this criterion - compare by next rule;
 - The handler/action has a higher priority if it namespace is part of the current location (request namespace) and the other is not. If both objects are part of the current location, the closest has higher priority. If none of the objects are part of the current location, they can be considered equal.

## Override handler/action

Create a new handler / action that inherits the handler / action that you want to override, and save it in accordance with the priority rules.

## Exception handler vs Exception action

 - All actions will be performed for the thrown exception. However, only one handler can handle the thrown exception;
 - The exception will be re-thrown after all actions are completed. But the exception can be handled by the exception handler, and the result will be returned to the caller;
 - The actions execution process faster. Because the exception handler works like `try / catch` block with several `catch` and search handlers for all base exceptions separately;
 - Both support priority sorting;
 - Both support overriding.
