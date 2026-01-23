using Deluxe.MediatR;

namespace MediatR.Examples;

public class Ping : IRequest<Pong>
{
    public string Message { get; set; }
}