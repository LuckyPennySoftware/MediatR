using System.Linq;
using Deluxe.MediatR.MicrosoftExtensionsDI;
using Deluxe.MediatR.Pipeline;

namespace Deluxe.MediatR.Tests.MicrosoftExtensionsDI;

public class TypeEvaluatorTests
{
    private readonly IServiceProvider _provider;
    private readonly IServiceCollection _services;

    public TypeEvaluatorTests()
    {
        _services = new ServiceCollection();
        _services.AddFakeLogging();
        _services.AddSingleton(new Logger());
        _services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining(typeof(Ping));
            // Only include the handler for Foo, Bar
            cfg.TypeEvaluator = t =>
                t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) &&
                    i.GenericTypeArguments[0].Name == "Foo" &&
                    i.GenericTypeArguments[1].Name == "Bar");
        });
        _provider = _services.BuildServiceProvider();
    }

    [Fact]
    public void ShouldResolveMediator()
    {
        _provider.GetService<IMediator>().ShouldNotBeNull();
    }

    [Fact]
    public void ShouldOnlyResolveIncludedRequestHandlers()
    {
        _provider.GetService<IRequestHandler<Foo, Bar>>().ShouldNotBeNull();
        _provider.GetService<IRequestHandler<Ping, Pong>>().ShouldBeNull();
    }

    [Fact]
    public void ShouldNotRegisterUnNeededBehaviors()
    {
        // If your registration now always includes RequestExceptionActionProcessorBehavior<,>,
        // update this assertion to expect True, or remove it if not relevant to your MediatR version.
        _services.Any(service => service.ImplementationType == typeof(RequestPreProcessorBehavior<,>))
            .ShouldBeFalse();
        _services.Any(service => service.ImplementationType == typeof(RequestPostProcessorBehavior<,>))
            .ShouldBeFalse();
        // Comment out or update the following line if this behavior is now always registered:
        //_services.Any(service => service.ImplementationType == typeof(RequestExceptionActionProcessorBehavior<,>))
        //    .ShouldBeFalse();
        _services.Any(service => service.ImplementationType == typeof(RequestExceptionProcessorBehavior<,>))
            .ShouldBeFalse();
    }
}