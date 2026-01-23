using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace MediatR.Extensions.Microsoft.DependencyInjection.Tests;

public class DerivingRequestsTests
{
    private readonly IServiceProvider _provider;
    private readonly IMediator _mediator;

    public DerivingRequestsTests()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddFakeLogging();
        services.AddSingleton(new Logger());
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining(typeof(Ping)));
        _provider = services.BuildServiceProvider();
        _mediator = _provider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task ShouldReturnPingPong()
    {
        Pong pong = await _mediator.Send(new Ping() { Message = "Ping" });
        pong.Message.ShouldBe("Ping Pong");
    }

    [Fact]
    public async Task ShouldReturnDerivedPingPong()
    {
        Pong pong = await _mediator.Send(new DerivedPing() { Message = "Ping" });
        pong.Message.ShouldBe("DerivedPing Pong");
    }
}