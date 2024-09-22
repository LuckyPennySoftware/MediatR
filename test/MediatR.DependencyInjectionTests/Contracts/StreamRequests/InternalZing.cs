namespace MediatR.DependencyInjectionTests.Contracts.StreamRequests;

internal record InternalZing : IStreamRequest<Zong>
{
    internal class Handler : IStreamRequestHandler<InternalZing, Zong>
    {
        public IAsyncEnumerable<Zong> Handle(InternalZing request, CancellationToken token) =>
            throw new NotImplementedException();
    }
}