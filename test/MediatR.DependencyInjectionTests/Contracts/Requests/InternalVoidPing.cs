namespace MediatR.DependencyInjectionTests.Contracts.Requests;

internal record InternalVoidPing : IRequest
{
    internal class Handler : IRequestHandler<InternalVoidPing>
    {
        public Task Handle(InternalVoidPing request, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}