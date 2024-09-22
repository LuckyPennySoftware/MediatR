namespace MediatR.DependencyInjectionTests.Contracts.Requests;

public record PublicPing : IRequest<Pong>
{
    public class Handler : IRequestHandler<PublicPing, Pong>
    {
        public Task<Pong> Handle(PublicPing request, CancellationToken cancellationToken) =>
            Task.FromResult(new Pong());
    }
}