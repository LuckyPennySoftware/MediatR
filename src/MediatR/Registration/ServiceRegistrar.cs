using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using MediatR.Licensing;
using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace MediatR.Registration;

public static class ServiceRegistrar
{
    private static int MaxGenericTypeParameters;
    private static int MaxTypesClosing;
    private static int MaxGenericTypeRegistrations;
    private static int RegistrationTimeout; 

    public static void SetGenericRequestHandlerRegistrationLimitations(MediatRServiceConfiguration configuration)
    {
        MaxGenericTypeParameters = configuration.MaxGenericTypeParameters;
        MaxTypesClosing = configuration.MaxTypesClosing;
        MaxGenericTypeRegistrations = configuration.MaxGenericTypeRegistrations;
        RegistrationTimeout = configuration.RegistrationTimeout;
    }

    public static void AddMediatRClassesWithTimeout(IServiceCollection services, MediatRServiceConfiguration configuration)
    {
        using(var cts = new CancellationTokenSource(RegistrationTimeout))
        {
            try
            {
                AddMediatRClasses(services, configuration, cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("The generic handler registration process timed out.");
            }
        }
    }

    public static void AddMediatRClasses(IServiceCollection services, MediatRServiceConfiguration configuration, CancellationToken cancellationToken = default)
    {   

        var assembliesToScan = configuration.AssembliesToRegister.Distinct().ToArray();

        ConnectImplementationsToTypesClosing(typeof(IRequestHandler<,>), services, assembliesToScan, false, configuration, cancellationToken);
        ConnectImplementationsToTypesClosing(typeof(IRequestHandler<>), services, assembliesToScan, false, configuration, cancellationToken);
        ConnectImplementationsToTypesClosing(typeof(INotificationHandler<>), services, assembliesToScan, true, configuration);
        ConnectImplementationsToTypesClosing(typeof(IStreamRequestHandler<,>), services, assembliesToScan, false, configuration);
        ConnectImplementationsToTypesClosing(typeof(IRequestExceptionHandler<,,>), services, assembliesToScan, true, configuration);
        ConnectImplementationsToTypesClosing(typeof(IRequestExceptionAction<,>), services, assembliesToScan, true, configuration);

        if (configuration.AutoRegisterRequestProcessors)
        {
            ConnectImplementationsToTypesClosing(typeof(IRequestPreProcessor<>), services, assembliesToScan, true, configuration);
            ConnectImplementationsToTypesClosing(typeof(IRequestPostProcessor<,>), services, assembliesToScan, true, configuration);
        }

        var multiOpenInterfaces = new List<Type>
        {
            typeof(INotificationHandler<>),
            typeof(IRequestExceptionHandler<,,>),
            typeof(IRequestExceptionAction<,>)
        };

        if (configuration.AutoRegisterRequestProcessors)
        {
            multiOpenInterfaces.Add(typeof(IRequestPreProcessor<>));
            multiOpenInterfaces.Add(typeof(IRequestPostProcessor<,>));
        }

        foreach (var multiOpenInterface in multiOpenInterfaces)
        {
            var arity = multiOpenInterface.GetGenericArguments().Length;

            var concretions = assembliesToScan
                .SelectMany(a => a.GetLoadableDefinedTypes())
                .Where(type => type.FindInterfacesThatClose(multiOpenInterface).Any())
                .Where(type => type.IsConcrete() && type.IsOpenGeneric())
                .Where(type => type.GetGenericArguments().Length == arity)
                .Where(configuration.TypeEvaluator)
                .ToList();

            foreach (var type in concretions)
            {
                services.AddTransient(multiOpenInterface, type);
            }
        }
    }

    private static void ConnectImplementationsToTypesClosing(Type openRequestInterface,
        IServiceCollection services,
        IEnumerable<Assembly> assembliesToScan,
        bool addIfAlreadyExists,
        MediatRServiceConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var concretions = new List<Type>();
        var interfaces = new List<Type>();
        var genericConcretions = new List<Type>();
        var genericInterfaces = new List<Type>();

        var types = assembliesToScan
            .SelectMany(a => a.GetLoadableDefinedTypes())
            .Where(t => !t.ContainsGenericParameters || configuration.RegisterGenericHandlers)
            .Where(t => t.IsConcrete() && t.FindInterfacesThatClose(openRequestInterface).Any())
            .Where(configuration.TypeEvaluator)
            .ToList();

        foreach (var type in types)
        {
            var interfaceTypes = type.FindInterfacesThatClose(openRequestInterface).ToArray();

            if (!type.IsOpenGeneric())
            {
                concretions.Add(type);

                foreach (var interfaceType in interfaceTypes)
                {
                    interfaces.Fill(interfaceType);
                }
            }
            else
            {
                genericConcretions.Add(type);
                foreach (var interfaceType in interfaceTypes)
                {
                    genericInterfaces.Fill(interfaceType);
                }
            }
        }

        foreach (var @interface in interfaces)
        {
            var exactMatches = concretions.Where(x => x.CanBeCastTo(@interface)).ToList();
            if (addIfAlreadyExists)
            {
                foreach (var type in exactMatches)
                {
                    services.AddTransient(@interface, type);
                }
            }
            else
            {
                if (exactMatches.Count > 1)
                {
                    exactMatches.RemoveAll(m => !IsMatchingWithInterface(m, @interface));
                }

                foreach (var type in exactMatches)
                {
                    services.TryAddTransient(@interface, type);
                }
            }

            if (!@interface.IsOpenGeneric())
            {
                AddConcretionsThatCouldBeClosed(@interface, concretions, services);
            }
        }

        foreach (var @interface in genericInterfaces)
        {
            var exactMatches = genericConcretions.Where(x => x.CanBeCastTo(@interface)).ToList();
            AddAllConcretionsThatClose(@interface, exactMatches, services, assembliesToScan, cancellationToken);
        }
    }

    private static bool IsMatchingWithInterface(Type? handlerType, Type handlerInterface)
    {
        if (handlerType == null || handlerInterface == null)
        {
            return false;
        }

        if (handlerType.IsInterface)
        {
            if (handlerType.GenericTypeArguments.SequenceEqual(handlerInterface.GenericTypeArguments))
            {
                return true;
            }
        }
        else
        {
            return IsMatchingWithInterface(handlerType.GetInterface(handlerInterface.Name), handlerInterface);
        }

        return false;
    }

    private static void AddConcretionsThatCouldBeClosed(Type @interface, List<Type> concretions, IServiceCollection services)
    {
        foreach (var type in concretions
                     .Where(x => x.IsOpenGeneric() && x.CouldCloseTo(@interface)))
        {
            try
            {
                services.TryAddTransient(@interface, type.MakeGenericType(@interface.GenericTypeArguments));
            }
            catch (Exception)
            {
            }
        }
    }

    private static (Type Service, Type Implementation) GetConcreteRegistrationTypes(Type openRequestHandlerInterface, Type concreteGenericTRequest, Type openRequestHandlerImplementation)
    {
        var closingTypes = concreteGenericTRequest.GetGenericArguments();

        var concreteTResponse = concreteGenericTRequest.GetInterfaces()
            .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRequest<>))
            ?.GetGenericArguments()
            .FirstOrDefault();

