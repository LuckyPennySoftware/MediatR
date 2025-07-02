using System.Threading;

namespace MediatR.Tests;

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using Lamar;
using Xunit;

public class SendVoidInterfaceTests
{
    public class Ping : IRequest
    {
        public string? Message { get; set; }
    }

    public class PingHandler : IRequestHandler<Ping>
    {
        private readonly TextWriter _writer;

        public PingHandler(TextWriter writer) => _writer = writer;

        public Task Handle(Ping request, CancellationToken cancellationToken)
            => _writer.WriteAsync(request.Message + " Pong");
    }

    [Fact]
    public async Task Should_resolve_main_void_handler()
    {
        var builder = new StringBuilder();
        var writer = new StringWriter(builder);

        var container = TestContainer.Create(cfg =>
        {
            cfg.Scan(scanner =>
            {
                scanner.AssemblyContainingType(typeof(PublishTests));
                scanner.IncludeNamespaceContainingType<Ping>();
                scanner.WithDefaultConventions();
                scanner.AddAllTypesOf(typeof (IRequestHandler<,>));
                scanner.AddAllTypesOf(typeof (IRequestHandler<>));
            });
            cfg.For<TextWriter>().Use(writer);
            cfg.For<IMediator>().Use<Mediator>();
        });


        var mediator = container.GetInstance<IMediator>();

        await mediator.Send(new Ping { Message = "Ping" });

        builder.ToString().ShouldBe("Ping Pong");
    }
}