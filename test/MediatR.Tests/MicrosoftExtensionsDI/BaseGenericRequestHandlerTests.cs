using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;

namespace MediatR.Tests.MicrosoftExtensionsDI
{
    public abstract class BaseGenericRequestHandlerTests
    { 
        protected static Assembly GenerateTypesClosingExceedsMaximumAssembly() =>
            CreateAssemblyModuleBuilder("ExceedsMaximumTypesClosingAssembly", 201, 1, CreateHandlerForExceedsMaximumClassesTest);

        protected static Assembly GenerateHandlerRegistrationsExceedsMaximumAssembly() =>
            CreateAssemblyModuleBuilder("ExceedsMaximumHandlerRegistrationsAssembly", 500, 10, CreateHandlerForExceedsMaximumHandlerRegistrationsTest);

        protected static Assembly GenerateGenericTypeParametersExceedsMaximumAssembly() =>
            CreateAssemblyModuleBuilder("ExceedsMaximumGenericTypeParametersAssembly", 1, 1, CreateHandlerForExceedsMaximumGenericTypeParametersTest);

        protected static Assembly GenerateTimeoutOccursAssembly() =>
            CreateAssemblyModuleBuilder("TimeOutOccursAssembly", 400, 3, CreateHandlerForTimeoutOccursTest);

        protected static Assembly GenerateOptOutAssembly() =>
            CreateAssemblyModuleBuilder("OptOutAssembly", 2, 2, CreateHandlerForOptOutTest);

        protected static void CreateHandlerForOptOutTest(ModuleBuilder moduleBuilder) =>
            CreateRequestHandler(moduleBuilder, "OptOutRequest", 2);

        protected static void CreateHandlerForMissingConstraintsTest(ModuleBuilder moduleBuilder) =>
          CreateRequestHandler(moduleBuilder, "MissingConstraintsRequest", 3, 0, false);

        protected static void CreateHandlerForExceedsMaximumClassesTest(ModuleBuilder moduleBuilder) =>
            CreateRequestHandler(moduleBuilder, "ExceedsMaximumTypesClosingRequest", 1);

        protected static void CreateHandlerForExceedsMaximumHandlerRegistrationsTest(ModuleBuilder moduleBuilder) =>
            CreateRequestHandler(moduleBuilder, "ExceedsMaximumHandlerRegistrationsRequest", 4);

        protected static void CreateHandlerForExceedsMaximumGenericTypeParametersTest(ModuleBuilder moduleBuilder) =>
            CreateRequestHandler(moduleBuilder, "ExceedsMaximumGenericTypeParametersRequest", 11, 1);

        protected static void CreateHandlerForTimeoutOccursTest(ModuleBuilder moduleBuilder) =>
            CreateRequestHandler(moduleBuilder, "TimeoutOccursRequest", 3);

        protected static void CreateHandlerForCombinationsTest(ModuleBuilder moduleBuilder, int numberOfGenericParameters) =>
            CreateRequestHandler(moduleBuilder, "DynamicRequest", numberOfGenericParameters);

        protected static void CreateClass(ModuleBuilder moduleBuilder, string className, Type interfaceType)
        {
            TypeBuilder typeBuilder = moduleBuilder.DefineType(className, TypeAttributes.Public);
            typeBuilder.AddInterfaceImplementation(interfaceType);
            typeBuilder.CreateTypeInfo();
        }

