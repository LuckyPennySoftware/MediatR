using MediatR.DependencyInjectionTests.Abstractions;
using MediatR.DependencyInjectionTests.Providers;

namespace MediatR.DependencyInjectionTests;

public class DryIocDependencyInjectionTests : BaseAssemblyResolutionTests
{
    public DryIocDependencyInjectionTests() : base(new DryIocServiceProviderFixture()) { }
}