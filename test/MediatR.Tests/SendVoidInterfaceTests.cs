using System.Threading;

namespace MediatR.Tests;

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
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
                scanner.FromAssemblyOf<PublishTests>()
                    .AddClasses(t => t.InNamespaceOf<Ping>()).AsImplementedInterfaces()
                    .AddClasses(t => t.AssignableTo(typeof(IRequestHandler<,>))).AsImplementedInterfaces()
                    .AddClasses(t => t.AssignableTo(typeof(IRequestHandler<>))).AsImplementedInterfaces();
            });
            cfg.AddSingleton<TextWriter>(writer);
            cfg.AddTransient<IMediator, Mediator>();
        });


        var mediator = container.GetRequiredService<IMediator>();

        await mediator.Send(new Ping { Message = "Ping" });

        builder.ToString().ShouldBe("Ping Pong");
    }
}