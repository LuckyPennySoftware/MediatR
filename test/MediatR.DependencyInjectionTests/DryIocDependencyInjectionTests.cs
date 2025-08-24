using MediatR.DependencyInjectionTests.Abstractions;
using MediatR.DependencyInjectionTests.Providers;

namespace MediatR.DependencyInjectionTests;

public class DryIocDependencyInjectionTests() 
    : BaseAssemblyResolutionTests(new DryIocServiceProviderFixture());