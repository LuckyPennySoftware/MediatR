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

    #region REQUESTS

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

    #endregion

    #region VOID_REQUESTS

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

    #endregion
}