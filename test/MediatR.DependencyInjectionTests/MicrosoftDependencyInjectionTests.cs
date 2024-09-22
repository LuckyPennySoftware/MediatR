using MediatR.DependencyInjectionTests.Abstractions;
using MediatR.DependencyInjectionTests.Providers;

namespace MediatR.DependencyInjectionTests;

public class MicrosoftDependencyInjectionTests : BaseAssemblyResolutionTests
{
    public MicrosoftDependencyInjectionTests() : base(new MicrosoftServiceProviderFixture()) { }
}