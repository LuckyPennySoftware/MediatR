using System.Linq;
using MediatR.Tests.MicrosoftExtensionsDI;

namespace MediatR.Tests
{
    [Collection(nameof(ServiceFactoryCollectionBehavior))]
    public class GenericRequestHandlerTests : BaseGenericRequestHandlerTests
    {

        [Theory]
        [InlineData(9, 3, 3)]
        [InlineData(10, 4, 4)]
        [InlineData(1, 1, 1)]
        [InlineData(50, 3, 3)]
        public void ShouldResolveAllCombinationsOfGenericHandler(int numberOfClasses, int numberOfInterfaces, int numberOfTypeParameters)
        {
            var services = new ServiceCollection();

            var dynamicAssembly = GenerateCombinationsTestAssembly(numberOfClasses, numberOfInterfaces, numberOfTypeParameters);

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblies(dynamicAssembly);
                cfg.RegisterGenericHandlers = true;
            });

            var provider = services.BuildServiceProvider();

            var dynamicRequestType = dynamicAssembly.GetType("DynamicRequest")!;

            int expectedCombinations = CalculateTotalCombinations(numberOfClasses, numberOfInterfaces, numberOfTypeParameters);

            var testClasses = Enumerable.Range(1, numberOfClasses)
                .Select(i => dynamicAssembly.GetType($"TestClass{i}")!)
                .ToArray();

            var combinations = GenerateCombinations(testClasses, numberOfInterfaces);          

            foreach (var combination in combinations)
            {
                var concreteRequestType = dynamicRequestType.MakeGenericType(combination);
                var requestHandlerInterface = typeof(IRequestHandler<>).MakeGenericType(concreteRequestType);

                var handler = provider.GetService(requestHandlerInterface);
                handler.ShouldNotBeNull($"Handler for {concreteRequestType} should not be null");
            }            
        }

        [Theory]
        [InlineData(9, 3, 3)]
        [InlineData(10, 4, 4)]
        [InlineData(1, 1, 1)]
        [InlineData(50, 3, 3)]
        public void ShouldRegisterTheCorrectAmountOfHandlers(int numberOfClasses, int numberOfInterfaces, int numberOfTypeParameters)
        {  
            var dynamicAssembly = GenerateCombinationsTestAssembly(numberOfClasses, numberOfInterfaces, numberOfTypeParameters);          
            int expectedCombinations = CalculateTotalCombinations(numberOfClasses, numberOfInterfaces, numberOfTypeParameters);
            var testClasses = Enumerable.Range(1, numberOfClasses)
               .Select(i => dynamicAssembly.GetType($"TestClass{i}")!)
               .ToArray();
            var combinations = GenerateCombinations(testClasses, numberOfInterfaces);
            combinations.Count.ShouldBe(expectedCombinations, $"Should have tested all {expectedCombinations} combinations");
        }

        [Theory]
        [InlineData(9, 3, 3)]
        [InlineData(10, 4, 4)]
        [InlineData(1, 1, 1)]
        [InlineData(50, 3, 3)]
        public void ShouldNotRegisterDuplicateHandlers(int numberOfClasses, int numberOfInterfaces, int numberOfTypeParameters)
        {
            var dynamicAssembly = GenerateCombinationsTestAssembly(numberOfClasses, numberOfInterfaces, numberOfTypeParameters);
            int expectedCombinations = CalculateTotalCombinations(numberOfClasses, numberOfInterfaces, numberOfTypeParameters);
            var testClasses = Enumerable.Range(1, numberOfClasses)
               .Select(i => dynamicAssembly.GetType($"TestClass{i}")!)
               .ToArray();
            var combinations = GenerateCombinations(testClasses, numberOfInterfaces);
            var hasDuplicates = combinations
              .Select(x => string.Join(", ", x.Select(y => y.Name)))
              .GroupBy(x => x)
              .Any(g => g.Count() > 1);

            hasDuplicates.ShouldBeFalse();
        }

        [Fact]
        public void ShouldThrowExceptionWhenTypesClosingExceedsMaximum()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddFakeLogging();

            var assembly = GenerateTypesClosingExceedsMaximumAssembly();

            Should.Throw<ArgumentException>(() =>
            {
                services.AddMediatR(cfg =>
                {
                    cfg.RegisterServicesFromAssembly(assembly);
                    cfg.RegisterGenericHandlers = true;
                });
            })
            .Message.ShouldContain("One of the generic type parameter's count of types that can close exceeds the maximum length allowed");
        }

        [Fact]
        public void ShouldThrowExceptionWhenGenericHandlerRegistrationsExceedsMaximum()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddFakeLogging();

            var assembly = GenerateHandlerRegistrationsExceedsMaximumAssembly();

            Should.Throw<ArgumentException>(() =>
            {
                services.AddMediatR(cfg =>
                {
                    cfg.RegisterServicesFromAssembly(assembly);
                    cfg.RegisterGenericHandlers = true;
                });
            })
            .Message.ShouldContain("The total number of generic type registrations exceeds the maximum allowed");
        }

        [Fact]
        public void ShouldThrowExceptionWhenGenericTypeParametersExceedsMaximum()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddFakeLogging();

            var assembly = GenerateGenericTypeParametersExceedsMaximumAssembly();

            Should.Throw<ArgumentException>(() =>
            {
                services.AddMediatR(cfg =>
                {
                    cfg.RegisterServicesFromAssembly(assembly);
                    cfg.RegisterGenericHandlers = true;
                });
            })
            .Message.ShouldContain("The number of generic type parameters exceeds the maximum allowed");
        }

        [Fact]
        public void ShouldThrowExceptionWhenTimeoutOccurs()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddFakeLogging();

            var assembly = GenerateTimeoutOccursAssembly();

            Should.Throw<TimeoutException>(() =>
            {
                services.AddMediatR(cfg =>
                {
                    cfg.MaxGenericTypeParameters = 0;
                    cfg.MaxGenericTypeRegistrations = 0;
                    cfg.MaxTypesClosing = 0;
                    cfg.RegistrationTimeout = 100;
                    cfg.RegisterGenericHandlers = true;
                    cfg.RegisterServicesFromAssembly(assembly);
                });
            })
            .Message.ShouldBe("The generic handler registration process timed out.");
        }

        [Fact]
        public void ShouldNotRegisterGenericHandlersWhenOptingOut()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddFakeLogging();

            var assembly = GenerateOptOutAssembly();
            services.AddMediatR(cfg =>
            {
                //opt out flag set
                cfg.RegisterGenericHandlers = false;
                cfg.RegisterServicesFromAssembly(assembly);
            });

            var provider = services.BuildServiceProvider();
            var testClasses = Enumerable.Range(1, 2)
                .Select(i => assembly.GetType($"TestClass{i}")!)
                .ToArray();
            var requestType = assembly.GetType("OptOutRequest")!;
            var combinations = GenerateCombinations(testClasses, 2);

            var concreteRequestType = requestType.MakeGenericType(combinations.First());
            var requestHandlerInterface = typeof(IRequestHandler<>).MakeGenericType(concreteRequestType);

            var handler = provider.GetService(requestHandlerInterface);
            handler.ShouldBeNull($"Handler for {concreteRequestType} should be null");


        }
    }
}
