namespace WinActionWeaver.Core.KeyMapping;

public class KeyMapper : IKeyMapper, IDisposable
{
    private readonly Dictionary<VirtualKey, VirtualKey> _keyMappings = new();
    private readonly NativeKeyboardHook _keyboardHook;
    private readonly KeyboardSimulator _keyboardSimulator;
    private readonly MessageLoop _messageLoop;
    
    public event EventHandler<VirtualKey>? KeyPressed;

    public KeyMapper(MessageLoop messageLoop)
    {
        try
        {
            Console.WriteLine("Initializing KeyMapper");
            _messageLoop = messageLoop;
            _keyboardSimulator = new KeyboardSimulator();
            
            _keyboardHook = _messageLoop.KeyboardHook;
            _keyboardHook.KeyIntercepted += OnKeyIntercepted;
            
            Console.WriteLine("KeyMapper initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize KeyMapper: {ex}");
            throw;
        }
    }

    private void OnKeyIntercepted(object? sender, VirtualKey key)
    {
        if (_keyMappings.TryGetValue(key, out var mappedKey))
        {
            Console.WriteLine($"Remapping {key} to {mappedKey}");
            _keyboardSimulator.SimulateKeyPress(mappedKey);
            KeyPressed?.Invoke(this, mappedKey);
        }
    }

    public void RemapKey(VirtualKey originalKey, VirtualKey newKey)
    {
        _keyMappings[originalKey] = newKey;
        _keyboardHook.AddRemappedKey(originalKey);
    }

    public void HandleKeyPress(VirtualKey key)
    {
        var mappedKey = _keyMappings.TryGetValue(key, out var newKey) ? newKey : key;
        KeyPressed?.Invoke(this, mappedKey);
    }

    public void Dispose()
    {
        _keyboardHook?.Dispose();
    }
} 