using System;
using System.Threading;
using System.Threading.Tasks;
using Deluxe.MediatR;

namespace MediatR.Examples.PublishStrategies;

public class AsyncPingedHandler : INotificationHandler<Pinged>
{
    public AsyncPingedHandler(string name)
    {
        Name = name;
    }

    public string Name { get; set; }

    public async Task Handle(Pinged notification, CancellationToken cancellationToken)
    {
        if (Name == "2")
        {
            throw new ArgumentException("Name cannot be '2'");
        }

        Console.WriteLine($"[AsyncPingedHandler {Name}] {DateTime.Now:HH:mm:ss.fff} : Pinged");
        await Task.Delay(100).ConfigureAwait(false);
        Console.WriteLine($"[AsyncPingedHandler {Name}] {DateTime.Now:HH:mm:ss.fff} : After pinged");
    }
}