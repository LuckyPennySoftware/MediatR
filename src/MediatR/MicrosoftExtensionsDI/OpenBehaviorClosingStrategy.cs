namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Determines who closes open behaviors and processors whose type parameters don't map one-to-one, in order,
/// onto the service interface, such as <c>Behavior&lt;TRequest, TItem&gt; : IPipelineBehavior&lt;TRequest, List&lt;TItem&gt;&gt;</c>.
/// Open generics that do map one-to-one, such as <c>Behavior&lt;TRequest, TResponse&gt;</c>, are always registered as open generics.
/// </summary>
public enum OpenBehaviorClosingStrategy
{
    /// <summary>
    /// MediatR scans the registered assemblies for request types and registers a closed implementation for each matching request.
    /// Requests outside the scanned assemblies, including closed generic request types, don't get the behavior.
    /// </summary>
    CloseForScannedRequests,

    /// <summary>
    /// MediatR registers only the open generic and leaves closing to the container when each request is resolved.
    /// Use this with containers that close open generics by matching the service type (such as DryIoc, LightInject or Autofac)
    /// when requests come from assemblies that aren't scanned. The Microsoft and Stashbox containers close open generics
    /// positionally: they skip or fail on these behaviors, and reject an open generic whose number of type parameters differs
    /// from the service interface when the service provider is built.
    /// Implementations with a type parameter that only appears in a constraint can't be closed by any container,
    /// so they are still closed for scanned requests.
    /// </summary>
    CloseInContainer
}
