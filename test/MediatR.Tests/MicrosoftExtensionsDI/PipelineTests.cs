using System.Runtime.CompilerServices;

namespace MediatR.Extensions.Microsoft.DependencyInjection.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pipeline;
using Shouldly;
using Xunit;

public class PipelineTests
{
    public class OuterBehavior : IPipelineBehavior<Ping, Pong>
    {
        private readonly Logger _output;

        public OuterBehavior(Logger output)
        {
            _output = output;
        }

        public async Task<Pong> Handle(Ping request, RequestHandlerDelegate<Pong> next, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Outer before");
            var response = await next();
            _output.Messages.Add("Outer after");

            return response;
        }
    }

    public class InnerBehavior : IPipelineBehavior<Ping, Pong>
    {
        private readonly Logger _output;

        public InnerBehavior(Logger output)
        {
            _output = output;
        }

        public async Task<Pong> Handle(Ping request, RequestHandlerDelegate<Pong> next, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Inner before");
            var response = await next();
            _output.Messages.Add("Inner after");

            return response;
        }
    }
   
    public class OuterStreamBehavior : IStreamPipelineBehavior<Ping, Pong>
    {
        private readonly Logger _output;

        public OuterStreamBehavior(Logger output)
        {
            _output = output;
        }

        public async IAsyncEnumerable<Pong> Handle(Ping request, StreamHandlerDelegate<Pong> next, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _output.Messages.Add("Outer before");
            await foreach (var item in next().WithCancellation(cancellationToken))
            {
                yield return item;
            }
            _output.Messages.Add("Outer after");
        }
    }

    public class InnerStreamBehavior : IStreamPipelineBehavior<Ping, Pong>
    {
        private readonly Logger _output;

        public InnerStreamBehavior(Logger output)
        {
            _output = output;
        }

        public async IAsyncEnumerable<Pong> Handle(Ping request, StreamHandlerDelegate<Pong> next, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _output.Messages.Add("Inner before");
            await foreach (var item in next().WithCancellation(cancellationToken))
            {
                yield return item;
            }
            _output.Messages.Add("Inner after");
        }
    }

    public class InnerBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly Logger _output;

