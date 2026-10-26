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
}
