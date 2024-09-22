namespace MediatR.DependencyInjectionTests.Contracts.Requests;

internal record InternalPing : IRequest<Pong>
{
    internal class Handler : IRequestHandler<InternalPing, Pong>
    {
        public Task<Pong> Handle(InternalPing request, CancellationToken cancellationToken) =>
            Task.FromResult(new Pong());
    }
}