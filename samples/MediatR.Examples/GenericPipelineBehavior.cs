using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Deluxe.MediatR;

namespace MediatR.Examples;

public class GenericPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly TextWriter _writer;

    public GenericPipelineBehavior(TextWriter writer)
    {
        _writer = writer;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        await _writer.WriteLineAsync("-- Handling Request");
        var response = await next();
        await  _writer.WriteLineAsync("-- Finished Request");
        return response;
    }
}