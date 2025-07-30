using System;
using System.Linq;
using MediatR;
using MediatR.Licensing;
using MediatR.Pipeline;
using MediatR.Registration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions to scan for MediatR handlers and registers them.
/// - Scans for any handler interface implementations and registers them as <see cref="ServiceLifetime.Transient"/>
/// - Scans for any <see cref="IRequestPreProcessor{TRequest}"/> and <see cref="IRequestPostProcessor{TRequest,TResponse}"/> implementations and registers them as transient instances
/// Registers <see cref="IMediator"/> as a transient instance
/// After calling AddMediatR you can use the container to resolve an <see cref="IMediator"/> instance.
/// This does not scan for any <see cref="IPipelineBehavior{TRequest,TResponse}"/> instances including <see cref="RequestPreProcessorBehavior{TRequest,TResponse}"/> and <see cref="RequestPreProcessorBehavior{TRequest,TResponse}"/>.
/// To register behaviors, use the <see cref="ServiceCollectionServiceExtensions.AddTransient(IServiceCollection,Type,Type)"/> with the open generic or closed generic types.
/// </summary>
public static class MediatRServiceCollectionExtensions
{
    /// <summary>
    /// Registers handlers and mediator types from the specified assemblies
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">The action used to configure the options</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddMediatR(this IServiceCollection services, 
        Action<MediatRServiceConfiguration> configuration)
    {
        var serviceConfig = new MediatRServiceConfiguration();

        configuration.Invoke(serviceConfig);

        return services.AddMediatR(serviceConfig);
    }
    
    /// <summary>
    /// Registers handlers and mediator types from the specified assemblies
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration options</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddMediatR(this IServiceCollection services, 
        MediatRServiceConfiguration configuration)
    {
        if (!configuration.AssembliesToRegister.Any())
        {
            throw new ArgumentException("No assemblies found to scan. Supply at least one assembly to scan for handlers.");
        }

        ServiceRegistrar.SetGenericRequestHandlerRegistrationLimitations(configuration);

        ServiceRegistrar.AddMediatRClassesWithTimeout(services, configuration);

        ServiceRegistrar.AddRequiredServices(services, configuration);

        return services;
    }
    
    internal static void CheckLicense(this IServiceProvider serviceProvider)
    {
        if (LicenseChecked == false)
        {
            var licenseAccessor = serviceProvider.GetService<LicenseAccessor>() ?? new LicenseAccessor(
                serviceProvider.GetRequiredService<MediatRServiceConfiguration>(),
                serviceProvider.GetRequiredService<ILoggerFactory>()
            );
            var licenseValidator = serviceProvider.GetService<LicenseValidator>() 
                                   ?? new LicenseValidator(serviceProvider.GetRequiredService<ILoggerFactory>());
            
            var license = licenseAccessor.Current;
            licenseValidator.Validate(license);
        }

        LicenseChecked = true;
    }

    internal static bool LicenseChecked { get; set; }
}