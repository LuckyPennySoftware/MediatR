# Migration guide: 10.x to 11.0

This release contains breaking changes to interfaces using `CancellationToken`. C# design guidelines recommend `CancellationToken` parameters be placed last ([CA1068](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1068)). As such, these types were changed to conform to this rule:

- `IPipelineBehavior`
- `IStreamPipelineBehavior`
- `RequestExceptionActionProcessorBehavior`
- `RequestExceptionProcessorBehavior`
- `RequestPostProcessorBehavior`
- `RequestPreProcessorBehavior`

To fix these, move any implementations to have the `CancellationToken` parameter last.
