using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Deluxe.MediatR;

public interface INotificationPublisher
{
    Task Publish(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification,
        CancellationToken cancellationToken);
}