using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Deluxe.MediatR.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Deluxe.MediatR.Pipeline;

/// <summary>
/// Behavior for executing all <see cref="IRequestExceptionHandler{TRequest,TResponse,TException}"/> instances
///     after an exception is thrown by the following pipeline steps
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class RequestExceptionProcessorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IServiceProvider _serviceProvider;

    public RequestExceptionProcessorBehavior(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            var state = new RequestExceptionHandlerState<TResponse>();

            var exceptionTypes = GetExceptionTypes(exception.GetType());

            var handlersForException = exceptionTypes
                .SelectMany(exceptionType => GetHandlersForException(exceptionType, request))
                .GroupBy(static handlerForException => handlerForException.Handler.GetType())
                .Select(static handlerForException => handlerForException.First())
                .Select(static handlerForException => (MethodInfo: GetMethodInfoForHandler(handlerForException.ExceptionType), handlerForException.Handler))
                .ToList();

            foreach (var handlerForException in handlersForException)
            {
                try
                {
                    await ((Task) (handlerForException.MethodInfo.Invoke(handlerForException.Handler, new object[] { request, exception, state, cancellationToken })
                                   ?? throw new InvalidOperationException("Did not return a Task from the exception handler."))).ConfigureAwait(false);
                }
                catch (TargetInvocationException invocationException) when (invocationException.InnerException != null)
                {
                    // Unwrap invocation exception to throw the actual error
                    ExceptionDispatchInfo.Capture(invocationException.InnerException).Throw();
                }

                if (state.Handled)
                {
                    break;
                }
            }

            if (!state.Handled)
            {
                throw;
            }

            if (state.Response is null)
            {
                throw;
            }

            return state.Response; //cannot be null if Handled
        }
    }
    private static IEnumerable<Type> GetExceptionTypes(Type? exceptionType)
    {
        while (exceptionType != null && exceptionType != typeof(object))
        {
            yield return exceptionType;
            exceptionType = exceptionType.BaseType;
        }
    }

    private IEnumerable<(Type ExceptionType, object Handler)> GetHandlersForException(Type exceptionType, TRequest request)
    {
        var exceptionHandlerInterfaceType = typeof(IRequestExceptionHandler<,,>).MakeGenericType(typeof(TRequest), typeof(TResponse), exceptionType);
        var enumerableExceptionHandlerInterfaceType = typeof(IEnumerable<>).MakeGenericType(exceptionHandlerInterfaceType);

        var exceptionHandlers = (IEnumerable<object>) _serviceProvider.GetRequiredService(enumerableExceptionHandlerInterfaceType);

        return HandlersOrderer.Prioritize(exceptionHandlers.ToList(), request)
            .Select(handler => (exceptionType, action: handler));
    }

    private static MethodInfo GetMethodInfoForHandler(Type exceptionType)
    {
        var exceptionHandlerInterfaceType = typeof(IRequestExceptionHandler<,,>).MakeGenericType(typeof(TRequest), typeof(TResponse), exceptionType);
        
        var handleMethodInfo = exceptionHandlerInterfaceType.GetMethod(nameof(IRequestExceptionHandler<TRequest, TResponse, Exception>.Handle))
                           ?? throw new InvalidOperationException($"Could not find method {nameof(IRequestExceptionHandler<TRequest, TResponse, Exception>.Handle)} on type {exceptionHandlerInterfaceType}");

        return handleMethodInfo;
    }
}