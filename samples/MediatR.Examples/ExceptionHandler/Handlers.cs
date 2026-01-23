using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Deluxe.MediatR;

namespace MediatR.Examples.ExceptionHandler;

public class PingResourceHandler : IRequestHandler<PingResource, Pong>
{
    private readonly TextWriter _writer;

    public PingResourceHandler(TextWriter writer) => _writer = writer;

    public Task<Pong> Handle(PingResource request, CancellationToken cancellationToken)
    {
        throw new ResourceNotFoundException();
    }
}

public class PingNewResourceHandler : IRequestHandler<PingNewResource, Pong>
{
    private readonly TextWriter _writer;

    public PingNewResourceHandler(TextWriter writer) => _writer = writer;

    public Task<Pong> Handle(PingNewResource request, CancellationToken cancellationToken)
    {
        throw new ServerException();
    }
}

public class PingResourceTimeoutHandler : IRequestHandler<PingResourceTimeout, Pong>
{
    private readonly TextWriter _writer;

    public PingResourceTimeoutHandler(TextWriter writer) => _writer = writer;

    public Task<Pong> Handle(PingResourceTimeout request, CancellationToken cancellationToken)
    {
        throw new TaskCanceledException();
    }
}

public class PingResourceTimeoutOverrideHandler : IRequestHandler<Overrides.PingResourceTimeout, Pong>
{
    private readonly TextWriter _writer;

    public PingResourceTimeoutOverrideHandler(TextWriter writer) => _writer = writer;

    public Task<Pong> Handle(Overrides.PingResourceTimeout request, CancellationToken cancellationToken)
    {
        throw new TaskCanceledException();
    }
}

public class PingProtectedResourceHandler : IRequestHandler<PingProtectedResource, Pong>
{
    private readonly TextWriter _writer;

    public PingProtectedResourceHandler(TextWriter writer) => _writer = writer;

    public Task<Pong> Handle(PingProtectedResource request, CancellationToken cancellationToken)
    {
        throw new ForbiddenException();
    }
}