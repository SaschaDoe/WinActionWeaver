using System.Diagnostics;
using System.Security.Principal;

namespace WinActionWeaverTests.TestHelpers;

public static class AdminTestRunner
{
    private const string TEST_RUNNER_ENV_VAR = "RUNNING_AS_ADMIN_TEST";
    
    public static void RunWithAdmin(Action testAction, string testName)
    {
        // Check if we're already running as the elevated process
        if (Environment.GetEnvironmentVariable(TEST_RUNNER_ENV_VAR) == "1")
        {
            try
            {
                testAction();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Test failed in elevated process: {ex}");
                Environment.Exit(1);
            }
            return;
        }

        // If we're not admin, restart the process with elevation
        if (!AdminPrivilegeHelper.IsRunningAsAdmin())
        {
            // Set the environment variable in the current process
            Environment.SetEnvironmentVariable(TEST_RUNNER_ENV_VAR, "1");
            
            var startInfo = new ProcessStartInfo
            {
                FileName = Process.GetCurrentProcess().MainModule?.FileName ?? throw new InvalidOperationException("Could not get current process filename"),
                Arguments = $"--test \"{testName}\"",
                UseShellExecute = true, // Required for elevation
                Verb = "runas" // This triggers the UAC prompt
            };

            try
            {
                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start elevated process");
                }
                
                process.WaitForExit();
                
                if (process.ExitCode != 0)
                {
                    Assert.Fail($"Admin test process exited with code {process.ExitCode}");
                }
                
                Assert.Pass("Test completed in elevated process");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to run test as admin: {ex.Message}");
            }
            finally
            {
                Environment.SetEnvironmentVariable(TEST_RUNNER_ENV_VAR, null);
            }
        }
        else
        {
            testAction();
        }
    }
} 