        public InnerBehavior(Logger output)
        {
            _output = output;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Inner generic before");
            var response = await next();
            _output.Messages.Add("Inner generic after");

            return response;
        }
    }

    public class OuterBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly Logger _output;

        public OuterBehavior(Logger output)
        {
            _output = output;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Outer generic before");
            var response = await next();
            _output.Messages.Add("Outer generic after");

            return response;
        }
    }

    public class ConstrainedBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
        where TResponse : Pong
    {
        private readonly Logger _output;

        public ConstrainedBehavior(Logger output)
        {
            _output = output;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Constrained before");
            var response = await next();
            _output.Messages.Add("Constrained after");

            return response;
        }
    }

    public class FirstPreProcessor<TRequest> : IRequestPreProcessor<TRequest> where TRequest : notnull
    {
        private readonly Logger _output;

        public FirstPreProcessor(Logger output)
        {
            _output = output;
        }
        public Task Process(TRequest request, CancellationToken cancellationToken)
        {
            _output.Messages.Add("First pre processor");
            return Task.FromResult(0);
        }
    }

    public class FirstConcretePreProcessor : IRequestPreProcessor<Ping>
    {
        private readonly Logger _output;

        public FirstConcretePreProcessor(Logger output)
        {
            _output = output;
        }
        public Task Process(Ping request, CancellationToken cancellationToken)
        {
            _output.Messages.Add("First concrete pre processor");
            return Task.FromResult(0);
        }
    }

    public class NextPreProcessor<TRequest> : IRequestPreProcessor<TRequest> where TRequest : notnull
    {
        private readonly Logger _output;

        public NextPreProcessor(Logger output)
        {
            _output = output;
        }
        public Task Process(TRequest request, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Next pre processor");
            return Task.FromResult(0);
        }
    }

    public class NextConcretePreProcessor : IRequestPreProcessor<Ping>
    {
        private readonly Logger _output;

        public NextConcretePreProcessor(Logger output)
        {
            _output = output;
        }
        public Task Process(Ping request, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Next concrete pre processor");
            return Task.FromResult(0);
        }
    }

    public class FirstPostProcessor<TRequest, TResponse> : IRequestPostProcessor<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly Logger _output;

        public FirstPostProcessor(Logger output)
        {
            _output = output;
        }
        public Task Process(TRequest request, TResponse response, CancellationToken cancellationToken)
        {
            _output.Messages.Add("First post processor");
            return Task.FromResult(0);
        }
    }

    public class FirstConcretePostProcessor : IRequestPostProcessor<Ping, Pong>
    {
        private readonly Logger _output;

        public FirstConcretePostProcessor(Logger output)
        {
            _output = output;
        }
        public Task Process(Ping request, Pong response, CancellationToken cancellationToken)
        {
            _output.Messages.Add("First concrete post processor");
            return Task.FromResult(0);
        }
    }

    public class NextPostProcessor<TRequest, TResponse> : IRequestPostProcessor<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly Logger _output;

        public NextPostProcessor(Logger output)
        {
            _output = output;
        }
        public Task Process(TRequest request, TResponse response, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Next post processor");
            return Task.FromResult(0);
        }
    }

    public class NextConcretePostProcessor : IRequestPostProcessor<Ping, Pong>
    {
        private readonly Logger _output;

        public NextConcretePostProcessor(Logger output)
        {
            _output = output;
        }
        public Task Process(Ping request, Pong response, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Next concrete post processor");
            return Task.FromResult(0);
        }
    }

    public class PingPongGenericExceptionAction : IRequestExceptionAction<Ping, Exception>
    {
        private readonly Logger _output;

        public PingPongGenericExceptionAction(Logger output) => _output = output;

        public Task Execute(Ping request, Exception exception, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Logging generic exception");

            return Task.CompletedTask;
        }
    }

    public class PingPongApplicationExceptionAction : IRequestExceptionAction<Ping, ApplicationException>
    {
        private readonly Logger _output;

        public PingPongApplicationExceptionAction(Logger output) => _output = output;

        public Task Execute(Ping request, ApplicationException exception, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Logging ApplicationException exception");

            return Task.CompletedTask;
        }
    }

    public class PingPongExceptionActionForType1 : IRequestExceptionAction<Ping, SystemException>
    {
        private readonly Logger _output;

        public PingPongExceptionActionForType1(Logger output) => _output = output;

        public Task Execute(Ping request, SystemException exception, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Logging exception 1");

            return Task.CompletedTask;
        }
    }

    public class PingPongExceptionActionForType2 : IRequestExceptionAction<Ping, SystemException>
    {
        private readonly Logger _output;

        public PingPongExceptionActionForType2(Logger output) => _output = output;

        public Task Execute(Ping request, SystemException exception, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Logging exception 2");

            return Task.CompletedTask;
        }
    }

    public class PingPongExceptionHandlerForType : IRequestExceptionHandler<Ping, Pong, ApplicationException>
    {
        public Task Handle(Ping request, ApplicationException exception, RequestExceptionHandlerState<Pong> state, CancellationToken cancellationToken)
        {
            state.SetHandled(new Pong { Message = exception.Message + " Handled by Specific Type" });

            return Task.CompletedTask;
        }
    }

    public class PingPongGenericExceptionHandler : IRequestExceptionHandler<Ping, Pong, Exception>
    {
        private readonly Logger _output;

        public PingPongGenericExceptionHandler(Logger output) => _output = output;

        public Task Handle(Ping request, Exception exception, RequestExceptionHandlerState<Pong> state, CancellationToken cancellationToken)
        {
            _output.Messages.Add(exception.Message + " Logged by Generic Type");

            return Task.CompletedTask;
        }
    }

    public class NotAnOpenBehavior : IPipelineBehavior<Ping, Pong>
    {
        public Task<Pong> Handle(Ping request, RequestHandlerDelegate<Pong> next, CancellationToken cancellationToken) => next();
    }

    public class ThrowingBehavior : IPipelineBehavior<Ping, Pong>
    {
        public Task<Pong> Handle(Ping request, RequestHandlerDelegate<Pong> next, CancellationToken cancellationToken) => throw new Exception(request.Message);
    }

    public class NotAnOpenStreamBehavior : IStreamPipelineBehavior<Ping, Pong>
    {
        public IAsyncEnumerable<Pong> Handle(Ping request, StreamHandlerDelegate<Pong> next, CancellationToken cancellationToken) => next();
    }

    public class OpenBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next();
    }

    public class OpenStreamBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next();
    }

    public class MultiOpenBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>, IStreamPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next();

        public IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next();
    }

    [Fact]
    public async Task Should_wrap_with_behavior()
    {
        var output = new Logger();
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(output);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Ping).Assembly);
            cfg.AddBehavior<IPipelineBehavior<Ping, Pong>, OuterBehavior>();
            cfg.AddBehavior<IPipelineBehavior<Ping, Pong>, InnerBehavior>();
        });
        var provider = services.BuildServiceProvider();

        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new Ping { Message = "Ping" });

        response.Message.ShouldBe("Ping Pong");

        output.Messages.ShouldBe(new[]
        {
            "Outer before",
            "Inner before",
            "Handler",
            "Inner after",
            "Outer after"
        });
    }

    [Fact]
    public async Task Should_wrap_generics_with_behavior()
    {
        var output = new Logger();
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(output);
        services.AddMediatR(cfg =>
        {
            // Call these registration methods multiple times to prove we don't register a service if it is already registered
            for (var i = 0; i < 3; i++)
            {
                cfg.AddOpenBehavior(typeof(OuterBehavior<,>));
                cfg.AddOpenBehavior(typeof(InnerBehavior<,>));
                cfg.RegisterServicesFromAssembly(typeof(Ping).Assembly);
            }
        });
        var provider = services.BuildServiceProvider();

        var mediator = provider.GetRequiredService<IMediator>();
            
        var response = await mediator.Send(new Ping { Message = "Ping" });

        response.Message.ShouldBe("Ping Pong");

        output.Messages.ShouldBe(new[]
        {
            "Outer generic before",
            "Inner generic before",
            "Handler",
            "Inner generic after",
            "Outer generic after",
        });
    }

    [Fact]
    public async Task Should_register_pre_and_post_processors()
    {
        var output = new Logger();
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(output);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Ping).Assembly);
            cfg.AddRequestPreProcessor<IRequestPreProcessor<Ping>, FirstConcretePreProcessor>();
            cfg.AddRequestPreProcessor<IRequestPreProcessor<Ping>, NextConcretePreProcessor>();
            cfg.AddOpenRequestPreProcessor(typeof(FirstPreProcessor<>));
            cfg.AddOpenRequestPreProcessor(typeof(NextPreProcessor<>));
            cfg.AddRequestPostProcessor<IRequestPostProcessor<Ping, Pong>, FirstConcretePostProcessor>();
            cfg.AddRequestPostProcessor<IRequestPostProcessor<Ping, Pong>, NextConcretePostProcessor>();
            cfg.AddOpenRequestPostProcessor(typeof(FirstPostProcessor<,>));
            cfg.AddOpenRequestPostProcessor(typeof(NextPostProcessor<,>));
        });
        var provider = services.BuildServiceProvider();

        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new Ping { Message = "Ping" });

        response.Message.ShouldBe("Ping Pong");

        output.Messages.ShouldBe(new[]
        {
            "First concrete pre processor",
            "Next concrete pre processor",
            "First pre processor",
            "Next pre processor",
            "Handler",
            "First concrete post processor",
            "Next concrete post processor",
            "First post processor",
            "Next post processor",
        });
    }

    [Fact]
    public async Task Should_pick_up_specific_exception_behaviors()
    {
        var output = new Logger();
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(output);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Ping).Assembly));
        var provider = services.BuildServiceProvider();

        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new Ping {Message = "Ping", ThrowAction = msg => throw new ApplicationException(msg.Message + " Thrown")});

        response.Message.ShouldBe("Ping Thrown Handled by Specific Type");
        output.Messages.ShouldNotContain("Logging ApplicationException exception");
    }

    [Fact]
    public void Should_pick_up_base_exception_behaviors()
    {
        var output = new Logger();
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(output);
        services.AddFakeLogging();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Ping).Assembly));
        var provider = services.BuildServiceProvider();

        var mediator = provider.GetRequiredService<IMediator>();

        Should.Throw<Exception>(async () => await mediator.Send(new Ping {Message = "Ping", ThrowAction = msg => throw new Exception(msg.Message + " Thrown")}));

        output.Messages.ShouldContain("Ping Thrown Logged by Generic Type");
        output.Messages.ShouldContain("Logging generic exception");
    }

    [Fact]
    public void Should_handle_exceptions_from_behaviors()
    {
        var output = new Logger();
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(output);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Ping).Assembly);
            cfg.AddBehavior<ThrowingBehavior>();
        });
        var provider = services.BuildServiceProvider();

        var mediator = provider.GetRequiredService<IMediator>();

        Should.Throw<Exception>(async () => await mediator.Send(new Ping {Message = "Ping"}));

        output.Messages.ShouldContain("Ping Logged by Generic Type");
        output.Messages.ShouldContain("Logging generic exception");
    }

    [Fact]
    public void Should_pick_up_exception_actions()
    {
        var output = new Logger();
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(output);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Ping).Assembly));
        var provider = services.BuildServiceProvider();

        var mediator = provider.GetRequiredService<IMediator>();

        Should.Throw<SystemException>(async () => await mediator.Send(new Ping {Message = "Ping", ThrowAction = msg => throw new SystemException(msg.Message + " Thrown")}));

        output.Messages.ShouldContain("Logging exception 1");
        output.Messages.ShouldContain("Logging exception 2");
    }

    [Fact]
    public async Task Should_handle_constrained_generics()
    {
        var output = new Logger();
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(output);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Ping).Assembly);
            cfg.AddOpenBehavior(typeof(OuterBehavior<,>));
            cfg.AddOpenBehavior(typeof(InnerBehavior<,>));
            cfg.AddOpenBehavior(typeof(ConstrainedBehavior<,>));
            cfg.AddRequestPreProcessor<IRequestPreProcessor<Ping>, FirstConcretePreProcessor>();
            cfg.AddRequestPreProcessor<IRequestPreProcessor<Ping>, NextConcretePreProcessor>();
            cfg.AddOpenRequestPreProcessor(typeof(FirstPreProcessor<>));
            cfg.AddOpenRequestPreProcessor(typeof(NextPreProcessor<>));
            cfg.AddRequestPostProcessor<IRequestPostProcessor<Ping, Pong>, FirstConcretePostProcessor>();
            cfg.AddRequestPostProcessor<IRequestPostProcessor<Ping, Pong>, NextConcretePostProcessor>();
            cfg.AddOpenRequestPostProcessor(typeof(FirstPostProcessor<,>));
            cfg.AddOpenRequestPostProcessor(typeof(NextPostProcessor<,>));
        });
        var provider = services.BuildServiceProvider();

        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new Ping { Message = "Ping" });

        response.Message.ShouldBe("Ping Pong");

        output.Messages.ShouldBe(new[]
        {
            "First concrete pre processor",
            "Next concrete pre processor",
            "First pre processor",
            "Next pre processor",
            "Outer generic before",
            "Inner generic before",
            "Constrained before",
            "Handler",
            "Constrained after",
            "Inner generic after",
            "Outer generic after",
            "First concrete post processor",
            "Next concrete post processor",
            "First post processor",
            "Next post processor"
        });

        output.Messages.Clear();

        var zingResponse = await mediator.Send(new Zing { Message = "Zing" });

        zingResponse.Message.ShouldBe("Zing Zong");

        output.Messages.ShouldBe(new[]
        {
            "First pre processor",
            "Next pre processor",
            "Outer generic before",
            "Inner generic before",
            "Handler",
            "Inner generic after",
            "Outer generic after",
            "First post processor",
            "Next post processor"
        });
    }

    [Fact]
    public void Should_throw_when_adding_non_open_behavior()
    {
        Should.Throw<InvalidOperationException>(() => new MediatRServiceConfiguration().AddOpenBehavior(typeof(NotAnOpenBehavior)));
    }

    [Fact]
    public void Should_throw_when_adding_non_open_stream_behavior()
    {
        Should.Throw<InvalidOperationException>(() => new MediatRServiceConfiguration().AddOpenBehavior(typeof(NotAnOpenStreamBehavior)));
    }

    [Fact]
    public void Should_throw_when_adding_random_generic_type_as_open_behavior()
    {
        Should.Throw<InvalidOperationException>(() => new MediatRServiceConfiguration().AddOpenBehavior(typeof(List<string>)));
    }

    [Fact]
    public void Should_handle_open_behavior_registration()
    {
        var cfg = new MediatRServiceConfiguration();
        cfg.AddOpenBehavior(typeof(OpenBehavior<,>));
        cfg.AddOpenStreamBehavior(typeof(OpenStreamBehavior<,>));

        cfg.BehaviorsToRegister.Count.ShouldBe(1);
        cfg.StreamBehaviorsToRegister.Count.ShouldBe(1);

        cfg.BehaviorsToRegister[0].ServiceType.ShouldBe(typeof(IPipelineBehavior<,>));
        cfg.BehaviorsToRegister[0].ImplementationType.ShouldBe(typeof(OpenBehavior<,>));
        cfg.BehaviorsToRegister[0].ImplementationFactory.ShouldBeNull();
        cfg.BehaviorsToRegister[0].ImplementationInstance.ShouldBeNull();
        cfg.BehaviorsToRegister[0].Lifetime.ShouldBe(ServiceLifetime.Transient);

        cfg.StreamBehaviorsToRegister[0].ServiceType.ShouldBe(typeof(IStreamPipelineBehavior<,>));
        cfg.StreamBehaviorsToRegister[0].ImplementationType.ShouldBe(typeof(OpenStreamBehavior<,>));
        cfg.StreamBehaviorsToRegister[0].ImplementationFactory.ShouldBeNull();
        cfg.StreamBehaviorsToRegister[0].ImplementationInstance.ShouldBeNull();
        cfg.StreamBehaviorsToRegister[0].Lifetime.ShouldBe(ServiceLifetime.Transient);

        var services = new ServiceCollection();

        cfg.RegisterServicesFromAssemblyContaining<Ping>();

        Should.NotThrow(() =>
        {
            services.AddMediatR(cfg);
            services.BuildServiceProvider();
        });
    }
    
    [Fact]
    public void Should_handle_inferred_behavior_registration()
    {
        var cfg = new MediatRServiceConfiguration();
        cfg.AddBehavior<InnerBehavior>();
        cfg.AddBehavior(typeof(OuterBehavior));

        cfg.BehaviorsToRegister.Count.ShouldBe(2);

        cfg.BehaviorsToRegister[0].ServiceType.ShouldBe(typeof(IPipelineBehavior<Ping, Pong>));
        cfg.BehaviorsToRegister[0].ImplementationType.ShouldBe(typeof(InnerBehavior));
        cfg.BehaviorsToRegister[0].ImplementationFactory.ShouldBeNull();
        cfg.BehaviorsToRegister[0].ImplementationInstance.ShouldBeNull();
        cfg.BehaviorsToRegister[0].Lifetime.ShouldBe(ServiceLifetime.Transient);
        cfg.BehaviorsToRegister[1].ServiceType.ShouldBe(typeof(IPipelineBehavior<Ping, Pong>));
        cfg.BehaviorsToRegister[1].ImplementationType.ShouldBe(typeof(OuterBehavior));
        cfg.BehaviorsToRegister[1].ImplementationFactory.ShouldBeNull();
        cfg.BehaviorsToRegister[1].ImplementationInstance.ShouldBeNull();
        cfg.BehaviorsToRegister[1].Lifetime.ShouldBe(ServiceLifetime.Transient);
        
        var services = new ServiceCollection();

        cfg.RegisterServicesFromAssemblyContaining<Ping>();

        Should.NotThrow(() =>
        {
            services.AddMediatR(cfg);
            services.BuildServiceProvider();
        });
    }

        
    [Fact]
    public void Should_handle_inferred_stream_behavior_registration()
    {
        var cfg = new MediatRServiceConfiguration();
        cfg.AddStreamBehavior<InnerStreamBehavior>();
        cfg.AddStreamBehavior(typeof(OuterStreamBehavior));

        cfg.StreamBehaviorsToRegister.Count.ShouldBe(2);

        cfg.StreamBehaviorsToRegister[0].ServiceType.ShouldBe(typeof(IStreamPipelineBehavior<Ping, Pong>));
        cfg.StreamBehaviorsToRegister[0].ImplementationType.ShouldBe(typeof(InnerStreamBehavior));
        cfg.StreamBehaviorsToRegister[0].ImplementationFactory.ShouldBeNull();
        cfg.StreamBehaviorsToRegister[0].ImplementationInstance.ShouldBeNull();
        cfg.StreamBehaviorsToRegister[0].Lifetime.ShouldBe(ServiceLifetime.Transient);
        cfg.StreamBehaviorsToRegister[1].ServiceType.ShouldBe(typeof(IStreamPipelineBehavior<Ping, Pong>));
        cfg.StreamBehaviorsToRegister[1].ImplementationType.ShouldBe(typeof(OuterStreamBehavior));
        cfg.StreamBehaviorsToRegister[1].ImplementationFactory.ShouldBeNull();
        cfg.StreamBehaviorsToRegister[1].ImplementationInstance.ShouldBeNull();
        cfg.StreamBehaviorsToRegister[1].Lifetime.ShouldBe(ServiceLifetime.Transient);
        
        var services = new ServiceCollection();

        cfg.RegisterServicesFromAssemblyContaining<Ping>();

        Should.NotThrow(() =>
        {
            services.AddMediatR(cfg);
            services.BuildServiceProvider();
        });
    }
    
    [Fact]
    public void Should_handle_inferred_pre_processor_registration()
    {
        var cfg = new MediatRServiceConfiguration();
        cfg.AddRequestPreProcessor<FirstConcretePreProcessor>();
        cfg.AddRequestPreProcessor(typeof(NextConcretePreProcessor));

        cfg.RequestPreProcessorsToRegister.Count.ShouldBe(2);

        cfg.RequestPreProcessorsToRegister[0].ServiceType.ShouldBe(typeof(IRequestPreProcessor<Ping>));
        cfg.RequestPreProcessorsToRegister[0].ImplementationType.ShouldBe(typeof(FirstConcretePreProcessor));
        cfg.RequestPreProcessorsToRegister[0].ImplementationFactory.ShouldBeNull();
        cfg.RequestPreProcessorsToRegister[0].ImplementationInstance.ShouldBeNull();
        cfg.RequestPreProcessorsToRegister[0].Lifetime.ShouldBe(ServiceLifetime.Transient);
        cfg.RequestPreProcessorsToRegister[1].ServiceType.ShouldBe(typeof(IRequestPreProcessor<Ping>));
        cfg.RequestPreProcessorsToRegister[1].ImplementationType.ShouldBe(typeof(NextConcretePreProcessor));
        cfg.RequestPreProcessorsToRegister[1].ImplementationFactory.ShouldBeNull();
        cfg.RequestPreProcessorsToRegister[1].ImplementationInstance.ShouldBeNull();
        cfg.RequestPreProcessorsToRegister[1].Lifetime.ShouldBe(ServiceLifetime.Transient);
        
        var services = new ServiceCollection();

        cfg.RegisterServicesFromAssemblyContaining<Ping>();

        Should.NotThrow(() =>
        {
            services.AddMediatR(cfg);
            services.BuildServiceProvider();
        });
    }
    
    [Fact]
    public void Should_handle_inferred_post_processor_registration()
    {
        var cfg = new MediatRServiceConfiguration();
        cfg.AddRequestPostProcessor<FirstConcretePostProcessor>();
        cfg.AddRequestPostProcessor(typeof(NextConcretePostProcessor));

        cfg.RequestPostProcessorsToRegister.Count.ShouldBe(2);

        cfg.RequestPostProcessorsToRegister[0].ServiceType.ShouldBe(typeof(IRequestPostProcessor<Ping, Pong>));
        cfg.RequestPostProcessorsToRegister[0].ImplementationType.ShouldBe(typeof(FirstConcretePostProcessor));
        cfg.RequestPostProcessorsToRegister[0].ImplementationFactory.ShouldBeNull();
        cfg.RequestPostProcessorsToRegister[0].ImplementationInstance.ShouldBeNull();
        cfg.RequestPostProcessorsToRegister[0].Lifetime.ShouldBe(ServiceLifetime.Transient);
        cfg.RequestPostProcessorsToRegister[1].ServiceType.ShouldBe(typeof(IRequestPostProcessor<Ping, Pong>));
        cfg.RequestPostProcessorsToRegister[1].ImplementationType.ShouldBe(typeof(NextConcretePostProcessor));
        cfg.RequestPostProcessorsToRegister[1].ImplementationFactory.ShouldBeNull();
        cfg.RequestPostProcessorsToRegister[1].ImplementationInstance.ShouldBeNull();
        cfg.RequestPostProcessorsToRegister[1].Lifetime.ShouldBe(ServiceLifetime.Transient);
        
        var services = new ServiceCollection();

        cfg.RegisterServicesFromAssemblyContaining<Ping>();

        Should.NotThrow(() =>
        {
            services.AddMediatR(cfg);
            services.BuildServiceProvider();
        });
    }
    
    [Fact]
    public void Should_handle_open_behaviors_registration_from_a_single_type()
    {
        var cfg = new MediatRServiceConfiguration();
        cfg.AddOpenBehavior(typeof(MultiOpenBehavior<,>), ServiceLifetime.Singleton);
        cfg.AddOpenStreamBehavior(typeof(MultiOpenBehavior<,>), ServiceLifetime.Singleton);

        cfg.BehaviorsToRegister.Count.ShouldBe(1);
        cfg.StreamBehaviorsToRegister.Count.ShouldBe(1);

        cfg.BehaviorsToRegister[0].ServiceType.ShouldBe(typeof(IPipelineBehavior<,>));
        cfg.BehaviorsToRegister[0].ImplementationType.ShouldBe(typeof(MultiOpenBehavior<,>));
        cfg.BehaviorsToRegister[0].ImplementationFactory.ShouldBeNull();
        cfg.BehaviorsToRegister[0].ImplementationInstance.ShouldBeNull();
        cfg.BehaviorsToRegister[0].Lifetime.ShouldBe(ServiceLifetime.Singleton);

        cfg.StreamBehaviorsToRegister[0].ServiceType.ShouldBe(typeof(IStreamPipelineBehavior<,>));
        cfg.StreamBehaviorsToRegister[0].ImplementationType.ShouldBe(typeof(MultiOpenBehavior<,>));
        cfg.StreamBehaviorsToRegister[0].ImplementationFactory.ShouldBeNull();
        cfg.StreamBehaviorsToRegister[0].ImplementationInstance.ShouldBeNull();
        cfg.StreamBehaviorsToRegister[0].Lifetime.ShouldBe(ServiceLifetime.Singleton);
        
        var services = new ServiceCollection();

        cfg.RegisterServicesFromAssemblyContaining<Ping>();

        Should.NotThrow(() =>
        {
            services.AddMediatR(cfg);
            services.BuildServiceProvider();
        });
    }

    [Fact]
    public void Should_auto_register_processors_when_configured_including_all_concrete_types()
    {
        var cfg = new MediatRServiceConfiguration
        {
            AutoRegisterRequestProcessors = true
        };

        var output = new Logger();
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(output);

        cfg.RegisterServicesFromAssemblyContaining<Ping>();

        services.AddMediatR(cfg);

        var provider = services.BuildServiceProvider();

        var preProcessors = provider.GetServices(typeof(IRequestPreProcessor<Ping>)).ToList();
        preProcessors.Count.ShouldBeGreaterThan(0);
        preProcessors.ShouldContain(p => p != null && p.GetType() == typeof(FirstConcretePreProcessor));
        preProcessors.ShouldContain(p => p != null && p.GetType() == typeof(NextConcretePreProcessor));

        var postProcessors = provider.GetServices(typeof(IRequestPostProcessor<Ping, Pong>)).ToList();
        postProcessors.Count.ShouldBeGreaterThan(0);
        postProcessors.ShouldContain(p => p != null && p.GetType() == typeof(FirstConcretePostProcessor));
        postProcessors.ShouldContain(p => p != null && p.GetType() == typeof(NextConcretePostProcessor));
    }


    public sealed record FooRequest : IRequest;
    
    public interface IBlogger<T>
    {
        IList<string> Messages { get; }
    }

    public class Blogger<T> : IBlogger<T>
    {
        private readonly Logger _logger;

        public Blogger(Logger logger)
        {
            _logger = logger;
        }

        public IList<string> Messages => _logger.Messages;
    }

    public sealed class FooRequestHandler : IRequestHandler<FooRequest> {
        public FooRequestHandler(IBlogger<FooRequestHandler> logger)
        {
            this.logger = logger;
        }

        readonly IBlogger<FooRequestHandler> logger;

        public Task Handle(FooRequest request, CancellationToken cancellationToken) {
            logger.Messages.Add("Invoked Handler");
            return Task.CompletedTask;
        }
    }

    sealed class ClosedBehavior : IPipelineBehavior<FooRequest, Unit> {
        public ClosedBehavior(IBlogger<ClosedBehavior> logger)
        {
            this.logger = logger;
        }

        readonly IBlogger<ClosedBehavior> logger;

        public Task<Unit> Handle(FooRequest request, RequestHandlerDelegate<Unit> next, CancellationToken cancellationToken) {
            logger.Messages.Add("Invoked Closed Behavior");
            return next();
        }
    }

    sealed class Open2Behavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull {
        public Open2Behavior(IBlogger<Open2Behavior<TRequest, TResponse>> logger) {
            this.logger = logger;
        }

        readonly IBlogger<Open2Behavior<TRequest, TResponse>> logger;

        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) {
            logger.Messages.Add("Invoked Open Behavior");
            return next();
        }
    }
    [Fact]
    public async Task Should_register_correctly()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<FooRequest>();
            cfg.AddBehavior<ClosedBehavior>();
            cfg.AddOpenBehavior(typeof(Open2Behavior<,>));
        });
        services.AddFakeLogging();
        var logger = new Logger();
        services.AddSingleton(logger);
        services.AddSingleton(new MediatR.Tests.PipelineTests.Logger());
        services.AddSingleton(new MediatR.Tests.StreamPipelineTests.Logger());
        services.AddSingleton(new MediatR.Tests.SendTests.Dependency());
        services.AddSingleton<System.IO.TextWriter>(new System.IO.StringWriter());
        services.AddTransient(typeof(IBlogger<>), typeof(Blogger<>));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true
        });

        var mediator = provider.GetRequiredService<IMediator>();
        var request = new FooRequest();
        await mediator.Send(request);
        
        logger.Messages.ShouldBe(new []
        {
            "Invoked Closed Behavior",
            "Invoked Open Behavior",
            "Invoked Handler",
        });
    }


    #region OpenBehaviorsForMultipleRegistration
    sealed class OpenBehaviorMultipleRegistration0<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public OpenBehaviorMultipleRegistration0(IBlogger<OpenBehaviorMultipleRegistration0<TRequest, TResponse>> logger)
        {
            this.logger = logger;
        }

        readonly IBlogger<OpenBehaviorMultipleRegistration0<TRequest, TResponse>> logger;

        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            logger.Messages.Add("Invoked OpenBehaviorMultipleRegistration0");
            return next();
        }
    }
    sealed class OpenBehaviorMultipleRegistration1<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public OpenBehaviorMultipleRegistration1(IBlogger<OpenBehaviorMultipleRegistration1<TRequest, TResponse>> logger)
        {
            this.logger = logger;
        }

        readonly IBlogger<OpenBehaviorMultipleRegistration1<TRequest, TResponse>> logger;

        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            logger.Messages.Add("Invoked OpenBehaviorMultipleRegistration1");
            return next();
        }
    }
    sealed class OpenBehaviorMultipleRegistration2<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public OpenBehaviorMultipleRegistration2(IBlogger<OpenBehaviorMultipleRegistration2<TRequest, TResponse>> logger)
        {
            this.logger = logger;
        }

        readonly IBlogger<OpenBehaviorMultipleRegistration2<TRequest, TResponse>> logger;

        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            logger.Messages.Add("Invoked OpenBehaviorMultipleRegistration2");
            return next();
        }
    }
    #endregion OpenBehaviorsForMultipleRegistration

    [Fact]
    public async Task Should_register_open_behaviors_correctly()
    {
        var behaviorTypeList = new List<Type>
        {
            typeof(OpenBehaviorMultipleRegistration0<,>),
            typeof(OpenBehaviorMultipleRegistration1<,>),
            typeof(OpenBehaviorMultipleRegistration2<,>)
        };
        var services = new ServiceCollection();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<FooRequest>();
            cfg.AddOpenBehaviors(behaviorTypeList);
        });
        services.AddFakeLogging();
        var logger = new Logger();
        services.AddSingleton(logger);
        services.AddSingleton(new MediatR.Tests.PipelineTests.Logger());
        services.AddSingleton(new MediatR.Tests.StreamPipelineTests.Logger());
        services.AddSingleton(new MediatR.Tests.SendTests.Dependency());
        services.AddSingleton<System.IO.TextWriter>(new System.IO.StringWriter());
        services.AddTransient(typeof(IBlogger<>), typeof(Blogger<>));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true
        });

        var mediator = provider.GetRequiredService<IMediator>();
        var request = new FooRequest();
        await mediator.Send(request);

        logger.Messages.ShouldBe(new[]
        {
            "Invoked OpenBehaviorMultipleRegistration0",
            "Invoked OpenBehaviorMultipleRegistration1",
            "Invoked OpenBehaviorMultipleRegistration2",
            "Invoked Handler",
        });
    }
}