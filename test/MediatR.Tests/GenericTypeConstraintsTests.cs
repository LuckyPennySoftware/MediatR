using System.Reflection;
using System.Threading;

namespace MediatR.Tests;

using System;
using System.Linq;
using Shouldly;
using Lamar;
using System.Threading.Tasks;
using Xunit;

public class GenericTypeConstraintsTests
{
    public interface IGenericTypeRequestHandlerTestClass<TRequest> where TRequest : IBaseRequest
    {
        Type[] Handle(TRequest request);
    }

    public abstract class GenericTypeRequestHandlerTestClass<TRequest> : IGenericTypeRequestHandlerTestClass<TRequest>
        where TRequest : IBaseRequest
    {
        public bool IsIRequest { get; }


        public bool IsIRequestT { get; }

        public bool IsIBaseRequest { get; }

        public GenericTypeRequestHandlerTestClass()
        {
            IsIRequest = typeof(IRequest).IsAssignableFrom(typeof(TRequest));
            IsIRequestT = typeof(TRequest).GetInterfaces()
                .Any(x => x.GetTypeInfo().IsGenericType &&
                          x.GetGenericTypeDefinition() == typeof(IRequest<>));

            IsIBaseRequest = typeof(IBaseRequest).IsAssignableFrom(typeof(TRequest));
        }

        public Type[] Handle(TRequest request)
        {
            return typeof(TRequest).GetInterfaces();
        }
    }

    public class GenericTypeConstraintPing : GenericTypeRequestHandlerTestClass<Ping>
    {

    }

    public class GenericTypeConstraintJing : GenericTypeRequestHandlerTestClass<Jing>
    {

    }

    public class Jing : IRequest
    {
        public string? Message { get; set; }
    }

    public class JingHandler : IRequestHandler<Jing>
    {
        public Task Handle(Jing request, CancellationToken cancellationToken)
        {
            // empty handle
            return Task.CompletedTask;
        }
    }

    public class Ping : IRequest<Pong>
    {
        public string? Message { get; set; }
    }

    public class Pong
    {
        public string? Message { get; set; }
    }

    public class PingHandler : IRequestHandler<Ping, Pong>
    {
        public Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Pong { Message = request.Message + " Pong" });
        }
    }

    private readonly IMediator _mediator;

    public GenericTypeConstraintsTests()
    {
        var container = TestContainer.Create(cfg =>
        {
            cfg.Scan(scanner =>
            {
                scanner.AssemblyContainingType(typeof(GenericTypeConstraintsTests));
                scanner.IncludeNamespaceContainingType<Ping>();
                scanner.IncludeNamespaceContainingType<Jing>();
                scanner.WithDefaultConventions();
                scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
                scanner.AddAllTypesOf(typeof(IRequestHandler<>));
            });
            cfg.For<IMediator>().Use<Mediator>();
        });

        _mediator = container.GetInstance<IMediator>();
    }

    [Fact]
    public async Task Should_Resolve_Void_Return_Request()
    {
        // Create Request
        var jing = new Jing { Message = "Jing" };

        // Test mediator still works sending request
        await _mediator.Send(jing);

        // Create new instance of type constrained class
        var genericTypeConstraintsVoidReturn = new  GenericTypeConstraintJing();

        // Assert it is of type IRequest and IRequest<T>
        Assert.True(genericTypeConstraintsVoidReturn.IsIRequest);
        Assert.False(genericTypeConstraintsVoidReturn.IsIRequestT);
        Assert.True(genericTypeConstraintsVoidReturn.IsIBaseRequest);

        // Verify it is of IRequest and IBaseRequest
        var results = genericTypeConstraintsVoidReturn.Handle(jing);

        Assert.Equal(2, results.Length);

        results.ShouldNotContain(typeof(IRequest<Unit>));
        results.ShouldContain(typeof(IBaseRequest));
        results.ShouldContain(typeof(IRequest));
    }

    [Fact]
    public async Task Should_Resolve_Response_Return_Request()
    {
        // Create Request
        var ping = new Ping { Message = "Ping" };

        // Test mediator still works sending request and gets response
        var pingResponse = await _mediator.Send(ping);
        pingResponse.Message.ShouldBe("Ping Pong");

        // Create new instance of type constrained class
        var genericTypeConstraintsResponseReturn = new GenericTypeConstraintPing();

        // Assert it is of type IRequest<T> but not IRequest
        Assert.False(genericTypeConstraintsResponseReturn.IsIRequest);
        Assert.True(genericTypeConstraintsResponseReturn.IsIRequestT);
        Assert.True(genericTypeConstraintsResponseReturn.IsIBaseRequest);

        // Verify it is of IRequest<Pong> and IBaseRequest, but not IRequest
        var results = genericTypeConstraintsResponseReturn.Handle(ping);

        Assert.Equal(2, results.Length);

        results.ShouldContain(typeof(IRequest<Pong>));
        results.ShouldContain(typeof(IBaseRequest));
        results.ShouldNotContain(typeof(IRequest));
    }
}