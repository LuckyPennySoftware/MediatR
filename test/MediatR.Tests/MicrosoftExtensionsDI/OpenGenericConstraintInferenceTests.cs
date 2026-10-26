using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.Extensions.Microsoft.DependencyInjection.Tests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pipeline;
using Shouldly;
using Xunit;
using static PipelineTests;

// Stream behaviors and request pre/post processors with type parameters inferred from constraints (#1180).
// Pipeline behavior coverage lives in PipelineTests.
public class OpenGenericConstraintInferenceTests
{
    public interface ITxStreamRequest<TDbContext> : IStreamRequest<Pong> where TDbContext : ITxDbContext { }

    public class TxStreamRequest : ITxStreamRequest<TxAppDbContext> { }

    public class TxStreamRequestHandler : IStreamRequestHandler<TxStreamRequest, Pong>
    {
        private readonly Logger _output;
        public TxStreamRequestHandler(Logger output) => _output = output;

        public async IAsyncEnumerable<Pong> Handle(TxStreamRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _output.Messages.Add("Handler");
            yield return await Task.FromResult(new Pong { Message = "Pong" });
        }
    }

    public class ContextStreamBehavior<TRequest, TResponse, TDbContext> : IStreamPipelineBehavior<TRequest, TResponse>
        where TRequest : ITxStreamRequest<TDbContext>
        where TDbContext : ITxDbContext
    {
        private readonly Logger _output;
        public ContextStreamBehavior(Logger output) => _output = output;

        public async IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _output.Messages.Add($"Stream before {typeof(TDbContext).Name}");
            await foreach (var item in next().WithCancellation(cancellationToken))
            {
                yield return item;
            }
            _output.Messages.Add($"Stream after {typeof(TDbContext).Name}");
        }
    }

    public class UninferrableStreamBehavior<TRequest, TResponse, TExtra> : IStreamPipelineBehavior<TRequest, TResponse>
        where TRequest : IStreamRequest<TResponse>
        where TExtra : new()
    {
        public IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next();
    }

    public class ContextPreProcessor<TRequest, TDbContext> : IRequestPreProcessor<TRequest>
        where TRequest : IInvariantTxCommand<TDbContext>
        where TDbContext : ITxDbContext
    {
        private readonly Logger _output;
        public ContextPreProcessor(Logger output) => _output = output;

        public Task Process(TRequest request, CancellationToken cancellationToken)
        {
            _output.Messages.Add($"Pre {typeof(TDbContext).Name}");
            return Task.CompletedTask;
        }
    }

    public class ContextPostProcessor<TRequest, TResponse, TDbContext> : IRequestPostProcessor<TRequest, TResponse>
        where TRequest : IInvariantTxCommand<TDbContext>
        where TDbContext : ITxDbContext
    {
        private readonly Logger _output;
        public ContextPostProcessor(Logger output) => _output = output;

        public Task Process(TRequest request, TResponse response, CancellationToken cancellationToken)
        {
            _output.Messages.Add($"Post {typeof(TDbContext).Name}");
            return Task.CompletedTask;
        }
    }

    private static (IMediator Mediator, Logger Output) BuildMediator(Action<MediatRServiceConfiguration> configure)
    {
        var output = new Logger();
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(output);
        services.AddFakeLogging();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(OpenGenericConstraintInferenceTests).Assembly);
            configure(cfg);
        });
        var provider = services.BuildServiceProvider();
        return (provider.GetRequiredService<IMediator>(), output);
    }

    [Fact]
    public async Task Should_apply_open_stream_behavior_with_type_parameter_bound_from_request_constraint()
    {
        var (mediator, output) = BuildMediator(cfg => cfg.AddOpenStreamBehavior(typeof(ContextStreamBehavior<,,>)));

        await foreach (var _ in mediator.CreateStream(new TxStreamRequest())) { }

        output.Messages.ShouldBe(new[] { "Stream before TxAppDbContext", "Handler", "Stream after TxAppDbContext" });
    }

    [Fact]
    public void Should_throw_when_open_stream_behavior_type_parameter_cannot_be_inferred()
    {
        var ex = Should.Throw<InvalidOperationException>(() =>
            BuildMediator(cfg => cfg.AddOpenStreamBehavior(typeof(UninferrableStreamBehavior<,,>))));

        ex.Message.ShouldContain("TExtra");
    }

    [Fact]
    public async Task Should_apply_open_pre_processor_with_type_parameter_bound_from_request_constraint()
    {
        var (mediator, output) = BuildMediator(cfg => cfg.AddOpenRequestPreProcessor(typeof(ContextPreProcessor<,>)));

        await mediator.Send(new ContextTxCommand());

        output.Messages.ShouldBe(new[] { "Pre TxAppDbContext", "Handler" });
    }

    [Fact]
    public async Task Should_apply_open_post_processor_with_type_parameter_bound_from_request_constraint()
    {
        var (mediator, output) = BuildMediator(cfg => cfg.AddOpenRequestPostProcessor(typeof(ContextPostProcessor<,,>)));

        await mediator.Send(new ContextTxCommand());

        output.Messages.ShouldBe(new[] { "Handler", "Post TxAppDbContext" });
    }

    [Fact]
    public async Task Should_not_apply_open_pre_processor_with_extra_type_parameter_to_unrelated_requests()
    {
        var (mediator, output) = BuildMediator(cfg => cfg.AddOpenRequestPreProcessor(typeof(ContextPreProcessor<,>)));

        await mediator.Send(new CovariantTxCommand());

        output.Messages.ShouldBe(new[] { "Handler" });
    }

    // Review follow-ups: array arguments, multiple constraint links, variance on fixed arguments,
    // and implementations that close the service interface more than once

    public interface IArrayMarker<T> { }
    public class ArrayCommand : IRequest, IArrayMarker<int[]> { }

    public class ArrayCommandHandler : IRequestHandler<ArrayCommand>
    {
        private readonly Logger _output;
        public ArrayCommandHandler(Logger output) => _output = output;
        public Task Handle(ArrayCommand request, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Handler");
            return Task.CompletedTask;
        }
    }

    public class ArrayElementBehavior<TRequest, TResponse, TItem> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IArrayMarker<TItem[]>
    {
        private readonly Logger _output;
        public ArrayElementBehavior(Logger output) => _output = output;
        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _output.Messages.Add($"Array behavior {typeof(TItem).Name}");
            return next();
        }
    }

    public class BaseThing { }
    public class DerivedThing : BaseThing { }
    public interface ICovariantLink<out T> { }
    public interface IInvariantLink<T> { }

    public class LinkResponse : IInvariantLink<BaseThing> { }
    public class LinkRequest : IRequest<LinkResponse>, ICovariantLink<DerivedThing> { }

    public class LinkRequestHandler : IRequestHandler<LinkRequest, LinkResponse>
    {
        private readonly Logger _output;
        public LinkRequestHandler(Logger output) => _output = output;
        public Task<LinkResponse> Handle(LinkRequest request, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Handler");
            return Task.FromResult(new LinkResponse());
        }
    }

    // The request's constraint is tried first and infers DerivedThing, which the response's invariant
    // constraint rejects; only the response's constraint infers BaseThing, which satisfies both
    public class TwoLinkBehavior<TRequest, TResponse, TThing> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ICovariantLink<TThing>
        where TResponse : IInvariantLink<TThing>
    {
        private readonly Logger _output;
        public TwoLinkBehavior(Logger output) => _output = output;
        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _output.Messages.Add($"Link behavior {typeof(TThing).Name}");
            return next();
        }
    }

    public interface IVariantTagMarker<out TContext, TTag> { }
    public class VariantTagCommand : IRequest, IVariantTagMarker<TxAppDbContext, int> { }

    public class VariantTagCommandHandler : IRequestHandler<VariantTagCommand>
    {
        private readonly Logger _output;
        public VariantTagCommandHandler(Logger output) => _output = output;
        public Task Handle(VariantTagCommand request, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Handler");
            return Task.CompletedTask;
        }
    }

    public class VariantTagBehavior<TRequest, TResponse, TTag> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IVariantTagMarker<ITxDbContext, TTag>
    {
        private readonly Logger _output;
        public VariantTagBehavior(Logger output) => _output = output;
        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _output.Messages.Add($"Tag behavior {typeof(TTag).Name}");
            return next();
        }
    }

    public class DualFooRequest : IRequest<Pong> { }
    public class DualBarRequest : IRequest<Pong> { }

    public class DualFooRequestHandler : IRequestHandler<DualFooRequest, Pong>
    {
        private readonly Logger _output;
        public DualFooRequestHandler(Logger output) => _output = output;
        public Task<Pong> Handle(DualFooRequest request, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Handler");
            return Task.FromResult(new Pong());
        }
    }

    public class DualBarRequestHandler : IRequestHandler<DualBarRequest, Pong>
    {
        private readonly Logger _output;
        public DualBarRequestHandler(Logger output) => _output = output;
        public Task<Pong> Handle(DualBarRequest request, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Handler");
            return Task.FromResult(new Pong());
        }
    }

    public class DualBehavior<TResponse> : IPipelineBehavior<DualFooRequest, TResponse>, IPipelineBehavior<DualBarRequest, TResponse>
    {
        private readonly Logger _output;
        public DualBehavior(Logger output) => _output = output;

        public Task<TResponse> Handle(DualFooRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Dual behavior Foo");
            return next();
        }

        public Task<TResponse> Handle(DualBarRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Dual behavior Bar");
            return next();
        }
    }

    [Fact]
    public async Task Should_infer_type_parameter_from_array_element_in_constraint()
    {
        var (mediator, output) = BuildMediator(cfg => cfg.AddOpenBehavior(typeof(ArrayElementBehavior<,,>)));

        await mediator.Send(new ArrayCommand());

        output.Messages.ShouldBe(new[] { "Array behavior Int32", "Handler" });
    }

    [Fact]
    public async Task Should_try_every_constraint_link_when_inferring_type_parameter()
    {
        var (mediator, output) = BuildMediator(cfg => cfg.AddOpenBehavior(typeof(TwoLinkBehavior<,,>)));

        await mediator.Send(new LinkRequest());

        output.Messages.ShouldBe(new[] { "Link behavior BaseThing", "Handler" });
    }

    [Fact]
    public async Task Should_infer_type_parameter_when_fixed_constraint_argument_is_satisfied_by_variance()
    {
        var (mediator, output) = BuildMediator(cfg => cfg.AddOpenBehavior(typeof(VariantTagBehavior<,,>)));

        await mediator.Send(new VariantTagCommand());

        output.Messages.ShouldBe(new[] { "Tag behavior Int32", "Handler" });
    }

    [Fact]
    public async Task Should_close_every_service_interface_an_open_behavior_implements()
    {
        var (mediator, output) = BuildMediator(cfg => cfg.AddOpenBehavior(typeof(DualBehavior<>)));

        await mediator.Send(new DualFooRequest());
        await mediator.Send(new DualBarRequest());

        output.Messages.ShouldBe(new[] { "Dual behavior Foo", "Handler", "Dual behavior Bar", "Handler" });
    }

    // Repeated parameters that line up with different types under variance, and many independent constraint links

    public interface IPair<out TLeft, out TRight> { }
    public class PairCommand : IRequest, IPair<DerivedThing, BaseThing> { }

    public class PairCommandHandler : IRequestHandler<PairCommand>
    {
        private readonly Logger _output;
        public PairCommandHandler(Logger output) => _output = output;
        public Task Handle(PairCommand request, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Handler");
            return Task.CompletedTask;
        }
    }

    // TItem lines up with DerivedThing and BaseThing; only BaseThing satisfies both covariant positions
    public class PairBehavior<TRequest, TResponse, TItem> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IPair<TItem, TItem>
    {
        private readonly Logger _output;
        public PairBehavior(Logger output) => _output = output;
        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _output.Messages.Add($"Pair behavior {typeof(TItem).Name}");
            return next();
        }
    }

    public interface ISlot1<T> { }
    public interface ISlot2<T> { }
    public interface ISlot3<T> { }
    public interface ISlot4<T> { }
    public interface ISlot5<T> { }
    public interface ISlot6<T> { }
    public interface ISlot7<T> { }
    public interface ISlot8<T> { }
    public interface ISlot9<T> { }
    public interface ISlot10<T> { }

    public class SlotCommand : IRequest, ISlot1<int>, ISlot2<int>, ISlot3<int>, ISlot4<int>, ISlot5<int>, ISlot6<int>, ISlot7<int>, ISlot8<int>, ISlot9<int>, ISlot10<int> { }

    public class SlotCommandHandler : IRequestHandler<SlotCommand>
    {
        private readonly Logger _output;
        public SlotCommandHandler(Logger output) => _output = output;
        public Task Handle(SlotCommand request, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Handler");
            return Task.CompletedTask;
        }
    }

    public class ManySlotBehavior<TRequest, TResponse, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ISlot1<T1>, ISlot2<T2>, ISlot3<T3>, ISlot4<T4>, ISlot5<T5>, ISlot6<T6>, ISlot7<T7>, ISlot8<T8>, ISlot9<T9>, ISlot10<T10>
    {
        private readonly Logger _output;
        public ManySlotBehavior(Logger output) => _output = output;
        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _output.Messages.Add("Slot behavior");
            return next();
        }
    }

    [Fact]
    public async Task Should_try_every_occurrence_of_a_repeated_type_parameter_in_a_constraint()
    {
        var (mediator, output) = BuildMediator(cfg => cfg.AddOpenBehavior(typeof(PairBehavior<,,>)));

        await mediator.Send(new PairCommand());

        output.Messages.ShouldBe(new[] { "Pair behavior BaseThing", "Handler" });
    }

    [Fact]
    public async Task Should_infer_many_independent_type_parameters_without_exploring_every_ordering()
    {
        // Exploring every ordering of the 10 constraint links visits 10! = 3,628,800 paths per matching request;
        // pruning repeated binding states visits at most 2^10 = 1,024
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var (mediator, output) = BuildMediator(cfg => cfg.AddOpenBehavior(typeof(ManySlotBehavior<,,,,,,,,,,,>)));
        stopwatch.Stop();

        await mediator.Send(new SlotCommand());

        output.Messages.ShouldBe(new[] { "Slot behavior", "Handler" });
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(5));
    }
}
