namespace MediatR.DependencyInjectionTests.Contracts.Requests;

public record PrivateVoidPing : IRequest
{
    private class Handler : IRequestHandler<PrivateVoidPing>
    {
        public Task Handle(PrivateVoidPing request, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}