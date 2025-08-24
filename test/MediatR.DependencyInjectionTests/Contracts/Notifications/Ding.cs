namespace MediatR.DependencyInjectionTests.Contracts.Notifications;

public record Ding : INotification
{
    public class Door1 : INotificationHandler<Ding>
    {
        public Task Handle(Ding notification, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    internal class Door2 : INotificationHandler<Ding>
    {
        public Task Handle(Ding notification, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private class Door3 : INotificationHandler<Ding>
    {
        public Task Handle(Ding notification, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}