namespace WinActionWeaver.Core.KeyMapping;

public class KeyMapper : IKeyMapper, IDisposable
{
    private readonly Dictionary<VirtualKey, VirtualKey> _keyMappings = new();
    private readonly NativeKeyboardHook _keyboardHook;
    private readonly KeyboardSimulator _keyboardSimulator;
    private readonly MessageLoop _messageLoop;
    
    public event EventHandler<VirtualKey>? KeyPressed;

    public KeyMapper()
    {
        try
        {
            Console.WriteLine("Initializing KeyMapper");
            _messageLoop = new MessageLoop();
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
        Console.WriteLine($"Key intercepted: {key}");
        if (_keyMappings.TryGetValue(key, out var mappedKey))
        {
            Console.WriteLine($"Remapping {key} to {mappedKey}");
            _keyboardSimulator.SimulateKeyPress(mappedKey);
            KeyPressed?.Invoke(this, mappedKey);
        }
        else
        {
            Console.WriteLine($"No mapping found for {key}");
            KeyPressed?.Invoke(this, key);
        }
    }

    public void RemapKey(VirtualKey originalKey, VirtualKey newKey)
    {
        _keyMappings[originalKey] = newKey;
    }

    public void HandleKeyPress(VirtualKey key)
    {
        var mappedKey = _keyMappings.TryGetValue(key, out var newKey) ? newKey : key;
        KeyPressed?.Invoke(this, mappedKey);
    }

    public void Dispose()
    {
        _keyboardHook.Dispose();
        _messageLoop.Dispose();
    }
} 