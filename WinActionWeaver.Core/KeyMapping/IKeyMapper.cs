namespace WinActionWeaver.Core.KeyMapping;

public interface IKeyMapper
{
    event EventHandler<VirtualKey> KeyPressed;
    void RemapKey(VirtualKey originalKey, VirtualKey newKey);
    void HandleKeyPress(VirtualKey key);
} 