using Deluxe.MediatR;

namespace MediatR.Examples;

public class Jing : IRequest
{
    public string Message { get; set; }
}