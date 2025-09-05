namespace MediatR.DependencyInjectionTests.Contracts.Requests;

public record PublicVoidPing : IRequest
{
    public class Handler : IRequestHandler<PublicVoidPing>
    {
        public Task Handle(PublicVoidPing request, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}