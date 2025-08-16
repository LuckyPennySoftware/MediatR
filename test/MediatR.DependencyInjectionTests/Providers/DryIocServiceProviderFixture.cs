using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using MediatR.DependencyInjectionTests.Abstractions;

namespace MediatR.DependencyInjectionTests.Providers;

public class DryIocServiceProviderFixture : BaseServiceProviderFixture
{
    public override IServiceProvider Provider
    {
        get
        {
            var container = new Container();
            container.RegisterMany(new[] { typeof(IMediator).GetAssembly(), typeof(Pong).GetAssembly() }, Registrator.Interfaces);
            container.Register<IMediator, Mediator>(made: Made.Of(() => new Mediator(Arg.Of<IServiceProvider>())));
            return container.BuildServiceProvider();
        }
    }
}