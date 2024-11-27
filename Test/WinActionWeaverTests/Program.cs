using System.Reflection;
using NUnit.Framework.Internal;
using NUnit.Framework;

namespace WinActionWeaverTests;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length > 0)
        {
            // Run specific test if specified
            var testName = args[0];
            return await RunSpecificTest(testName);
        }
        
        // Run all tests by default
        return await RunAllTests();
    }

    private static async Task<int> RunSpecificTest(string testName)
    {
        try
        {
            var testClass = typeof(KeyRemappingTests);
            var testMethod = testClass.GetMethod(testName);
            
            if (testMethod == null)
            {
                Console.Error.WriteLine($"Test method {testName} not found");
                return 1;
            }

            var instance = Activator.CreateInstance(testClass);
            
            // Run setup if it exists
            var setup = testClass.GetMethod("Setup");
            setup?.Invoke(instance, null);

            try
            {
                // Handle async test methods
                if (testMethod.ReturnType == typeof(Task))
                {
                    await (Task)testMethod.Invoke(instance, null)!;
                }
                else
                {
                    testMethod.Invoke(instance, null);
                }
                return 0;
            }
            finally
            {
                // Run teardown if it exists
                var teardown = testClass.GetMethod("TearDown");
                teardown?.Invoke(instance, null);
            }
        }
        catch (Exception ex)
        {
            var innerException = ex.InnerException ?? ex;
            Console.Error.WriteLine($"Test failed: {innerException.Message}");
            Console.Error.WriteLine(innerException.StackTrace);
            return 1;
        }
    }

    private static async Task<int> RunAllTests()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var testClasses = assembly.GetTypes()
                .Where(t => t.GetMethods().Any(m => m.GetCustomAttributes(typeof(TestAttribute), false).Length > 0));

            foreach (var testClass in testClasses)
            {
                var instance = Activator.CreateInstance(testClass);
                var testMethods = testClass.GetMethods()
                    .Where(m => m.GetCustomAttributes(typeof(TestAttribute), false).Length > 0);

                foreach (var testMethod in testMethods)
                {
                    Console.WriteLine($"Running test: {testClass.Name}.{testMethod.Name}");
                    
                    if (testMethod.ReturnType == typeof(Task))
                    {
                        await (Task)testMethod.Invoke(instance, null)!;
                    }
                    else
                    {
                        testMethod.Invoke(instance, null);
                    }
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Tests failed: {ex.Message}");
            return 1;
        }
    }
} 