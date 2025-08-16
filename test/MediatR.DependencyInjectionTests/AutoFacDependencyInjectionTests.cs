using MediatR.DependencyInjectionTests.Abstractions;
using MediatR.DependencyInjectionTests.Providers;

namespace MediatR.DependencyInjectionTests;

public class AutoFacDependencyInjectionTests : BaseAssemblyResolutionTests
{
    public AutoFacDependencyInjectionTests() : base(new AutoFacServiceProviderFixture()) { }
}