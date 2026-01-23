using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Deluxe.MediatR;

namespace MediatR.Examples.PublishStrategies;

public class CustomMediator : Mediator
{
    private readonly Func<IEnumerable<NotificationHandlerExecutor>, INotification, CancellationToken, Task> _publish;

    public CustomMediator(IServiceProvider serviceFactory, Func<IEnumerable<NotificationHandlerExecutor>, INotification, CancellationToken, Task> publish) : base(serviceFactory) 
        => _publish = publish;

    protected override Task PublishCore(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification, CancellationToken cancellationToken) 
        => _publish(handlerExecutors, notification, cancellationToken);
}