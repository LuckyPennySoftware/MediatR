using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Deluxe.MediatR.Wrappers;

internal abstract class StreamRequestHandlerBase
{
    public abstract IAsyncEnumerable<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

internal abstract class StreamRequestHandlerWrapper<TResponse> : StreamRequestHandlerBase
{
    public abstract IAsyncEnumerable<TResponse> Handle(
        IStreamRequest<TResponse> request, 
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

internal class StreamRequestHandlerWrapperImpl<TRequest, TResponse> 
    : StreamRequestHandlerWrapper<TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    public override async IAsyncEnumerable<object?> Handle(object request, IServiceProvider serviceProvider, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in Handle((IStreamRequest<TResponse>) request, serviceProvider, cancellationToken))
        {
            yield return item;
        }
    }

    public override async IAsyncEnumerable<TResponse> Handle(IStreamRequest<TResponse> request, 
        IServiceProvider serviceProvider, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IAsyncEnumerable<TResponse> Handler() => serviceProvider
            .GetRequiredService<IStreamRequestHandler<TRequest, TResponse>>()
            .Handle((TRequest) request, cancellationToken);

        var items = serviceProvider
            .GetServices<IStreamPipelineBehavior<TRequest, TResponse>>()
            .Reverse()
            .Aggregate(
                (StreamHandlerDelegate<TResponse>) Handler, 
                (next, pipeline) => () => pipeline.Handle(
                    (TRequest) request, 
                    () => NextWrapper(next(), cancellationToken),
                    cancellationToken
                )
            )();

        await foreach ( var item in items.WithCancellation(cancellationToken) )
        {
            yield return item;
        }
    }


    private static async IAsyncEnumerable<T> NextWrapper<T>(
        IAsyncEnumerable<T> items,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var cancellable = items
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false);
        await foreach (var item in cancellable)
        {
            yield return item;
        }
    }

}