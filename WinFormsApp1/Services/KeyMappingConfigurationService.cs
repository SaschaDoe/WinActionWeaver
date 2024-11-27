using System.Text.Json;
using WinFormsApp1.Models;

namespace WinFormsApp1.Services;

/// <summary>
/// Service for managing key mapping configuration persistence
/// </summary>
public class KeyMappingConfigurationService
{
    private const string ConfigFileName = "config.json";

    /// <summary>
    /// Loads the key mapping configuration from file
    /// </summary>
    /// <returns>The loaded configuration</returns>
    /// <exception cref="FileNotFoundException">Thrown when config file doesn't exist</exception>
    public KeyMappingConfiguration LoadConfiguration()
    {
        LogService.Log("Loading configuration...");
        
        if (!File.Exists(ConfigFileName))
        {
            LogService.Log("Configuration file not found");
            throw new FileNotFoundException("Configuration file not found", ConfigFileName);
        }

        var jsonString = File.ReadAllText(ConfigFileName);
        var config = JsonSerializer.Deserialize<KeyMappingConfiguration>(jsonString) 
            ?? throw new InvalidOperationException("Failed to deserialize configuration");
            
        LogService.Log($"Loaded {config.Mappings.Count} key mappings");
        return config;
    }

    /// <summary>
    /// Saves the key mapping configuration to file
    /// </summary>
    /// <param name="configuration">The configuration to save</param>
    public void SaveConfiguration(KeyMappingConfiguration configuration)
    {
        LogService.Log("Saving configuration...");
        
        var jsonString = JsonSerializer.Serialize(configuration, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        
        File.WriteAllText(ConfigFileName, jsonString);
        LogService.Log($"Saved {configuration.Mappings.Count} key mappings");
    }
} 