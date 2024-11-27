using System.Runtime.InteropServices;
using WinFormsApp1.Models;
using WinFormsApp1.Services;

namespace WinFormsApp1;

static class Program
{
    [STAThread]
    static void Main()
    {
        // Enable console immediately for debugging
        AllocConsole();
        LogService.EnableConsoleOutput();
        
        LogService.Log("Application starting...");
        LogService.Log($"Current Directory: {Environment.CurrentDirectory}");
        LogService.Log($"Config file exists: {File.Exists("config.json")}");
        
        if (File.Exists("config.json"))
        {
            LogService.Log($"Config content: {File.ReadAllText("config.json")}");
        }
        
        var config = EnsureConfigurationExists();
        LogService.Log($"Loaded config ShowDebugConsole: {config.ShowDebugConsole}");
        
        ApplicationConfiguration.Initialize();
        using var keyboardHook = new KeyboardHook();
        Application.Run();
    }

    private static KeyMappingConfiguration EnsureConfigurationExists()
    {
        var configService = new KeyMappingConfigurationService();
        try
        {
            var config = configService.LoadConfiguration();
            LogService.Log("Successfully loaded existing configuration");
            return config;
        }
        catch (Exception ex)
        {
            LogService.Log($"Error loading configuration: {ex.Message}");
            LogService.Log("Creating default configuration...");
            var defaultConfig = new KeyMappingConfiguration
            {
                ShowDebugConsole = false,
                Mappings = new List<KeyMapping>
                {
                    new() { SourceKey = Keys.A, TargetKey = Keys.B },
                    new() { SourceKey = Keys.Space, TargetKey = Keys.Enter }
                }
            };
            configService.SaveConfiguration(defaultConfig);
            return defaultConfig;
        }
    }

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();
}