using MediatR.Licensing;
using MediatR.Registration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediatR.Tests;

public static class TestContainer
{
    public static IServiceProvider Create(Action<ServiceCollection> config)
    {
        var services = new ServiceCollection();
        
        ConfigAction(services);

        var container = services.BuildServiceProvider();

        return container;

        void ConfigAction(ServiceCollection cfg)
        {
            cfg.AddSingleton<ILoggerFactory, NullLoggerFactory>();

            ServiceRegistrar.AddRequiredServices(cfg, new MediatRServiceConfiguration());

            config(cfg);
        }
    } 
}