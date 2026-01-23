using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Deluxe.MediatR.Entities;
/// <summary>
/// Represents a registration entity for pipeline behaviors with a specified service lifetime.
/// </summary>
public class OpenBehavior
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenBehavior"/> class.
    /// </summary>
    /// <param name="openBehaviorType">The type of the pipeline behavior to register.</param>
    /// <param name="serviceLifetime">The lifetime of the registered service. Defaults to Transient.</param>
    /// <exception cref="InvalidOperationException">Thrown if the specified type does not implement IPipelineBehavior.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="openBehaviorType"/> is null.</exception>
    public OpenBehavior(Type openBehaviorType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        ValidatePipelineBehaviorType(openBehaviorType);
        OpenBehaviorType = openBehaviorType;
        ServiceLifetime = serviceLifetime;
    }

    /// <summary>
    /// The type of the open behavior.
    /// </summary>
    public Type OpenBehaviorType { get; }

    /// <summary>
    /// The service lifetime of the open behavior.
    /// </summary>
    public ServiceLifetime ServiceLifetime { get; }

    /// <summary>
    /// Validates whether the specified type implements the <see cref="IPipelineBehavior{TRequest, TResponse}"/> interface.
    /// </summary>
    /// <param name="openBehaviorType">The type to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown if the type does not implement <see cref="IPipelineBehavior{TRequest, TResponse}"/>.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="openBehaviorType"/> is null.</exception>
    private static void ValidatePipelineBehaviorType(Type openBehaviorType)
    {
        if (openBehaviorType == null) throw new ArgumentNullException($"Open behavior type can not be null.");

        bool isPipelineBehavior = openBehaviorType.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

        if (!isPipelineBehavior)
        {
            throw new InvalidOperationException($"The type \"{openBehaviorType.Name}\" must implement IPipelineBehavior<,> interface.");
        }
    }
}