using MediatR.DependencyInjectionTests.Abstractions;
using MediatR.DependencyInjectionTests.Providers;

namespace MediatR.DependencyInjectionTests;

public class LightInjectDependencyInjectionTests() 
    : BaseAssemblyResolutionTests(new LightInjectServiceProviderFixture());