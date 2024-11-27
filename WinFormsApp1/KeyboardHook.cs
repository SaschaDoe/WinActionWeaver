using System.Diagnostics;
using System.Runtime.InteropServices;
using WinFormsApp1.Services;
using System.Windows.Forms;

namespace WinFormsApp1;

public class KeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookID = IntPtr.Zero;
    private readonly Dictionary<Keys, Keys> _keyMappings = new();
    private readonly KeyMappingConfigurationService _configService;

    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_KEYDOWN = 0x0000;

    public KeyboardHook()
    {
        _configService = new KeyMappingConfigurationService();
        LoadKeyMappings();
        _proc = HookCallback;
        _hookID = SetHook(_proc);
        LogService.Log("Keyboard hook initialized");
        LogMappings();
    }

    private void LogMappings()
    {
        LogService.Log("Current key mappings:");
        foreach (var mapping in _keyMappings)
        {
            LogService.Log($"  {mapping.Key} -> {mapping.Value}");
        }
    }

    private void LoadKeyMappings()
    {
        var config = _configService.LoadConfiguration();
        
        _keyMappings.Clear();
        foreach (var mapping in config.Mappings)
        {
            _keyMappings[mapping.SourceKey] = mapping.TargetKey;
        }
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private delegate IntPtr LowLevelKeyboardProc(
        int nCode, IntPtr wParam, IntPtr lParam);

    private IntPtr HookCallback(
        int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            var sourceKey = (Keys)vkCode;

            LogService.Log($"Key pressed: {sourceKey}");

            if (_keyMappings.TryGetValue(sourceKey, out Keys targetKey))
            {
                LogService.Log($"Mapping {sourceKey} to {targetKey}");
                keybd_event((byte)targetKey, 0, KEYEVENTF_KEYDOWN, 0);
                keybd_event((byte)targetKey, 0, KEYEVENTF_KEYUP, 0);
                return (IntPtr)1; // Block the original key
            }
        }
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        LogService.Log("Disposing keyboard hook");
        UnhookWindowsHookEx(_hookID);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
        IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);
} 