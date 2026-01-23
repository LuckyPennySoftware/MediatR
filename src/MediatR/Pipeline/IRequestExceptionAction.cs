using System;
using System.Threading;
using System.Threading.Tasks;

namespace Deluxe.MediatR.Pipeline;

/// <summary>
/// Defines an exception action for a request
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TException">Exception type</typeparam>
public interface IRequestExceptionAction<in TRequest, in TException>
    where TRequest : notnull
    where TException : Exception
{
    /// <summary>
    /// Called when the request handler throws an exception
    /// </summary>
    /// <param name="request">Request instance</param>
    /// <param name="exception">The thrown exception</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An awaitable task</returns>
    Task Execute(TRequest request, TException exception, CancellationToken cancellationToken);
}
