using System;
using System.Threading;
using System.Threading.Tasks;
using Deluxe.MediatR;

namespace MediatR.Examples.PublishStrategies;

public class SyncPingedHandler : INotificationHandler<Pinged>
{
    public SyncPingedHandler(string name)
    {
        Name = name;
    }

    public string Name { get; set; }

    public Task Handle(Pinged notification, CancellationToken cancellationToken)
    {
        if (Name == "2")
        {
            throw new ArgumentException("Name cannot be '2'");
        }

        Console.WriteLine($"[SyncPingedHandler {Name}] {DateTime.Now:HH:mm:ss.fff} : Pinged");
        Thread.Sleep(100);
        Console.WriteLine($"[SyncPingedHandler {Name}] {DateTime.Now:HH:mm:ss.fff} : After pinged");
        return Task.CompletedTask;
    }
}