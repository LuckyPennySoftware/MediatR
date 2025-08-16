namespace MediatR.DependencyInjectionTests.Contracts.StreamRequests;

internal record PrivateZing : IStreamRequest<Zong>
{
    private class Handler : IStreamRequestHandler<PrivateZing, Zong>
    {
        public IAsyncEnumerable<Zong> Handle(PrivateZing request, CancellationToken token) =>
            throw new NotImplementedException();
    }
}