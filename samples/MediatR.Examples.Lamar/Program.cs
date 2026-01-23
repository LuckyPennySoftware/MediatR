using System;
using System.IO;
using System.Threading.Tasks;
using Deluxe.MediatR;
using Deluxe.MediatR.Pipeline;
using Lamar;

namespace MediatR.Examples.Lamar;

class Program
{
    static Task Main(string[] args)
    {
        var writer = new WrappingWriter(Console.Out);
        var mediator = BuildMediator(writer);

        return Runner.Run(mediator, writer, "Lamar");
    }

    private static IMediator BuildMediator(WrappingWriter writer)
    {
        var container = new Container(cfg =>
        {
            cfg.Scan(scanner =>
            {
                scanner.AssemblyContainingType<Ping>();
                scanner.ConnectImplementationsToTypesClosing(typeof(IRequestHandler<,>));
                scanner.ConnectImplementationsToTypesClosing(typeof(INotificationHandler<>));
                scanner.ConnectImplementationsToTypesClosing(typeof(IRequestExceptionAction<,>));
                scanner.ConnectImplementationsToTypesClosing(typeof(IRequestExceptionHandler<,,>));
            });

            //Pipeline
            cfg.For(typeof(IPipelineBehavior<,>)).Add(typeof(RequestExceptionProcessorBehavior<,>));
            cfg.For(typeof(IPipelineBehavior<,>)).Add(typeof(RequestExceptionActionProcessorBehavior<,>));
            cfg.For(typeof(IPipelineBehavior<,>)).Add(typeof(RequestPreProcessorBehavior<,>));
            cfg.For(typeof(IPipelineBehavior<,>)).Add(typeof(RequestPostProcessorBehavior<,>));
            cfg.For(typeof(IPipelineBehavior<,>)).Add(typeof(GenericPipelineBehavior<,>));
            cfg.For(typeof(IRequestPreProcessor<>)).Add(typeof(GenericRequestPreProcessor<>));
            cfg.For(typeof(IRequestPostProcessor<,>)).Add(typeof(GenericRequestPostProcessor<,>));
            cfg.For(typeof(IRequestPostProcessor<,>)).Add(typeof(ConstrainedRequestPostProcessor<,>));

            //Constrained notification handlers
            cfg.For(typeof(INotificationHandler<>)).Add(typeof(ConstrainedPingedHandler<>));

            // This is the default but let's be explicit. At most we should be container scoped.
            cfg.For<IMediator>().Use<Mediator>().Transient();

            cfg.For<TextWriter>().Use(writer);
        });


        var mediator = container.GetInstance<IMediator>();

        return mediator;
    }
}