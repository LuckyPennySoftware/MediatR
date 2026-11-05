# Migration guide: 9.x to 10.0

This release includes the following breaking changes in the API:
- Addition of `CreateStream` methods to `IMediator`
- Modification of generic constraint on `IPipelineBehavior` from `where TRequest notnull` to `where TRequest : IRequest<TResponse>`
- Modification of generic constraint on `IRequestExceptionHandler` from `where TRequest notnull` to `where TRequest : IRequest<TResponse>`
- Modification of generic constraint on `IRequestPostProcessor` from `where TRequest notnull` to `where TRequest : IRequest<TResponse>`
- Request types (`IRequest`, `IRequest<TResponse>`, `IBaseRequest`, `IStreamRequest<TResponse>`, and `INotification`) moved to the [`MediatR.Contracts`](https://www.nuget.org/packages/MediatR.Contracts) package

For the `IMediator` change, it will likely only affect manually created implementations for testing purposes.

The modification of the generic constraints affects existing open generic implementations. These will likely need to include an additional constraint:

```diff
public class GenericPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
+    where TRequest : IRequest<TResponse>
{
```
