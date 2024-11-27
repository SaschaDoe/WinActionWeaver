using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WinActionWeaver.Core.KeyMapping;

namespace WinActionWeaver.Console;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            System.Console.WriteLine("Starting WinActionWeaver...");
            
            try
            {
                var builder = Host.CreateApplicationBuilder(args);
                System.Console.WriteLine("Created application builder");

                // Configure logging
                builder.Logging.ClearProviders();
                builder.Logging.AddConsole();
                builder.Logging.SetMinimumLevel(LogLevel.Debug);
                System.Console.WriteLine("Configured logging");

                // Register our services
                builder.Services.AddSingleton<MessageLoop>();
                builder.Services.AddSingleton<IKeyMapper, KeyMapper>();
                builder.Services.AddHostedService<KeyMappingHostedService>();
                System.Console.WriteLine("Registered services");

                System.Console.WriteLine("Building host...");
                var host = builder.Build();
                System.Console.WriteLine("Host built successfully");

                // Handle application shutdown
                var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
                lifetime.ApplicationStopping.Register(() =>
                {
                    System.Console.WriteLine("Application is shutting down...");
                });

                AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
                {
                    System.Console.Error.WriteLine($"Fatal unhandled exception: {eventArgs.ExceptionObject}");
                    Environment.Exit(1);
                };

                System.Console.WriteLine("Press Enter to start the service...");
                System.Console.ReadLine();
                System.Console.WriteLine("Starting host...");

                try
                {
                    await host.RunAsync();
                    System.Console.WriteLine("Host stopped normally");
                    return;
                }
                catch (Exception ex)
                {
                    System.Console.Error.WriteLine($"Host error: {ex}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"Setup error: {ex}");
                throw;
            }
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine($"Fatal error: {ex}");
            System.Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            
            System.Console.WriteLine("Press Enter to exit...");
            System.Console.ReadLine();
            Environment.Exit(1);
        }
    }
} 