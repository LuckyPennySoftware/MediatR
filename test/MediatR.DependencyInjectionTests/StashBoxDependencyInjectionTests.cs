using MediatR.DependencyInjectionTests.Abstractions;
using MediatR.DependencyInjectionTests.Providers;

namespace MediatR.DependencyInjectionTests;

public class StashBoxDependencyInjectionTests : BaseAssemblyResolutionTests
{
    public StashBoxDependencyInjectionTests() : base(new StashBoxServiceProviderFixture()) { }
}