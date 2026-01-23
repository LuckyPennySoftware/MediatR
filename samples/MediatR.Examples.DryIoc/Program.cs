using System;
using System.IO;
using System.Threading.Tasks;
using Deluxe.MediatR;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.Examples.DryIoc;

class Program
{
    static Task Main()
    {
        var writer = new WrappingWriter(Console.Out);
        var mediator = BuildMediator(writer);

        return Runner.Run(mediator, writer, "DryIoc");
    }

    private static IMediator BuildMediator(WrappingWriter writer)
    {
        var container = new Container();
        // Since Mediator has multiple constructors, consider adding rule to allow that
        // var container = new Container(rules => rules.With(FactoryMethod.ConstructorWithResolvableArguments))

        container.Use<TextWriter>(writer);

        //Pipeline works out of the box here

        container.RegisterMany(new[] { typeof(IMediator).GetAssembly(), typeof(Ping).GetAssembly() }, Registrator.Interfaces);
        //Without the container having FactoryMethod.ConstructorWithResolvableArguments commented above
        //You must select the desired constructor
        container.Register<IMediator, Mediator>(made: Made.Of(() => new Mediator(Arg.Of<IServiceProvider>())));

        var services = new ServiceCollection();

        var adapterContainer = container.WithDependencyInjectionAdapter(services);

        return adapterContainer.GetRequiredService<IMediator>();
    }
}