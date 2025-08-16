namespace MediatR.DependencyInjectionTests.Contracts.Requests;

public record PrivatePing : IRequest<Pong>
{
    private class Handler : IRequestHandler<PrivatePing, Pong>
    {
        public Task<Pong> Handle(PrivatePing request, CancellationToken cancellationToken) =>
            Task.FromResult(new Pong());
    }
}