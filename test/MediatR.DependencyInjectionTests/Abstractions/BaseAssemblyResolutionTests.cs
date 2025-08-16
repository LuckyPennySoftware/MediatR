using MediatR.DependencyInjectionTests.Contracts.Notifications;
using MediatR.DependencyInjectionTests.Contracts.StreamRequests;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.DependencyInjectionTests.Abstractions;

public abstract class BaseAssemblyResolutionTests : IClassFixture<BaseServiceProviderFixture>
{
    private readonly IServiceProvider _provider;

    protected BaseAssemblyResolutionTests(BaseServiceProviderFixture fixture) =>
        _provider = fixture.Provider;

    [Fact]
    public void Should_Resolve_Mediator() =>
        _provider.GetService<IMediator>()
            .ShouldNotBeNull();

    [Fact]
    public void Should_Resolve_Public_RequestHandler() =>
        _provider.GetService<IRequestHandler<PublicPing, Pong>>()
            .ShouldNotBeNull();

    [Fact]
    public void Should_Resolve_Internal_RequestHandler() =>
        _provider.GetService<IRequestHandler<InternalPing, Pong>>()
            .ShouldNotBeNull();

    [Fact]
    public void Should_Resolve_Private_RequestHandler() =>
        _provider.GetService<IRequestHandler<PrivatePing, Pong>>()
            .ShouldNotBeNull();

    [Fact]
    public void Should_Resolve_Public_Void_RequestHandler() =>
        _provider.GetService<IRequestHandler<PublicVoidPing>>()
            .ShouldNotBeNull();

    [Fact]
    public void Should_Resolve_Internal_Void_RequestHandler() =>
        _provider.GetService<IRequestHandler<InternalVoidPing>>()
            .ShouldNotBeNull();

    [Fact]
    public void Should_Resolve_Private_Void_RequestHandler() =>
        _provider.GetService<IRequestHandler<PrivateVoidPing>>()
            .ShouldNotBeNull();

    [Fact]
    public void Should_Resolve_Public_Private_Internal_Notification_Handlers() =>
        _provider.GetServices<INotificationHandler<Ding>>()
            .Count()
            .ShouldBe(3);

    [Fact]
    public void Should_Resolve_Public_Stream_Request_Handlers() =>
        _provider.GetService<IStreamRequestHandler<PublicZing, Zong>>()
            .ShouldNotBeNull();

    [Fact]
    public void Should_Resolve_Internal_Stream_Request_Handlers() =>
        _provider.GetService<IStreamRequestHandler<InternalZing, Zong>>()
            .ShouldNotBeNull();

    [Fact]
    public void Should_Resolve_Private_Stream_Request_Handlers() =>
        _provider.GetService<IStreamRequestHandler<PrivateZing, Zong>>()
            .ShouldNotBeNull();
}