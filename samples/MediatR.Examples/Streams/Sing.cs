using Deluxe.MediatR;

namespace MediatR.Examples.Streams;

public class Sing : IStreamRequest<Song>
{
    public string Message { get; set; }
}