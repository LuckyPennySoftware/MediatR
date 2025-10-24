using MediatR.DependencyInjectionTests.Abstractions;
using MediatR.DependencyInjectionTests.Providers;

namespace MediatR.DependencyInjectionTests;

public class LamarDependencyInjectionTests() 
    : BaseAssemblyResolutionTests(new LamarServiceProviderFixture());