using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Deluxe.MediatR;

namespace MediatR.Examples;

public class JingHandler : IRequestHandler<Jing>
{
    private readonly TextWriter _writer;

    public JingHandler(TextWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(Jing request, CancellationToken cancellationToken)
    {
        return _writer.WriteLineAsync($"--- Handled Jing: {request.Message}, no Jong");
    }
}