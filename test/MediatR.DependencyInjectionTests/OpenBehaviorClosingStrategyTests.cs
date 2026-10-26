using Autofac;
using Autofac.Extensions.DependencyInjection;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using LightInject;
using LightInject.Microsoft.DependencyInjection;
using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Stashbox;

namespace MediatR.DependencyInjectionTests;

// Open behaviors the container can't close positionally, across every supported container (#1180)
public class OpenBehaviorClosingStrategyTests
{
    private static readonly Dictionary<string, Func<IServiceCollection, IServiceProvider>> Containers = new()
    {
        ["Microsoft"] = services => services.BuildServiceProvider(),
        ["Autofac"] = services =>
        {
            var builder = new ContainerBuilder();
            builder.Populate(services);
            return new AutofacServiceProvider(builder.Build());
        },
        ["DryIoc"] = services =>
        {
            var container = new DryIoc.Container(Rules.MicrosoftDependencyInjectionRules);
            container.WithDependencyInjectionAdapter(services);
            return container.BuildServiceProvider();
        },
        ["Lamar"] = services => new Lamar.Container(services).ServiceProvider,
        ["LightInject"] = services =>
            new ServiceContainer(new ContainerOptions { EnableMicrosoftCompatibility = true }).CreateServiceProvider(services),
        ["StashBox"] = services =>
        {
            var container = new StashboxContainer();
            services.UseStashbox(container);
            return services.BuildServiceProvider();
        },
    };

    // Containers that close open generics by matching the service type rather than positionally
    private static readonly string[] UnifyingContainers = ["DryIoc", "LightInject"];

    public static IEnumerable<object[]> AllContainers => Containers.Keys.Select(name => new object[] { name });

    public static IEnumerable<object[]> UnifyingContainerNames => UnifyingContainers.Select(name => new object[] { name });

    public class Log
    {
        public List<string> Messages { get; } = new();
    }

    // Handlers log through the request rather than injecting Log, since the shared container fixtures
    // scan this assembly and validate every handler without registering Log
    public class ClosingListPing(Log log) : IRequest<List<Pong>>
    {
        public Log Log { get; } = log;
    }

    public class ClosingListPingHandler : IRequestHandler<ClosingListPing, List<Pong>>
    {
        public Task<List<Pong>> Handle(ClosingListPing request, CancellationToken cancellationToken)
        {
            request.Log.Messages.Add("Handler");
            return Task.FromResult(new List<Pong> { new() });
        }
    }

    // Generic request definitions are skipped by assembly scanning, so a closed GenericListPing<Pong>
    // stands in for a request type from an assembly that isn't scanned
    public class GenericListPing<T> : IRequest<List<T>> { }

    public interface IClosingDbContext { }
    public class ClosingDbContext : IClosingDbContext { }
    public interface IClosingTxCommand<TDbContext> : IRequest where TDbContext : IClosingDbContext { }

    public class ClosingTxCommand(Log log) : IClosingTxCommand<ClosingDbContext>
    {
        public Log Log { get; } = log;
    }

    public class ClosingTxCommandHandler : IRequestHandler<ClosingTxCommand>
    {
        public Task Handle(ClosingTxCommand request, CancellationToken cancellationToken)
        {
            request.Log.Messages.Add("Handler");
            return Task.CompletedTask;
        }
    }

    public class NestedListBehavior<TRequest, TItem>(Log log) : IPipelineBehavior<TRequest, List<TItem>>
        where TRequest : IRequest<List<TItem>>
    {
        public async Task<List<TItem>> Handle(TRequest request, RequestHandlerDelegate<List<TItem>> next, CancellationToken cancellationToken)
        {
            log.Messages.Add("Nested behavior");
            return await next();
        }
    }

