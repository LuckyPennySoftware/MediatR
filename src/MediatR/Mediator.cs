namespace MediatR
{
    using Internal;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Default mediator implementation relying on single- and multi instance delegates for resolving handlers.
    /// </summary>
    public class Mediator : IMediator
    {
        private readonly ServiceFactory _serviceFactory;
        private readonly ITaskExecutionStrategy _taskExecutionStrategy;

        private static readonly ConcurrentDictionary<Type, object> _requestHandlers = new ConcurrentDictionary<Type, object>();
        private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> _notificationHandlers = new ConcurrentDictionary<Type, NotificationHandlerWrapper>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Mediator"/> class.
        /// </summary>
        /// <param name="serviceFactory">The single instance factory.</param>
        /// <param name="sequentialExecution">Sequential execution switch.</param>
        public Mediator(ServiceFactory serviceFactory, bool sequentialExecution = false)
        {
            _serviceFactory = serviceFactory;
            _taskExecutionStrategy = sequentialExecution
                ? (ITaskExecutionStrategy) new SequentialExecutionStrategy()
                : new DefaultExecutionStrategy();
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var requestType = request.GetType();

            var handler = (RequestHandlerWrapper<TResponse>)_requestHandlers.GetOrAdd(requestType,
                t => Activator.CreateInstance(typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(requestType, typeof(TResponse))));

            return handler.Handle(request, cancellationToken, _serviceFactory);
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            var notificationType = notification.GetType();
            var handler = _notificationHandlers.GetOrAdd(notificationType,
                t => (NotificationHandlerWrapper)Activator.CreateInstance(typeof(NotificationHandlerWrapperImpl<>).MakeGenericType(notificationType)));

            return handler.Handle(notification, cancellationToken, _serviceFactory, PublishCore);
        }

        /// <summary>
        /// Override in a derived class to control how the tasks are awaited. By default the implementation is <see cref="Task.WhenAll(IEnumerable{Task})" />
        /// </summary>
        /// <param name="allHandlers">Enumerable of tasks representing invoking each notification handler</param>
        /// <returns>A task representing invoking all handlers</returns>
        protected virtual Task PublishCore(IEnumerable<Task> allHandlers) =>
            _taskExecutionStrategy.Execute(allHandlers);
    }
}
