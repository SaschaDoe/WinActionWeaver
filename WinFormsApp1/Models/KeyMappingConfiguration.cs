namespace WinFormsApp1.Models;

/// <summary>
/// Represents the configuration for key mappings
/// </summary>
public class KeyMappingConfiguration
{
    public List<KeyMapping> Mappings { get; set; } = new();
    public bool ShowDebugConsole { get; set; } = false;
} 