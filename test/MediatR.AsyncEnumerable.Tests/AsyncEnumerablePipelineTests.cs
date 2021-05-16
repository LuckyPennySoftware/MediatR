﻿
namespace MediatR.AsyncEnumerable.Tests
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using MediatR.AsyncEnumerable;
    using Shouldly;
    using StructureMap;
    using Xunit;

    public class AsyncEnumerablePipelineTests
    {
        public class Ping : IRequest<Pong>
        {
            public string Message { get; set; }
        }

        public class Pong
        {
            public string Message { get; set; }
        }

        public class Zing : IRequest<Zong>
        {
            public string Message { get; set; }
        }

        public class Zong
        {
            public string Message { get; set; }
        }

        public class PingHandler : IAsyncEnumerableRequestHandler<Ping, Pong>
        {
            private readonly Logger _output;

            public PingHandler(Logger output)
            {
                _output = output;
            }

            public async IAsyncEnumerable<Pong> Handle(Ping request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                _output.Messages.Add("Handler");
                yield return await Task.FromResult(new Pong { Message = request.Message + " Pong" });
            }
        }

        public class ZingHandler : IAsyncEnumerableRequestHandler<Zing, Zong>
        {
            private readonly Logger _output;

            public ZingHandler(Logger output)
            {
                _output = output;
            }

            public async IAsyncEnumerable<Zong> Handle(Zing request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                _output.Messages.Add("Handler");
                yield return await Task.FromResult(new Zong { Message = request.Message + " Zong" });
            }
        }

        public class OuterBehavior : IAsyncEnumerablePipelineBehavior<Ping, Pong>
        {
            private readonly Logger _output;

            public OuterBehavior(Logger output)
            {
                _output = output;
            }

            public async IAsyncEnumerable<Pong> Handle(Ping request, [EnumeratorCancellation] CancellationToken cancellationToken, AsyncEnumerableHandlerDelegate<Pong> next)
            {
                _output.Messages.Add("Outer before");
                await foreach (var result in next())
                {
                    yield return result;
                }
                _output.Messages.Add("Outer after");
            }
        }

        public class InnerBehavior : IAsyncEnumerablePipelineBehavior<Ping, Pong>
        {
            private readonly Logger _output;

            public InnerBehavior(Logger output)
            {
                _output = output;
            }

            public async IAsyncEnumerable<Pong> Handle(Ping request, [EnumeratorCancellation] CancellationToken cancellationToken, AsyncEnumerableHandlerDelegate<Pong> next)
            {
                _output.Messages.Add("Inner before");
                await foreach (var result in next())
                {
                    yield return result;
                }
                _output.Messages.Add("Inner after");
            }
        }

        public class InnerBehavior<TRequest, TResponse> : IAsyncEnumerablePipelineBehavior<TRequest, TResponse>
        {
            private readonly Logger _output;

            public InnerBehavior(Logger output)
            {
                _output = output;
            }

            public async IAsyncEnumerable<TResponse> Handle(TRequest request, [EnumeratorCancellation] CancellationToken cancellationToken, AsyncEnumerableHandlerDelegate<TResponse> next)
            {
                _output.Messages.Add("Inner generic before");
                await foreach (var result in next())
                {
                    yield return result;
                }
                _output.Messages.Add("Inner generic after");
            }
        }

        public class OuterBehavior<TRequest, TResponse> : IAsyncEnumerablePipelineBehavior<TRequest, TResponse>
        {
            private readonly Logger _output;

            public OuterBehavior(Logger output)
            {
                _output = output;
            }

            public async IAsyncEnumerable<TResponse> Handle(TRequest request, [EnumeratorCancellation] CancellationToken cancellationToken, AsyncEnumerableHandlerDelegate<TResponse> next)
            {
                _output.Messages.Add("Outer generic before");
                await foreach (var result in next())
                {
                    yield return result;
                }
                _output.Messages.Add("Outer generic after");
            }
        }

        public class ConstrainedBehavior<TRequest, TResponse> : IAsyncEnumerablePipelineBehavior<TRequest, TResponse>
            where TRequest : Ping
            where TResponse : Pong
        {
            private readonly Logger _output;

            public ConstrainedBehavior(Logger output)
            {
                _output = output;
            }
            public async IAsyncEnumerable<TResponse> Handle(TRequest request, [EnumeratorCancellation] CancellationToken cancellationToken, AsyncEnumerableHandlerDelegate<TResponse> next)
            {
                _output.Messages.Add("Constrained before");
                await foreach (var result in next())
                {
                    yield return result;
                }
                _output.Messages.Add("Constrained after");
            }
        }

        public class ConcreteBehavior : IAsyncEnumerablePipelineBehavior<Ping, Pong>
        {
            private readonly Logger _output;

            public ConcreteBehavior(Logger output)
            {
                _output = output;
            }

            public async IAsyncEnumerable<Pong> Handle(Ping request, [EnumeratorCancellation] CancellationToken cancellationToken, AsyncEnumerableHandlerDelegate<Pong> next)
            {
                _output.Messages.Add("Concrete before");
                await foreach (var result in next())
                {
                    yield return result;
                }
                _output.Messages.Add("Concrete after");
            }
        }

        public class Logger
        {
            public IList<string> Messages { get; } = new List<string>();
        }



        [Fact]
        public async Task Should_wrap_with_behavior()
        {
            // Arrange
            var output = new Logger();
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(AsyncEnumerablePipelineTests));
                    scanner.IncludeNamespaceContainingType<Ping>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IAsyncEnumerableRequestHandler<,>));
                });
                cfg.For<Logger>().Singleton().Use(output);
                cfg.For<IAsyncEnumerablePipelineBehavior<Ping, Pong>>().Add<OuterBehavior>();
                cfg.For<IAsyncEnumerablePipelineBehavior<Ping, Pong>>().Add<InnerBehavior>();
                cfg.For<ServiceFactory>().Use<ServiceFactory>(ctx => t => ctx.GetInstance(t));
                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator = container.GetInstance<IMediator>();

            // Act
            await foreach (var response in mediator.ToAsyncEnumerable(new Ping { Message = "Ping" }))
            {
                response.Message.ShouldBe("Ping Pong");
            }

            // Assert
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
            // Arrange
            var output = new Logger();
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(AsyncEnumerablePipelineTests));
                    scanner.IncludeNamespaceContainingType<Ping>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IAsyncEnumerableRequestHandler<,>));
                });
                cfg.For<Logger>().Singleton().Use(output);

                cfg.For(typeof(IAsyncEnumerablePipelineBehavior<,>)).Add(typeof(OuterBehavior<,>));
                cfg.For(typeof(IAsyncEnumerablePipelineBehavior<,>)).Add(typeof(InnerBehavior<,>));

                cfg.For<ServiceFactory>().Use<ServiceFactory>(ctx => t => ctx.GetInstance(t));
                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator = container.GetInstance<IMediator>();

            // Act
            await foreach (var response in mediator.ToAsyncEnumerable(new Ping { Message = "Ping" }))
            {
                response.Message.ShouldBe("Ping Pong");
            }

            // Assert
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
        public async Task Should_handle_constrained_generics()
        {
            // Arrange
            var output = new Logger();
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(AsyncEnumerablePipelineTests));
                    scanner.IncludeNamespaceContainingType<Ping>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IAsyncEnumerableRequestHandler<,>));
                });
                cfg.For<Logger>().Singleton().Use(output);

                cfg.For(typeof(IAsyncEnumerablePipelineBehavior<,>)).Add(typeof(OuterBehavior<,>));
                cfg.For(typeof(IAsyncEnumerablePipelineBehavior<,>)).Add(typeof(InnerBehavior<,>));
                cfg.For(typeof(IAsyncEnumerablePipelineBehavior<,>)).Add(typeof(ConstrainedBehavior<,>));

                cfg.For<ServiceFactory>().Use<ServiceFactory>(ctx => t => ctx.GetInstance(t));
                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator = container.GetInstance<IMediator>();

            // Act 1
            await foreach (var response in mediator.ToAsyncEnumerable(new Ping { Message = "Ping" }))
            {
                response.Message.ShouldBe("Ping Pong");
            }

            // Assert 1
            output.Messages.ShouldBe(new[]
            {
                "Outer generic before",
                "Inner generic before",
                "Constrained before",
                "Handler",
                "Constrained after",
                "Inner generic after",
                "Outer generic after",
            });

            output.Messages.Clear();

            // Act 2
            await foreach (var response in mediator.ToAsyncEnumerable(new Zing { Message = "Zing" }))
            {
                response.Message.ShouldBe("Zing Zong");
            }

            // Assert 2
            output.Messages.ShouldBe(new[]
            {
                "Outer generic before",
                "Inner generic before",
                "Handler",
                "Inner generic after",
                "Outer generic after",
            });
        }



        [Fact(Skip = "StructureMap does not mix concrete and open generics. Use constraints instead.")]
        public async Task Should_handle_concrete_and_open_generics()
        {
            // Arrange
            var output = new Logger();
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(AsyncEnumerablePipelineTests));
                    scanner.IncludeNamespaceContainingType<Ping>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IAsyncEnumerableRequestHandler<,>));
                });
                cfg.For<Logger>().Singleton().Use(output);

                cfg.For(typeof(IAsyncEnumerablePipelineBehavior<,>)).Add(typeof(OuterBehavior<,>));
                cfg.For(typeof(IAsyncEnumerablePipelineBehavior<,>)).Add(typeof(InnerBehavior<,>));
                cfg.For(typeof(IAsyncEnumerablePipelineBehavior<Ping, Pong>)).Add(typeof(ConcreteBehavior));

                cfg.For<ServiceFactory>().Use<ServiceFactory>(ctx => t => ctx.GetInstance(t));
                cfg.For<IMediator>().Use<Mediator>();
            });

            var mediator = container.GetInstance<IMediator>();

            // Act 1
            await foreach (var response in mediator.ToAsyncEnumerable(new Ping { Message = "Ping" }))
            {
                response.Message.ShouldBe("Ping Pong");
            }

            // Assert 1
            output.Messages.ShouldBe(new[]
            {
                "Outer generic before",
                "Inner generic before",
                "Concrete before",
                "Handler",
                "Concrete after",
                "Inner generic after",
                "Outer generic after",
            });

            output.Messages.Clear();

            // Act 2
            await foreach (var response in mediator.ToAsyncEnumerable(new Zing { Message = "Zing" }))
            {
                response.Message.ShouldBe("Zing Zong");
            }

            // Assert 2
            output.Messages.ShouldBe(new[]
            {
                "Outer generic before",
                "Inner generic before",
                "Handler",
                "Inner generic after",
                "Outer generic after",
            });
        }
    }
}