    public class ContextTxBehavior<TRequest, TResponse, TDbContext>(Log log) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IClosingTxCommand<TDbContext>
        where TDbContext : IClosingDbContext
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            log.Messages.Add($"Tx behavior {typeof(TDbContext).Name}");
            return await next();
        }
    }

    public class NestedListPostProcessor<TRequest, TItem>(Log log) : IRequestPostProcessor<TRequest, List<TItem>>
        where TRequest : IRequest<List<TItem>>
    {
        public Task Process(TRequest request, List<TItem> response, CancellationToken cancellationToken)
        {
            log.Messages.Add("Nested post-processor");
            return Task.CompletedTask;
        }
    }

    public class GenericTuplePing<TLeft, TRight> : IRequest<Tuple<TLeft, TRight>> { }
    public class GenericVoidPing<T> : IRequest { }

    // Every parameter appears in the service interface, so a unifying container can close these
    // even though their arity differs from IPipelineBehavior<TRequest, TResponse>
    public class TupleBehavior<TRequest, TLeft, TRight>(Log log) : IPipelineBehavior<TRequest, Tuple<TLeft, TRight>>
        where TRequest : IRequest<Tuple<TLeft, TRight>>
    {
        public async Task<Tuple<TLeft, TRight>> Handle(TRequest request, RequestHandlerDelegate<Tuple<TLeft, TRight>> next, CancellationToken cancellationToken)
        {
            log.Messages.Add("Tuple behavior");
            return await next();
        }
    }

    public class UnitBehavior<TRequest>(Log log) : IPipelineBehavior<TRequest, Unit>
        where TRequest : IRequest
    {
        public async Task<Unit> Handle(TRequest request, RequestHandlerDelegate<Unit> next, CancellationToken cancellationToken)
        {
            log.Messages.Add("Unit behavior");
            return await next();
        }
    }

    private static IServiceCollection CreateServices(Type openBehaviorType, OpenBehaviorClosingStrategy? strategy = null) =>
        CreateServices(cfg => cfg.AddOpenBehavior(openBehaviorType), strategy);

    private static IServiceCollection CreateServices(Action<MediatRServiceConfiguration> configure, OpenBehaviorClosingStrategy? strategy = null)
    {
        IServiceCollection services = new ServiceCollection();
        services.AddFakeLogging();
        services.AddSingleton<Log>();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<OpenBehaviorClosingStrategyTests>();
            if (strategy != null) cfg.OpenBehaviorClosingStrategy = strategy.Value;
            configure(cfg);
        });
        return services;
    }

    [Theory]
    [MemberData(nameof(AllContainers))]
    public async Task Should_apply_nested_generic_behavior_once_for_scanned_requests_by_default(string container)
    {
        var provider = Containers[container](CreateServices(typeof(NestedListBehavior<,>)));

        await provider.GetRequiredService<IMediator>().Send(new ClosingListPing(provider.GetRequiredService<Log>()));

        provider.GetRequiredService<Log>().Messages.ShouldBe(["Nested behavior", "Handler"]);
    }

    [Theory]
    [MemberData(nameof(AllContainers))]
    public void Should_not_apply_nested_generic_behavior_to_unscanned_requests_by_default(string container)
    {
        var provider = Containers[container](CreateServices(typeof(NestedListBehavior<,>)));

        provider.GetServices<IPipelineBehavior<GenericListPing<Pong>, List<Pong>>>()
            .OfType<NestedListBehavior<GenericListPing<Pong>, Pong>>()
            .ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(AllContainers))]
    public async Task Should_apply_behavior_with_extra_type_parameter_once_by_default(string container)
    {
        var provider = Containers[container](CreateServices(typeof(ContextTxBehavior<,,>)));

        await provider.GetRequiredService<IMediator>().Send(new ClosingTxCommand(provider.GetRequiredService<Log>()));

        provider.GetRequiredService<Log>().Messages.ShouldBe(["Tx behavior ClosingDbContext", "Handler"]);
    }

    [Fact]
    public void Should_register_only_the_open_generic_when_closing_in_container()
    {
        var services = CreateServices(typeof(NestedListBehavior<,>), OpenBehaviorClosingStrategy.CloseInContainer);

        services.Where(d => d.ImplementationType is { IsGenericType: true } t && t.GetGenericTypeDefinition() == typeof(NestedListBehavior<,>))
            .ShouldHaveSingleItem()
            .ServiceType.ShouldBe(typeof(IPipelineBehavior<,>));
    }

    [Theory]
    [MemberData(nameof(UnifyingContainerNames))]
    public async Task Should_apply_nested_generic_behavior_once_for_scanned_requests_when_closing_in_container(string container)
    {
        var provider = Containers[container](CreateServices(typeof(NestedListBehavior<,>), OpenBehaviorClosingStrategy.CloseInContainer));

        await provider.GetRequiredService<IMediator>().Send(new ClosingListPing(provider.GetRequiredService<Log>()));

        provider.GetRequiredService<Log>().Messages.ShouldBe(["Nested behavior", "Handler"]);
    }

    [Theory]
    [MemberData(nameof(UnifyingContainerNames))]
    public void Should_apply_nested_generic_behavior_to_unscanned_requests_when_closing_in_container(string container)
    {
        var provider = Containers[container](CreateServices(typeof(NestedListBehavior<,>), OpenBehaviorClosingStrategy.CloseInContainer));

        provider.GetServices<IPipelineBehavior<GenericListPing<Pong>, List<Pong>>>()
            .OfType<NestedListBehavior<GenericListPing<Pong>, Pong>>()
            .ShouldHaveSingleItem();
    }

    [Theory]
    [MemberData(nameof(AllContainers))]
    public async Task Should_still_close_behavior_with_extra_type_parameter_for_scanned_requests_when_closing_in_container(string container)
    {
        var provider = Containers[container](CreateServices(typeof(ContextTxBehavior<,,>), OpenBehaviorClosingStrategy.CloseInContainer));

        await provider.GetRequiredService<IMediator>().Send(new ClosingTxCommand(provider.GetRequiredService<Log>()));

        provider.GetRequiredService<Log>().Messages.ShouldBe(["Tx behavior ClosingDbContext", "Handler"]);
    }

    [Theory]
    [MemberData(nameof(AllContainers))]
    public async Task Should_apply_auto_registered_nested_generic_processor_once_when_also_added_explicitly(string container)
    {
        var provider = Containers[container](CreateServices(cfg =>
        {
            cfg.AutoRegisterRequestProcessors = true;
            cfg.AddOpenRequestPostProcessor(typeof(NestedListPostProcessor<,>));
        }));

        await provider.GetRequiredService<IMediator>().Send(new ClosingListPing(provider.GetRequiredService<Log>()));

        provider.GetRequiredService<Log>().Messages.ShouldBe(["Handler", "Nested post-processor"]);
    }

    [Theory]
    [InlineData(typeof(TupleBehavior<,,>))]
    [InlineData(typeof(UnitBehavior<>))]
    public void Should_register_only_the_open_generic_when_closing_in_container_with_different_arity(Type openBehaviorType)
    {
        var services = CreateServices(openBehaviorType, OpenBehaviorClosingStrategy.CloseInContainer);

        services.Where(d => d.ImplementationType is { IsGenericType: true } t && t.GetGenericTypeDefinition() == openBehaviorType)
            .ShouldHaveSingleItem()
            .ServiceType.ShouldBe(typeof(IPipelineBehavior<,>));
    }

    [Theory]
    [MemberData(nameof(UnifyingContainerNames))]
    public void Should_apply_higher_arity_behavior_to_unscanned_requests_when_closing_in_container(string container)
    {
        var provider = Containers[container](CreateServices(typeof(TupleBehavior<,,>), OpenBehaviorClosingStrategy.CloseInContainer));

        provider.GetServices<IPipelineBehavior<GenericTuplePing<Pong, string>, Tuple<Pong, string>>>()
            .OfType<TupleBehavior<GenericTuplePing<Pong, string>, Pong, string>>()
            .ShouldHaveSingleItem();
    }

    [Theory]
    [MemberData(nameof(UnifyingContainerNames))]
    public void Should_apply_lower_arity_behavior_to_unscanned_requests_when_closing_in_container(string container)
    {
        var provider = Containers[container](CreateServices(typeof(UnitBehavior<>), OpenBehaviorClosingStrategy.CloseInContainer));

        provider.GetServices<IPipelineBehavior<GenericVoidPing<Pong>, Unit>>()
            .OfType<UnitBehavior<GenericVoidPing<Pong>>>()
            .ShouldHaveSingleItem();
    }
}
