namespace MediatR.DependencyInjectionTests.Contracts.StreamRequests;

public record PublicZing : IStreamRequest<Zong>
{
    public class Handler : IStreamRequestHandler<PublicZing, Zong>
    {
        public IAsyncEnumerable<Zong> Handle(PublicZing request, CancellationToken token) =>
            throw new NotImplementedException();
    }
}