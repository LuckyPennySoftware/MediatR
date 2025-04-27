using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MediatR;
using MediatR.Entities;
using MediatR.NotificationPublishers;
using MediatR.Pipeline;
using MediatR.Registration;

namespace Microsoft.Extensions.DependencyInjection;

public class MediatRServiceConfiguration
{
    /// <summary>
    /// Optional filter for types to register. Default value is a function returning true.
    /// </summary>
    public Func<Type, bool> TypeEvaluator { get; set; } = t => true;

    /// <summary>
    /// Mediator implementation type to register. Default is <see cref="Mediator"/>
    /// </summary>
    public Type MediatorImplementationType { get; set; } = typeof(Mediator);

    /// <summary>
    /// Strategy for publishing notifications. Defaults to <see cref="ForeachAwaitPublisher"/>
    /// </summary>
    public INotificationPublisher NotificationPublisher { get; set; } = new ForeachAwaitPublisher();

    /// <summary>
    /// Type of notification publisher strategy to register. If set, overrides <see cref="NotificationPublisher"/>
    /// </summary>
    public Type? NotificationPublisherType { get; set; }

    /// <summary>
    /// Service lifetime to register services under. Default value is <see cref="ServiceLifetime.Transient"/>
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;

    /// <summary>
    /// Request exception action processor strategy. Default value is <see cref="DependencyInjection.RequestExceptionActionProcessorStrategy.ApplyForUnhandledExceptions"/>
    /// </summary>
    public RequestExceptionActionProcessorStrategy RequestExceptionActionProcessorStrategy { get; set; }
        = RequestExceptionActionProcessorStrategy.ApplyForUnhandledExceptions;

    internal List<Assembly> AssembliesToRegister { get; } = new();

    /// <summary>
    /// List of behaviors to register in specific order
    /// </summary>
    public List<ServiceDescriptor> BehaviorsToRegister { get; } = new();

    /// <summary>
    /// List of stream behaviors to register in specific order
    /// </summary>
    public List<ServiceDescriptor> StreamBehaviorsToRegister { get; } = new();

    /// <summary>
    /// List of request pre processors to register in specific order
    /// </summary>
    public List<ServiceDescriptor> RequestPreProcessorsToRegister { get; } = new();

    /// <summary>
    /// List of request post processors to register in specific order
    /// </summary>
    public List<ServiceDescriptor> RequestPostProcessorsToRegister { get; } = new();

    /// <summary>
    /// Automatically register processors during assembly scanning
    /// </summary>
    public bool AutoRegisterRequestProcessors { get; set; }

    /// <summary>
    /// Configure the maximum number of type parameters that a generic request handler can have. To Disable this constraint, set the value to 0.
    /// </summary>
    public int MaxGenericTypeParameters { get; set; } = 10;

    /// <summary>
    /// Configure the maximum number of types that can close a generic request type parameter constraint.  To Disable this constraint, set the value to 0.
    /// </summary>
    public int MaxTypesClosing { get; set; } = 100;

    /// <summary>
    /// Configure the Maximum Amount of Generic RequestHandler Types MediatR will try to register.  To Disable this constraint, set the value to 0.
    /// </summary>
    public int MaxGenericTypeRegistrations { get; set; } = 125000;

    /// <summary>
    /// Configure the Timeout in Milliseconds that the GenericHandler Registration Process will exit with error.  To Disable this constraint, set the value to 0.
    /// </summary>
    public int RegistrationTimeout { get; set; } = 15000;

    /// <summary>
    /// Flag that controlls whether MediatR will attempt to register handlers that containg generic type parameters.
    /// </summary>
    public bool RegisterGenericHandlers { get; set; } = false;

    /// <summary>
    /// Flag that controls whether MediatR will defer to an existing registration when resolving a handler.
    /// </summary>
    public bool DeferToExistingRegistrations { get; set; } = false;

    /// <summary>
    /// Register various handlers from assembly containing given type
    /// </summary>
    /// <typeparam name="T">Type from assembly to scan</typeparam>
    /// <returns>This</returns>
    public MediatRServiceConfiguration RegisterServicesFromAssemblyContaining<T>()
        => RegisterServicesFromAssemblyContaining(typeof(T));

    /// <summary>
    /// Register various handlers from assembly containing given type
    /// </summary>
    /// <param name="type">Type from assembly to scan</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration RegisterServicesFromAssemblyContaining(Type type)
        => RegisterServicesFromAssembly(type.Assembly);

    /// <summary>
    /// Register various handlers from assembly
    /// </summary>
    /// <param name="assembly">Assembly to scan</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        AssembliesToRegister.Add(assembly);

