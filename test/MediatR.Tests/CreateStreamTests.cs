using System.Threading;

namespace MediatR.Tests;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

public class CreateStreamTests
{

    public class Ping : IStreamRequest<Pong>
    {
        public string? Message { get; set; }
    }

    public class Pong
    {
        public string? Message { get; set; }
    }

    public class PingStreamHandler : IStreamRequestHandler<Ping, Pong>
    {
        public async IAsyncEnumerable<Pong> Handle(Ping request, [EnumeratorCancellation]CancellationToken cancellationToken)
        {
            yield return await Task.Run(() => new Pong { Message = request.Message + " Pang" });
        }
    }

    [Fact]
    public async Task Should_resolve_main_handler()
    {
        var container = TestContainer.Create(cfg =>
        {
            cfg.Scan(scanner =>
            {
                scanner.FromAssemblyOf<CreateStreamTests>()
                    .AddClasses(t => t.InNamespaceOf<Ping>().AssignableTo(typeof(IStreamRequestHandler<,>))).AsImplementedInterfaces();
            });
            cfg.AddTransient<IMediator, Mediator>();
        });

        var mediator = container.GetRequiredService<IMediator>();

        var response = mediator.CreateStream(new Ping { Message = "Ping" });
        int i = 0;
        await foreach (Pong result in response)
        {
            if (i == 0)
            {
                result.Message.ShouldBe("Ping Pang");
            }

            i++;
        }

        i.ShouldBe(1);
    }

    [Fact]
    public async Task Should_resolve_main_handler_via_dynamic_dispatch()
    {
        var container = TestContainer.Create(cfg =>
        {
            cfg.Scan(scanner =>
            {
                scanner.FromAssemblyOf<CreateStreamTests>()
                    .AddClasses(t => t.InNamespaceOf<Ping>().AssignableTo(typeof(IStreamRequestHandler<,>))).AsImplementedInterfaces();
            });
            cfg.AddTransient<IMediator, Mediator>();
        });

        var mediator = container.GetRequiredService<IMediator>();

        object request = new Ping { Message = "Ping" };
        var response = mediator.CreateStream(request);
        int i = 0;
        await foreach (Pong? result in response)
        {
            if (i == 0)
            {
                result!.Message.ShouldBe("Ping Pang");
            }

            i++;
        }

        i.ShouldBe(1);
    }

    [Fact]
    public async Task Should_resolve_main_handler_by_specific_interface()
    {
        var container = TestContainer.Create(cfg =>
        {
            cfg.Scan(scanner =>
            {
                scanner.FromAssemblyOf<CreateStreamTests>()
                    .AddClasses(t => t.InNamespaceOf<Ping>().AssignableTo(typeof(IStreamRequestHandler<,>))).AsImplementedInterfaces();
            });
            cfg.AddTransient<ISender, Mediator>();
        });

        var mediator = container.GetRequiredService<ISender>();
        var response = mediator.CreateStream(new Ping { Message = "Ping" });
        int i = 0;
        await foreach (Pong result in response)
        {
            if (i == 0)
            {
                result.Message.ShouldBe("Ping Pang");
            }

            i++;
        }

        i.ShouldBe(1);
    }

    [Fact]
    public void Should_raise_execption_on_null_request()
    {
        var container = TestContainer.Create(cfg =>
        {
            cfg.AddTransient<IMediator, Mediator>();
        });

        var mediator = container.GetRequiredService<IMediator>();

        Should.Throw<ArgumentNullException>(() => mediator.CreateStream((Ping) null!));
    }

    [Fact]
    public void Should_raise_execption_on_null_request_via_dynamic_dispatch()
    {
        var container = TestContainer.Create(cfg =>
        {
            cfg.AddTransient<IMediator, Mediator>();
        });

        var mediator = container.GetRequiredService<IMediator>();

        Should.Throw<ArgumentNullException>(() => mediator.CreateStream((object) null!));
    }
}