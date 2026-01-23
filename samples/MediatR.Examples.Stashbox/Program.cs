using Stashbox;
using Stashbox.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using Deluxe.MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.Examples.Stashbox;

class Program
{
    static Task Main()
    {
        var writer = new WrappingWriter(Console.Out);
        var mediator = BuildMediator(writer);
        return Runner.Run(mediator, writer, "Stashbox", testStreams: true);
    }

    private static IMediator BuildMediator(WrappingWriter writer)
    {
        var container = new StashboxContainer()
            .RegisterInstance<TextWriter>(writer)
            .RegisterAssemblies(new[] { typeof(Mediator).Assembly, typeof(Ping).Assembly }, 
                serviceTypeSelector: Rules.ServiceRegistrationFilters.Interfaces, registerSelf: false);

        return container.GetRequiredService<IMediator>();
    }
}