        protected static Type CreateInterface(ModuleBuilder moduleBuilder, string interfaceName)
        {
            TypeBuilder interfaceBuilder = moduleBuilder.DefineType(interfaceName, TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            return interfaceBuilder.CreateTypeInfo().AsType();
        }

        protected static AssemblyBuilder CreateAssemblyModuleBuilder(string name, int classes, int interfaces, Action<ModuleBuilder> handlerCreation)
        {
            AssemblyName assemblyName = new AssemblyName(name);
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

            CreateTestClassesAndInterfaces(moduleBuilder, classes, interfaces);
            handlerCreation.Invoke(moduleBuilder);

            return assemblyBuilder;
        }

        protected static AssemblyBuilder GenerateCombinationsTestAssembly(int classes, int interfaces, int genericParameters)
        {
            AssemblyName assemblyName = new AssemblyName("DynamicAssembly");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

            CreateTestClassesAndInterfaces(moduleBuilder, classes, interfaces);
            CreateHandlerForCombinationsTest(moduleBuilder, genericParameters);

            return assemblyBuilder;
        }

        protected static string[] GetGenericParameterNames(int numberOfTypeParameters) => 
            Enumerable.Range(1, numberOfTypeParameters).Select(i => $"T{i}").ToArray();

        protected static void CreateRequestHandler(ModuleBuilder moduleBuilder, string requestName, int numberOfTypeParameters, int numberOfInterfaces = 0, bool includeConstraints = true)
        {
            if(numberOfInterfaces == 0)
            {
                numberOfInterfaces = numberOfTypeParameters;
            }

            // Define the dynamic request class
            var handlerTypeBuilder = moduleBuilder!.DefineType($"{requestName}Handler", TypeAttributes.Public);
            var requestTypeBuilder = moduleBuilder!.DefineType(requestName, TypeAttributes.Public);

            // Define the generic parameters
            string[] genericParameterNames = GetGenericParameterNames(numberOfTypeParameters);
            var handlerGenericParameters = handlerTypeBuilder.DefineGenericParameters(genericParameterNames);
            var requestGenericParameters = requestTypeBuilder.DefineGenericParameters(genericParameterNames);
            requestTypeBuilder.AddInterfaceImplementation(typeof(IRequest));

            if(includeConstraints) 
            {
                for (int i = 0; i < numberOfTypeParameters; i++)
                {
                    int interfaceIndex = i % numberOfInterfaces + 1;

                    var constraintType = moduleBuilder.Assembly.GetType($"ITestInterface{interfaceIndex}");
                    handlerGenericParameters[i].SetInterfaceConstraints(constraintType!);
                    requestGenericParameters[i].SetInterfaceConstraints(constraintType!);
                }
            }

            var requestType = requestTypeBuilder.CreateTypeInfo().AsType();
            handlerTypeBuilder.AddInterfaceImplementation(typeof(IRequestHandler<>).MakeGenericType(requestType));

            // Define the Handle method
            MethodBuilder handleMethodBuilder = handlerTypeBuilder.DefineMethod(
                "Handle",
                MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(Task),
                new[] { requestType, typeof(CancellationToken) });

            ILGenerator ilGenerator = handleMethodBuilder.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ret);

            // Implement the interface method
            handlerTypeBuilder.DefineMethodOverride(handleMethodBuilder, typeof(IRequestHandler<>).MakeGenericType(requestType).GetMethod("Handle")!);

            // Create the dynamic request class
            handlerTypeBuilder.CreateTypeInfo();
        }

        protected static void CreateTestClassesAndInterfaces(ModuleBuilder moduleBuilder, int numberOfClasses, int numberOfInterfaces)
        {

            Type[] interfaces = new Type[numberOfInterfaces];
            for (int i = 1; i <= numberOfInterfaces; i++)
            {
                string interfaceName = $"ITestInterface{i}";
                interfaces[i - 1] = CreateInterface(moduleBuilder, interfaceName);
            }

            for (int i = 1; i <= numberOfClasses; i++)
            {
                string className = $"TestClass{i}";
                Type interfaceType = interfaces[(i - 1) % numberOfInterfaces];
                CreateClass(moduleBuilder, className, interfaceType);
            }
        }

        protected List<Type[]> GenerateCombinations(Type[] types, int interfaces)
        {
            var groups = new List<Type>[interfaces];
            for (int i = 0; i < interfaces; i++)
            {
                groups[i] = types.Where((t, index) => index % interfaces == i).ToList();
            }

            return GenerateCombinationsRecursive(groups, 0);
        }

        protected List<Type[]> GenerateCombinationsRecursive(List<Type>[] groups, int currentGroup)
        {
            var result = new List<Type[]>();

            if (currentGroup == groups.Length)
            {
                result.Add(Array.Empty<Type>());
                return result;
            }

            foreach (var type in groups[currentGroup])
            {
                foreach (var subCombination in GenerateCombinationsRecursive(groups, currentGroup + 1))
                {
                    result.Add(new[] { type }.Concat(subCombination).ToArray());
                }
            }

            return result;
        }

        protected static int CalculateTotalCombinations(int numberOfClasses, int numberOfInterfaces, int numberOfTypeParameters)
        {
            var testClasses = Enumerable.Range(1, numberOfClasses)
                .Select(i => $"TestClass{i}")
                .ToArray();

            var groups = new List<string>[numberOfInterfaces];
            for (int i = 0; i < numberOfInterfaces; i++)
            {
                groups[i] = testClasses.Where((t, index) => index % numberOfInterfaces == i).ToList();
            }

            return groups
                .Take(numberOfTypeParameters)
                .Select(group => group.Count)
                .Aggregate(1, (a, b) => a * b);
        }
    }
}
