using System.Threading;

namespace MediatR.Tests.Pipeline;

using System.Threading.Tasks;
using MediatR.Pipeline;
using Shouldly;
using Xunit;

public class RequestPostProcessorTests
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

    public class PingPongPostProcessor : IRequestPostProcessor<Ping, Pong>
    {
        public Task Process(Ping request, Pong response, CancellationToken cancellationToken)
        {
            response.Message = response.Message + " " + request.Message;

            return Task.FromResult(0);
        }
    }

    [Fact]
    public async Task Should_run_postprocessors()
    {
        var container = TestContainer.Create(cfg =>
        {
            cfg.Scan(scanner =>
            {
                scanner.FromAssemblyOf<PublishTests>()
                    .AddClasses(t => t.InNamespaceOf<Ping>()).AsImplementedInterfaces();
            });
            cfg.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestPostProcessorBehavior<,>));
            cfg.AddTransient<IMediator, Mediator>();
        });

        var mediator = container.GetRequiredService<IMediator>();

        var response = await mediator.Send(new Ping { Message = "Ping" });

        response.Message.ShouldBe("Ping Pong Ping");
    }

}