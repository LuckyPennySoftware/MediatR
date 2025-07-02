using Lamar;
using MediatR.Licensing;
using MediatR.Registration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediatR.Tests;

public static class TestContainer
{
    public static Container Create(Action<ServiceRegistry> config)
    {
        Action<ServiceRegistry> configAction = cfg =>
        {
            cfg.ForSingletonOf<ILoggerFactory>().Use(new NullLoggerFactory());
            
            ServiceRegistrar.AddRequiredServices(cfg, new MediatRServiceConfiguration());

            config(cfg);
        };
        var container = new Container(configAction);

        return container;
    } 
}