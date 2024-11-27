using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace WinActionWeaver.Core.KeyMapping;

public class NativeKeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;

    private IntPtr _hookHandle;
    private readonly GCHandle _hookDelegate;
    private readonly LowLevelKeyboardProc _proc;
    private readonly KeyboardSimulator _keyboardSimulator;
    private readonly HashSet<VirtualKey> _remappedKeys = new();

    public event EventHandler<VirtualKey>? KeyIntercepted;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    public NativeKeyboardHook(IntPtr windowHandle)
    {
        _proc = HookCallback;
        _hookDelegate = GCHandle.Alloc(_proc, GCHandleType.Normal);
        _keyboardSimulator = new KeyboardSimulator();
        
        // Get the module handle for the current process
        var moduleHandle = GetModuleHandle(Process.GetCurrentProcess().MainModule?.ModuleName);
        
        // Install the hook with the correct module handle
        _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, moduleHandle, 0);
        
        if (_hookHandle == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"Failed to set keyboard hook. Error: {error} ({new Win32Exception(error).Message})");
        }

        Console.WriteLine($"Keyboard hook installed successfully with handle {_hookHandle}");

        // Check if we're on a UI thread
        if (!Dispatcher.CurrentDispatcher.HasShutdownStarted)
        {
            Console.WriteLine("Running on UI thread with message pump");
        }
        else
        {
            Console.WriteLine("WARNING: No message pump detected - hook may not work!");
        }
    } 

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var key = (VirtualKey)hookStruct.vkCode;

            if (key == VirtualKey.A)
            {
                Console.WriteLine("Key intercepted");
       
            // Create a new KBDLLHOOKSTRUCT with modified vkCode for B
            var modifiedHookStruct = new KBDLLHOOKSTRUCT
            {
                vkCode = (uint)VirtualKey.B,
                scanCode = hookStruct.scanCode,
                flags = hookStruct.flags,
                time = hookStruct.time,
                dwExtraInfo = hookStruct.dwExtraInfo
            };

            // Allocate unmanaged memory and marshal the modified struct
            var modifiedLParam = Marshal.AllocHGlobal(Marshal.SizeOf<KBDLLHOOKSTRUCT>());
            Marshal.StructureToPtr(modifiedHookStruct, modifiedLParam, false);

            // Call next hook with modified lParam
            var result = CallNextHookEx(_hookHandle, nCode, wParam, modifiedLParam);

            // Free the allocated memory
            Marshal.FreeHGlobal(modifiedLParam);
            return result;
            }
        }
        
        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void AddRemappedKey(VirtualKey key)
    {
        Console.WriteLine($"Adding remapped key: {key}");
        _remappedKeys.Add(key);
    }

    public void RemoveRemappedKey(VirtualKey key)
    {
        _remappedKeys.Remove(key);
    }

    public void Dispose()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
        
        if (_hookDelegate.IsAllocated)
        {
            _hookDelegate.Free();
        }
    }
} 