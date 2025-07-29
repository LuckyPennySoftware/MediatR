using System.Threading;

namespace MediatR.Tests.Pipeline;

using System.Threading.Tasks;
using MediatR.Pipeline;
using Shouldly;
using Xunit;

public class RequestPreProcessorTests
{
    public class Ping : IRequest<Pong>
    {
        public string? Message { get; set; }
    }

    public class Pong
    {
        public string? Message { get; set; }
    }

    public class PingHandler : IRequestHandler<Ping, Pong>
    {
        public Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Pong { Message = request.Message + " Pong" });
        }
    }

    public class PingPreProcessor : IRequestPreProcessor<Ping>
    {
        public Task Process(Ping request, CancellationToken cancellationToken)
        {
            request.Message = request.Message + " Ping";

            return Task.FromResult(0);
        }
    }

    [Fact]
    public async Task Should_run_preprocessors()
    {
        var container = TestContainer.Create(cfg =>
        {
            cfg.Scan(scanner =>
            {
                scanner.FromAssemblyOf<PublishTests>()
                    .AddClasses(t => t.InNamespaceOf<Ping>()).AsImplementedInterfaces();
            });
            cfg.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestPreProcessorBehavior<,>));
            cfg.AddTransient<IMediator, Mediator>();
        });

        var mediator = container.GetRequiredService<IMediator>();

        var response = await mediator.Send(new Ping { Message = "Ping" });

        response.Message.ShouldBe("Ping Ping Pong");
    }

}