        var typeDefinition = openRequestHandlerInterface.GetGenericTypeDefinition();

        var serviceType = concreteTResponse != null ?
            typeDefinition.MakeGenericType(concreteGenericTRequest, concreteTResponse) :
            typeDefinition.MakeGenericType(concreteGenericTRequest);

        return (serviceType, openRequestHandlerImplementation.MakeGenericType(closingTypes));
    }

    private static List<Type>? GetConcreteRequestTypes(Type openRequestHandlerInterface, Type openRequestHandlerImplementation, IEnumerable<Assembly> assembliesToScan, CancellationToken cancellationToken)
    {
        //request generic type constraints       
        var constraintsForEachParameter = openRequestHandlerImplementation
            .GetGenericArguments()
            .Select(x => x.GetGenericParameterConstraints())
            .ToList();

        var typesThatCanCloseForEachParameter = constraintsForEachParameter
            .Select(constraints => assembliesToScan
                .SelectMany(assembly => assembly.GetLoadableDefinedTypes())
                .Where(type => type.IsClass && !type.IsAbstract && constraints.All(constraint => constraint.IsAssignableFrom(type))).ToList()
            ).ToList();

        var requestType = openRequestHandlerInterface.GenericTypeArguments.First();

        if (requestType.IsGenericParameter)
            return null;

        var requestGenericTypeDefinition = requestType.GetGenericTypeDefinition();
              
        var combinations = GenerateCombinations(requestType, typesThatCanCloseForEachParameter, 0, cancellationToken);

        return combinations.Select(types => requestGenericTypeDefinition.MakeGenericType(types.ToArray())).ToList();
    }

    // Method to generate combinations recursively
    public static List<List<Type>> GenerateCombinations(Type requestType, List<List<Type>> lists, int depth = 0, CancellationToken cancellationToken = default)
    {
        if (depth == 0)
        {
            // Initial checks
            if (MaxGenericTypeParameters > 0 && lists.Count > MaxGenericTypeParameters)
                throw new ArgumentException($"Error registering the generic type: {requestType.FullName}. The number of generic type parameters exceeds the maximum allowed ({MaxGenericTypeParameters}).");

            foreach (var list in lists)
            {
                if (MaxTypesClosing > 0 && list.Count > MaxTypesClosing)
                    throw new ArgumentException($"Error registering the generic type: {requestType.FullName}. One of the generic type parameter's count of types that can close exceeds the maximum length allowed ({MaxTypesClosing}).");
            }

            // Calculate the total number of combinations
            long totalCombinations = 1;
            foreach (var list in lists)
            {
                totalCombinations *= list.Count;
                if (MaxGenericTypeParameters > 0 && totalCombinations > MaxGenericTypeRegistrations)
                    throw new ArgumentException($"Error registering the generic type: {requestType.FullName}. The total number of generic type registrations exceeds the maximum allowed ({MaxGenericTypeRegistrations}).");
            }
        }

        if (depth >= lists.Count)
            return new List<List<Type>> { new List<Type>() };
       
        cancellationToken.ThrowIfCancellationRequested();

        var currentList = lists[depth];
        var childCombinations = GenerateCombinations(requestType, lists, depth + 1, cancellationToken);
        var combinations = new List<List<Type>>();

        foreach (var item in currentList)
        {
            foreach (var childCombination in childCombinations)
            {
                var currentCombination = new List<Type> { item };
                currentCombination.AddRange(childCombination);
                combinations.Add(currentCombination);
            }
        }

        return combinations;
    }

    private static void AddAllConcretionsThatClose(Type openRequestInterface, List<Type> concretions, IServiceCollection services, IEnumerable<Assembly> assembliesToScan, CancellationToken cancellationToken)
    {
        foreach (var concretion in concretions)
        {   
            var concreteRequests = GetConcreteRequestTypes(openRequestInterface, concretion, assembliesToScan, cancellationToken);

            if (concreteRequests is null)
                continue;

            var registrationTypes = concreteRequests
                .Select(concreteRequest => GetConcreteRegistrationTypes(openRequestInterface, concreteRequest, concretion));

            foreach (var (Service, Implementation) in registrationTypes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                services.AddTransient(Service, Implementation);
            }
        }
    }

    internal static bool CouldCloseTo(this Type openConcretion, Type closedInterface)
    {
        var openInterface = closedInterface.GetGenericTypeDefinition();
        var arguments = closedInterface.GenericTypeArguments;

        var concreteArguments = openConcretion.GenericTypeArguments;
        return arguments.Length == concreteArguments.Length && openConcretion.CanBeCastTo(openInterface);
    }

    private static bool CanBeCastTo(this Type pluggedType, Type pluginType)
    {
        if (pluggedType == null) return false;

        if (pluggedType == pluginType) return true;

        return pluginType.IsAssignableFrom(pluggedType);
    }

    private static bool IsOpenGeneric(this Type type)
    {
        return type.IsGenericTypeDefinition || type.ContainsGenericParameters;
    }

    internal static IEnumerable<Type> FindInterfacesThatClose(this Type pluggedType, Type templateType)
    {
        return FindInterfacesThatClosesCore(pluggedType, templateType).Distinct();
    }

    private static IEnumerable<Type> FindInterfacesThatClosesCore(Type pluggedType, Type templateType)
    {
        if (pluggedType == null) yield break;

        if (!pluggedType.IsConcrete()) yield break;

        if (templateType.IsInterface)
        {
            foreach (
                var interfaceType in
                pluggedType.GetInterfaces()
                    .Where(type => type.IsGenericType && (type.GetGenericTypeDefinition() == templateType)))
            {
                yield return interfaceType;
            }
        }
        else if (pluggedType.BaseType!.IsGenericType &&
                 (pluggedType.BaseType!.GetGenericTypeDefinition() == templateType))
        {
            yield return pluggedType.BaseType!;
        }

        if (pluggedType.BaseType == typeof(object)) yield break;

        foreach (var interfaceType in FindInterfacesThatClosesCore(pluggedType.BaseType!, templateType))
        {
            yield return interfaceType;
        }
    }

    private static bool IsConcrete(this Type type)
    {
        return !type.IsAbstract && !type.IsInterface;
    }

    private static void Fill<T>(this IList<T> list, T value)
    {
        if (list.Contains(value)) return;
        list.Add(value);
    }

    private static IEnumerable<Type> GetLoadableDefinedTypes(this Assembly assembly)
    {
        try
        {
            return assembly.DefinedTypes;
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }

    private static Type? GetOpenServiceInterface(Type openImplementationType, Type openServiceType) =>
        openImplementationType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == openServiceType);

    // The DI container closes open generics positionally, which only works when the implementation's
    // type parameters map one-to-one, in order, onto the service interface (e.g. IPipelineBehavior<TRequest, TResponse>).
    // Anything else (nested generics like List<T>, extra type parameters, reordering) must be closed explicitly.
    private static bool RequiresExplicitClosing(Type openImplementationType, Type openServiceType)
    {
        var iface = GetOpenServiceInterface(openImplementationType, openServiceType);
        if (iface == null) return false;
        return !iface.GetGenericArguments().SequenceEqual(openImplementationType.GetGenericArguments());
    }

    private static bool TryMatchType(Type pattern, Type concrete, Dictionary<Type, Type> bindings)
    {
        if (pattern.IsGenericParameter)
        {
            if (bindings.TryGetValue(pattern, out var existing)) return existing == concrete;
            bindings[pattern] = concrete;
            return true;
        }
        if (pattern.IsGenericType && concrete.IsGenericType)
        {
            if (pattern.GetGenericTypeDefinition() != concrete.GetGenericTypeDefinition()) return false;
            var pArgs = pattern.GetGenericArguments();
            var cArgs = concrete.GetGenericArguments();
            if (pArgs.Length != cArgs.Length) return false;
            return pArgs.Zip(cArgs, (p, c) => TryMatchType(p, c, bindings)).All(x => x);
        }
        return pattern == concrete;
    }

    private static IEnumerable<Type> GetGenericParametersIn(Type type)
    {
        if (type.IsGenericParameter) return new[] { type };
        if (type.IsArray) return GetGenericParametersIn(type.GetElementType()!);
        if (!type.IsGenericType) return Enumerable.Empty<Type>();
        return type.GetGenericArguments().SelectMany(GetGenericParametersIn);
    }

    private static IEnumerable<Type> GetSelfBaseTypesAndInterfaces(Type type)
    {
        for (var current = type; current != null && current != typeof(object); current = current.BaseType)
        {
            yield return current;
        }
        foreach (var @interface in type.GetInterfaces())
        {
            yield return @interface;
        }
    }

    // Behavior type parameters that appear in neither TRequest nor TResponse can still be inferred
    // when a constraint links them to a parameter that is already known, e.g.
    // TransactionBehavior<TRequest, TResponse, TDbContext> where TRequest : ITransactionalCommand<TDbContext>.
    // Returns every complete set of bindings, since a request may implement the constraint more than once.
    private static IEnumerable<Dictionary<Type, Type>> ExpandBindingsFromConstraints(
        Type[] behaviorParams, Dictionary<Type, Type> bindings)
    {
        if (behaviorParams.All(bindings.ContainsKey))
        {
            yield return bindings;
            yield break;
        }

        var link = behaviorParams
            .Where(bindings.ContainsKey)
            .SelectMany(tp => tp.GetGenericParameterConstraints().Select(c => (Param: tp, Constraint: c)))
            .FirstOrDefault(x => GetGenericParametersIn(x.Constraint).Any(p => !bindings.ContainsKey(p)));

        if (link.Constraint == null) yield break;

        foreach (var candidate in GetSelfBaseTypesAndInterfaces(bindings[link.Param]))
        {
            var extended = new Dictionary<Type, Type>(bindings);
            if (!TryMatchType(link.Constraint, candidate, extended) || extended.Count == bindings.Count) continue;

            foreach (var result in ExpandBindingsFromConstraints(behaviorParams, extended))
            {
                yield return result;
            }
        }
    }

    private static void ValidateTypeParametersCanBeInferred(Type openImplementationType, Type iface)
    {
        var reachable = new HashSet<Type>(iface.GetGenericArguments().SelectMany(GetGenericParametersIn));
        var added = true;
        while (added)
        {
            added = false;
            foreach (var tp in reachable.ToList())
            {
                foreach (var p in tp.GetGenericParameterConstraints().SelectMany(GetGenericParametersIn))
                {
                    added |= reachable.Add(p);
                }
            }
        }

        var unreachable = openImplementationType.GetGenericArguments().Where(tp => !reachable.Contains(tp)).ToList();
        if (unreachable.Count > 0)
        {
            throw new InvalidOperationException(
                $"Cannot register {openImplementationType.Name}: type parameter(s) {string.Join(", ", unreachable.Select(t => t.Name))} " +
                $"do not appear in the {iface.Name} arguments or in a constraint linked to them, so they cannot be inferred.");
        }
    }

    private static IEnumerable<(Type RequestType, Type ResponseType)> FindRequestTypes(IEnumerable<Assembly> assemblies, bool streamRequests) =>
        assemblies
            .SelectMany(a => a.GetLoadableDefinedTypes())
            .Where(t => t.IsConcrete() && !t.ContainsGenericParameters)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => streamRequests
                    ? i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamRequest<>)
                    : i == typeof(IRequest) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)))
                .Select(i => (RequestType: t, ResponseType: i == typeof(IRequest) ? typeof(Unit) : i.GetGenericArguments()[0])))
            .ToList();

    private static void RegisterClosedImplementations(
        Type openImplementationType, Type iface, IServiceCollection services,
        IEnumerable<(Type RequestType, Type ResponseType)> requests, ServiceLifetime lifetime)
    {
        // Single-argument services (IRequestPreProcessor<TRequest>) only match on the request type
        var patterns   = iface.GetGenericArguments();
        var implParams = openImplementationType.GetGenericArguments();
        var openServiceType = iface.GetGenericTypeDefinition();

        foreach (var (requestType, responseType) in requests)
        {
            var serviceArgs = patterns.Length == 1 ? new[] { requestType } : new[] { requestType, responseType };
            var bindings = new Dictionary<Type, Type>();
            if (!patterns.Zip(serviceArgs, (p, c) => TryMatchType(p, c, bindings)).All(x => x)) continue;

            foreach (var complete in ExpandBindingsFromConstraints(implParams, bindings))
            {
                try
                {
                    var closedImplementation = openImplementationType.MakeGenericType(implParams.Select(tp => complete[tp]).ToArray());
                    var closedService        = openServiceType.MakeGenericType(serviceArgs);
                    services.TryAddEnumerable(new ServiceDescriptor(closedService, closedImplementation, lifetime));
                }
                catch (ArgumentException) { /* generic constraints not satisfied for this request */ }
            }
        }
    }

    // Open implementations the DI container can't close positionally (nested generics such as List<T>,
    // or extra type parameters inferred from constraints) are closed explicitly by scanning
    // the registered assemblies for matching requests. The open registration is skipped for these,
    // since the Microsoft container would either close it incorrectly or reject it for mismatched arity,
    // and containers that can close it would apply it a second time for scanned requests.
    // With CloseInContainer, implementations of matching arity are left open for the container to close instead.
    private static void RegisterOpenGenericServices(
        IServiceCollection services, IEnumerable<ServiceDescriptor> serviceDescriptors, Type openServiceType,
        Lazy<IEnumerable<(Type RequestType, Type ResponseType)>> requests, OpenBehaviorClosingStrategy closingStrategy)
    {
        foreach (var serviceDescriptor in serviceDescriptors)
        {
            if (serviceDescriptor.ImplementationType != null
                && serviceDescriptor.ServiceType == openServiceType
                && serviceDescriptor.ImplementationType.IsOpenGeneric()
                && RequiresExplicitClosing(serviceDescriptor.ImplementationType, openServiceType)
                && !(closingStrategy == OpenBehaviorClosingStrategy.CloseInContainer
                     && serviceDescriptor.ImplementationType.GetGenericArguments().Length == openServiceType.GetGenericArguments().Length))
            {
                var iface = GetOpenServiceInterface(serviceDescriptor.ImplementationType, openServiceType)!;
                ValidateTypeParametersCanBeInferred(serviceDescriptor.ImplementationType, iface);
                RegisterClosedImplementations(serviceDescriptor.ImplementationType, iface, services, requests.Value, serviceDescriptor.Lifetime);
                continue;
            }

            services.TryAddEnumerable(serviceDescriptor);
        }
    }

    public static void AddRequiredServices(IServiceCollection services, MediatRServiceConfiguration serviceConfiguration)
    {
        // Use TryAdd, so any existing ServiceFactory/IMediator registration doesn't get overridden
        services.TryAdd(new ServiceDescriptor(typeof(IMediator), serviceConfiguration.MediatorImplementationType, serviceConfiguration.Lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(ISender), sp => sp.GetRequiredService<IMediator>(), serviceConfiguration.Lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(IPublisher), sp => sp.GetRequiredService<IMediator>(), serviceConfiguration.Lifetime));

        MediatRServiceCollectionExtensions.LicenseChecked = false;
        
        services.TryAddSingleton(serviceConfiguration);
        services.TryAddSingleton<LicenseAccessor>(static sp =>
        {
            var loggerFactory = sp.GetService<ILoggerFactory>()
                ?? throw new InvalidOperationException(
                    "MediatR requires ILoggerFactory to be registered. " +
                    "Call services.AddLogging() before services.AddMediatR().");
            var config = sp.GetService<MediatRServiceConfiguration>();
            return config != null
                ? new LicenseAccessor(config, loggerFactory)
                : new LicenseAccessor(loggerFactory);
        });
        services.TryAddSingleton<LicenseValidator>(static sp =>
        {
            var loggerFactory = sp.GetService<ILoggerFactory>()
                ?? throw new InvalidOperationException(
                    "MediatR requires ILoggerFactory to be registered. " +
                    "Call services.AddLogging() before services.AddMediatR().");
            return new LicenseValidator(loggerFactory);
        });

        var notificationPublisherServiceDescriptor = serviceConfiguration.NotificationPublisherType != null
            ? new ServiceDescriptor(typeof(INotificationPublisher), serviceConfiguration.NotificationPublisherType, serviceConfiguration.Lifetime)
            : new ServiceDescriptor(typeof(INotificationPublisher), serviceConfiguration.NotificationPublisher);

        services.TryAdd(notificationPublisherServiceDescriptor);

        // Register pre processors, then post processors, then behaviors
        if (serviceConfiguration.RequestExceptionActionProcessorStrategy == RequestExceptionActionProcessorStrategy.ApplyForUnhandledExceptions)
        {
            RegisterBehaviorIfImplementationsExist(services, typeof(RequestExceptionActionProcessorBehavior<,>), typeof(IRequestExceptionAction<,>));
            RegisterBehaviorIfImplementationsExist(services, typeof(RequestExceptionProcessorBehavior<,>), typeof(IRequestExceptionHandler<,,>));
        }
        else
        {
            RegisterBehaviorIfImplementationsExist(services, typeof(RequestExceptionProcessorBehavior<,>), typeof(IRequestExceptionHandler<,,>));
            RegisterBehaviorIfImplementationsExist(services, typeof(RequestExceptionActionProcessorBehavior<,>), typeof(IRequestExceptionAction<,>));
        }

        var requests = new Lazy<IEnumerable<(Type, Type)>>(() => FindRequestTypes(serviceConfiguration.AssembliesToRegister, streamRequests: false));
        var streamRequests = new Lazy<IEnumerable<(Type, Type)>>(() => FindRequestTypes(serviceConfiguration.AssembliesToRegister, streamRequests: true));

        if (serviceConfiguration.RequestPreProcessorsToRegister.Any())
        {
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestPreProcessorBehavior<,>), ServiceLifetime.Transient));
            RegisterOpenGenericServices(services, serviceConfiguration.RequestPreProcessorsToRegister, typeof(IRequestPreProcessor<>), requests, serviceConfiguration.OpenBehaviorClosingStrategy);
        }

        if (serviceConfiguration.RequestPostProcessorsToRegister.Any())
        {
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(RequestPostProcessorBehavior<,>), ServiceLifetime.Transient));
            RegisterOpenGenericServices(services, serviceConfiguration.RequestPostProcessorsToRegister, typeof(IRequestPostProcessor<,>), requests, serviceConfiguration.OpenBehaviorClosingStrategy);
        }

        RegisterOpenGenericServices(services, serviceConfiguration.BehaviorsToRegister, typeof(IPipelineBehavior<,>), requests, serviceConfiguration.OpenBehaviorClosingStrategy);
        RegisterOpenGenericServices(services, serviceConfiguration.StreamBehaviorsToRegister, typeof(IStreamPipelineBehavior<,>), streamRequests, serviceConfiguration.OpenBehaviorClosingStrategy);
    }

    private static void RegisterBehaviorIfImplementationsExist(IServiceCollection services, Type behaviorType, Type subBehaviorType)
    {
        var hasAnyRegistrationsOfSubBehaviorType = services
            .Where(service => !service.IsKeyedService)
            .Select(service => service.ImplementationType)
            .OfType<Type>()
            .SelectMany(type => type.GetInterfaces())
            .Where(type => type.IsGenericType)
            .Select(type => type.GetGenericTypeDefinition())
            .Any(type => type == subBehaviorType);

        if (hasAnyRegistrationsOfSubBehaviorType)
        {
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), behaviorType, ServiceLifetime.Transient));
        }
    }
}
