using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace WinActionWeaver.Core.KeyMapping;

public class NativeKeyboardHook : IDisposable
{
    private const int WM_INPUT = 0x00FF;
    private const int RID_INPUT = 0x10000003;
    private const int RIDEV_INPUTSINK = 0x00000100;
    private const int RIM_TYPEKEYBOARD = 1;
    
    private readonly IntPtr _windowHandle;
    
    public event EventHandler<VirtualKey>? KeyIntercepted;

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICE
    {
        public ushort usUsagePage;
        public ushort usUsage;
        public uint dwFlags;
        public IntPtr hwndTarget;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTHEADER
    {
        public uint dwType;
        public uint dwSize;
        public IntPtr hDevice;
        public IntPtr wParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWKEYBOARD
    {
        public ushort MakeCode;
        public ushort Flags;
        public ushort Reserved;
        public ushort VKey;
        public uint Message;
        public uint ExtraInformation;
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterRawInputDevices(
        [MarshalAs(UnmanagedType.LPArray)] RAWINPUTDEVICE[] pRawInputDevices,
        uint uiNumDevices,
        uint cbSize);

    [DllImport("user32.dll")]
    private static extern int GetRawInputData(
        IntPtr hRawInput,
        uint uiCommand,
        IntPtr pData,
        ref uint pcbSize,
        uint cbSizeHeader);

    public NativeKeyboardHook(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
        
        var rid = new RAWINPUTDEVICE
        {
            usUsagePage = 0x01,      // Generic Desktop Controls
            usUsage = 0x06,          // Keyboard
            dwFlags = RIDEV_INPUTSINK,
            hwndTarget = windowHandle
        };

        if (!RegisterRawInputDevices(new[] { rid }, 1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"Failed to register raw input device. Error: {error}");
        }

        Console.WriteLine("Raw input device registered successfully");
    }

    public void ProcessRawInput(IntPtr lParam)
    {
        uint dwSize = 0;
        GetRawInputData(lParam, RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf<RAWINPUTHEADER>());

        if (dwSize == 0)
            return;

        var buffer = Marshal.AllocHGlobal((int)dwSize);
        try
        {
            GetRawInputData(lParam, RID_INPUT, buffer, ref dwSize, (uint)Marshal.SizeOf<RAWINPUTHEADER>());

            var header = Marshal.PtrToStructure<RAWINPUTHEADER>(buffer);
            if (header.dwType == RIM_TYPEKEYBOARD)
            {
                var rawKeyboard = Marshal.PtrToStructure<RAWKEYBOARD>(IntPtr.Add(buffer, Marshal.SizeOf<RAWINPUTHEADER>()));
                
                // Check if it's a key down event and not injected
                if ((rawKeyboard.Message & 0x0001) == 0 && !KeyboardSimulator.IsInjectedInput((IntPtr)rawKeyboard.ExtraInformation))
                {
                    Console.WriteLine($"Raw input received - VKey: {rawKeyboard.VKey:X}");
                    KeyIntebrcepted?.Invoke(this, (VirtualKey)rawKeyboard.VKey);
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public void Dispose()
    {
        // Unregister by setting flags to 0
        var rid = new RAWINPUTDEVICE
        {
            usUsagePage = 0x01,
            usUsage = 0x06,
            dwFlags = 0,
            hwndTarget = IntPtr.Zero
        };

        RegisterRawInputDevices(new[] { rid }, 1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
    }
} 