        return this;
    }

    /// <summary>
    /// Register various handlers from assemblies
    /// </summary>
    /// <param name="assemblies">Assemblies to scan</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration RegisterServicesFromAssemblies(
        params Assembly[] assemblies)
    {
        AssembliesToRegister.AddRange(assemblies);

        return this;
    }

    /// <summary>
    /// Register a closed behavior type
    /// </summary>
    /// <typeparam name="TServiceType">Closed behavior interface type</typeparam>
    /// <typeparam name="TImplementationType">Closed behavior implementation type</typeparam>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration AddBehavior<TServiceType, TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddBehavior(typeof(TServiceType), typeof(TImplementationType), serviceLifetime);

    /// <summary>
    /// Register a closed behavior type against all <see cref="IPipelineBehavior{TRequest,TResponse}"/> implementations
    /// </summary>
    /// <typeparam name="TImplementationType">Closed behavior implementation type</typeparam>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration AddBehavior<TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        return AddBehavior(typeof(TImplementationType), serviceLifetime);
    }

    /// <summary>
    /// Register a closed behavior type against all <see cref="IPipelineBehavior{TRequest,TResponse}"/> implementations
    /// </summary>
    /// <param name="implementationType">Closed behavior implementation type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration AddBehavior(Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        var implementedGenericInterfaces = implementationType.FindInterfacesThatClose(typeof(IPipelineBehavior<,>)).ToList();

        if (implementedGenericInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{implementationType.Name} must implement {typeof(IPipelineBehavior<,>).FullName}");
        }

        foreach (var implementedBehaviorType in implementedGenericInterfaces)
        {
            BehaviorsToRegister.Add(new ServiceDescriptor(implementedBehaviorType, implementationType, serviceLifetime));
        }

        return this;
    }

    /// <summary>
    /// Register a closed behavior type
    /// </summary>
    /// <param name="serviceType">Closed behavior interface type</param>
    /// <param name="implementationType">Closed behavior implementation type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration AddBehavior(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        BehaviorsToRegister.Add(new ServiceDescriptor(serviceType, implementationType, serviceLifetime));

        return this;
    }

    /// <summary>
    /// Registers an open behavior type against the <see cref="IPipelineBehavior{TRequest,TResponse}"/> open generic interface type
    /// </summary>
    /// <param name="openBehaviorType">An open generic behavior type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration AddOpenBehavior(Type openBehaviorType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        if (!openBehaviorType.IsGenericType)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must be generic");
        }

        var implementedGenericInterfaces = openBehaviorType.GetInterfaces().Where(i => i.IsGenericType).Select(i => i.GetGenericTypeDefinition());
        var implementedOpenBehaviorInterfaces = new HashSet<Type>(implementedGenericInterfaces.Where(i => i == typeof(IPipelineBehavior<,>)));

        if (implementedOpenBehaviorInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must implement {typeof(IPipelineBehavior<,>).FullName}");
        }

        foreach (var openBehaviorInterface in implementedOpenBehaviorInterfaces)
        {
            BehaviorsToRegister.Add(new ServiceDescriptor(openBehaviorInterface, openBehaviorType, serviceLifetime));
        }

        return this;
    }

    /// <summary>
    /// Registers multiple open behavior types against the <see cref="IPipelineBehavior{TRequest,TResponse}"/> open generic interface type
    /// </summary>
    /// <param name="openBehaviorTypes">An open generic behavior type list includes multiple open generic behavior types.</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration AddOpenBehaviors(IEnumerable<Type> openBehaviorTypes, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        foreach (var openBehaviorType in openBehaviorTypes)
        {
            AddOpenBehavior(openBehaviorType, serviceLifetime);
        }

        return this;
    }

    /// <summary>
    /// Registers open behaviors against the <see cref="IPipelineBehavior{TRequest,TResponse}"/> open generic interface type
    /// </summary>
    /// <param name="openBehaviors">An open generic behavior list includes multiple <see cref="OpenBehavior"/> open generic behaviors.</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration AddOpenBehaviors(IEnumerable<OpenBehavior> openBehaviors)
    {
        foreach (var openBehavior in openBehaviors)
        {
            AddOpenBehavior(openBehavior.OpenBehaviorType!, openBehavior.ServiceLifetime);
        }

        return this;
    }

    /// <summary>
    /// Register a closed stream behavior type
    /// </summary>
    /// <typeparam name="TServiceType">Closed stream behavior interface type</typeparam>
    /// <typeparam name="TImplementationType">Closed stream behavior implementation type</typeparam>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration AddStreamBehavior<TServiceType, TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddStreamBehavior(typeof(TServiceType), typeof(TImplementationType), serviceLifetime);

    /// <summary>
    /// Register a closed stream behavior type
    /// </summary>
    /// <param name="serviceType">Closed stream behavior interface type</param>
    /// <param name="implementationType">Closed stream behavior implementation type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration AddStreamBehavior(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        StreamBehaviorsToRegister.Add(new ServiceDescriptor(serviceType, implementationType, serviceLifetime));

        return this;
    }

    /// <summary>
    /// Register a closed stream behavior type against all <see cref="IStreamPipelineBehavior{TRequest,TResponse}"/> implementations
    /// </summary>
    /// <typeparam name="TImplementationType">Closed stream behavior implementation type</typeparam>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration AddStreamBehavior<TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddStreamBehavior(typeof(TImplementationType), serviceLifetime);

    /// <summary>
    /// Register a closed stream behavior type against all <see cref="IStreamPipelineBehavior{TRequest,TResponse}"/> implementations
    /// </summary>
    /// <param name="implementationType">Closed stream behavior implementation type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration AddStreamBehavior(Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        var implementedGenericInterfaces = implementationType.FindInterfacesThatClose(typeof(IStreamPipelineBehavior<,>)).ToList();

        if (implementedGenericInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{implementationType.Name} must implement {typeof(IStreamPipelineBehavior<,>).FullName}");
        }

        foreach (var implementedBehaviorType in implementedGenericInterfaces)
        {
            StreamBehaviorsToRegister.Add(new ServiceDescriptor(implementedBehaviorType, implementationType, serviceLifetime));
        }

        return this;
    }

    /// <summary>
    /// Registers an open stream behavior type against the <see cref="IStreamPipelineBehavior{TRequest,TResponse}"/> open generic interface type
    /// </summary>
    /// <param name="openBehaviorType">An open generic stream behavior type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration AddOpenStreamBehavior(Type openBehaviorType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        if (!openBehaviorType.IsGenericType)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must be generic");
        }

        var implementedGenericInterfaces = openBehaviorType.GetInterfaces().Where(i => i.IsGenericType).Select(i => i.GetGenericTypeDefinition());
        var implementedOpenBehaviorInterfaces = new HashSet<Type>(implementedGenericInterfaces.Where(i => i == typeof(IStreamPipelineBehavior<,>)));

        if (implementedOpenBehaviorInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must implement {typeof(IStreamPipelineBehavior<,>).FullName}");
        }

        foreach (var openBehaviorInterface in implementedOpenBehaviorInterfaces)
        {
            StreamBehaviorsToRegister.Add(new ServiceDescriptor(openBehaviorInterface, openBehaviorType, serviceLifetime));
        }

        return this;
    }

    /// <summary>
    /// Register a closed request pre processor type
    /// </summary>
    /// <typeparam name="TServiceType">Closed request pre processor interface type</typeparam>
    /// <typeparam name="TImplementationType">Closed request pre processor implementation type</typeparam>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration AddRequestPreProcessor<TServiceType, TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddRequestPreProcessor(typeof(TServiceType), typeof(TImplementationType), serviceLifetime);

    /// <summary>
    /// Register a closed request pre processor type
    /// </summary>
    /// <param name="serviceType">Closed request pre processor interface type</param>
    /// <param name="implementationType">Closed request pre processor implementation type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration AddRequestPreProcessor(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        RequestPreProcessorsToRegister.Add(new ServiceDescriptor(serviceType, implementationType, serviceLifetime));

        return this;
    }

    /// <summary>
    /// Register a closed request pre processor type against all <see cref="IRequestPreProcessor{TRequest}"/> implementations
    /// </summary>
    /// <typeparam name="TImplementationType">Closed request pre processor implementation type</typeparam>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration AddRequestPreProcessor<TImplementationType>(
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddRequestPreProcessor(typeof(TImplementationType), serviceLifetime);

    /// <summary>
    /// Register a closed request pre processor type against all <see cref="IRequestPreProcessor{TRequest}"/> implementations
    /// </summary>
    /// <param name="implementationType">Closed request pre processor implementation type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration AddRequestPreProcessor(Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        var implementedGenericInterfaces = implementationType.FindInterfacesThatClose(typeof(IRequestPreProcessor<>)).ToList();

        if (implementedGenericInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{implementationType.Name} must implement {typeof(IRequestPreProcessor<>).FullName}");
        }

        foreach (var implementedPreProcessorType in implementedGenericInterfaces)
        {
            RequestPreProcessorsToRegister.Add(new ServiceDescriptor(implementedPreProcessorType, implementationType, serviceLifetime));
        }

        return this;
    }

    /// <summary>
    /// Registers an open request pre processor type against the <see cref="IRequestPreProcessor{TRequest}"/> open generic interface type
    /// </summary>
    /// <param name="openBehaviorType">An open generic request pre processor type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration AddOpenRequestPreProcessor(Type openBehaviorType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        if (!openBehaviorType.IsGenericType)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must be generic");
        }

        var implementedGenericInterfaces = openBehaviorType.GetInterfaces().Where(i => i.IsGenericType).Select(i => i.GetGenericTypeDefinition());
        var implementedOpenBehaviorInterfaces = new HashSet<Type>(implementedGenericInterfaces.Where(i => i == typeof(IRequestPreProcessor<>)));

        if (implementedOpenBehaviorInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must implement {typeof(IRequestPreProcessor<>).FullName}");
        }

        foreach (var openBehaviorInterface in implementedOpenBehaviorInterfaces)
        {
            RequestPreProcessorsToRegister.Add(new ServiceDescriptor(openBehaviorInterface, openBehaviorType, serviceLifetime));
        }

        return this;
    }

    /// <summary>
    /// Register a closed request post processor type
    /// </summary>
    /// <typeparam name="TServiceType">Closed request post processor interface type</typeparam>
    /// <typeparam name="TImplementationType">Closed request post processor implementation type</typeparam>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration AddRequestPostProcessor<TServiceType, TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddRequestPostProcessor(typeof(TServiceType), typeof(TImplementationType), serviceLifetime);

    /// <summary>
    /// Register a closed request post processor type
    /// </summary>
    /// <param name="serviceType">Closed request post processor interface type</param>
    /// <param name="implementationType">Closed request post processor implementation type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration AddRequestPostProcessor(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        RequestPostProcessorsToRegister.Add(new ServiceDescriptor(serviceType, implementationType, serviceLifetime));

        return this;
    }

    /// <summary>
    /// Register a closed request post processor type against all <see cref="IRequestPostProcessor{TRequest,TResponse}"/> implementations
    /// </summary>
    /// <typeparam name="TImplementationType">Closed request post processor implementation type</typeparam>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration AddRequestPostProcessor<TImplementationType>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        => AddRequestPostProcessor(typeof(TImplementationType), serviceLifetime);

    /// <summary>
    /// Register a closed request post processor type against all <see cref="IRequestPostProcessor{TRequest,TResponse}"/> implementations
    /// </summary>
    /// <param name="implementationType">Closed request post processor implementation type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration AddRequestPostProcessor(Type implementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        var implementedGenericInterfaces = implementationType.FindInterfacesThatClose(typeof(IRequestPostProcessor<,>)).ToList();

        if (implementedGenericInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{implementationType.Name} must implement {typeof(IRequestPostProcessor<,>).FullName}");
        }

        foreach (var implementedPostProcessorType in implementedGenericInterfaces)
        {
            RequestPostProcessorsToRegister.Add(new ServiceDescriptor(implementedPostProcessorType, implementationType, serviceLifetime));
        }
        return this;
    }

    /// <summary>
    /// Registers an open request post processor type against the <see cref="IRequestPostProcessor{TRequest,TResponse}"/> open generic interface type
    /// </summary>
    /// <param name="openBehaviorType">An open generic request post processor type</param>
    /// <param name="serviceLifetime">Optional service lifetime, defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <returns>This</returns>
    public MediatRServiceConfiguration AddOpenRequestPostProcessor(Type openBehaviorType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        if (!openBehaviorType.IsGenericType)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must be generic");
        }

        var implementedGenericInterfaces = openBehaviorType.GetInterfaces().Where(i => i.IsGenericType).Select(i => i.GetGenericTypeDefinition());
        var implementedOpenBehaviorInterfaces = new HashSet<Type>(implementedGenericInterfaces.Where(i => i == typeof(IRequestPostProcessor<,>)));

        if (implementedOpenBehaviorInterfaces.Count == 0)
        {
            throw new InvalidOperationException($"{openBehaviorType.Name} must implement {typeof(IRequestPostProcessor<,>).FullName}");
        }

        foreach (var openBehaviorInterface in implementedOpenBehaviorInterfaces)
        {
            RequestPostProcessorsToRegister.Add(new ServiceDescriptor(openBehaviorInterface, openBehaviorType, serviceLifetime));
        }

        return this;
    }


}