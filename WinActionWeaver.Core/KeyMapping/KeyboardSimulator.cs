using System.Runtime.InteropServices;
using System.ComponentModel;

namespace WinActionWeaver.Core.KeyMapping;

public class KeyboardSimulator
{
    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint INPUT_KEYBOARD = 1;
    private const uint LLKHF_INJECTED = 0x00000010;

    private static readonly IntPtr INJECTED_INPUT_MARKER = new(0x1234);

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION u;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray)] INPUT[] pInputs, int cbSize);

    public void SimulateKeyPress(VirtualKey key)
    {
        Console.WriteLine($"Simulating key press for key: {key} (0x{(int)key:X})");

        var inputs = new INPUT[2];
        
        // Key down
        inputs[0] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = (ushort)key,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYDOWN,
                    time = 0,
                    dwExtraInfo = INJECTED_INPUT_MARKER
                }
            }
        };

        // Key up
        inputs[1] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = (ushort)key,
                    wScan = 0,
                    dwFlags = KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = INJECTED_INPUT_MARKER
                }
            }
        };

        var size = Marshal.SizeOf<INPUT>();
        Console.WriteLine($"Input size: {size}, Input array length: {inputs.Length}");
        Console.WriteLine($"KEYBDINPUT size: {Marshal.SizeOf<KEYBDINPUT>()}");
        Console.WriteLine($"INPUTUNION size: {Marshal.SizeOf<INPUTUNION>()}");

        var result = SendInput((uint)inputs.Length, inputs, size);
        if (result != inputs.Length)
        {
            var error = Marshal.GetLastWin32Error();
            Console.WriteLine($"SendInput failed. Error code: {error}, Error: {new Win32Exception(error).Message}");
            Console.WriteLine($"Expected to send {inputs.Length} inputs, but sent {result}");
            
            // Dump memory for debugging
            var bytes = new byte[size];
            var ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(inputs[0], ptr, false);
                Marshal.Copy(ptr, bytes, 0, size);
                Console.WriteLine("Memory dump of first INPUT structure:");
                Console.WriteLine(BitConverter.ToString(bytes));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
        else
        {
            Console.WriteLine($"SendInput succeeded, sent {result} inputs");
        }
    }

    public static bool IsInjectedInput(IntPtr extraInfo)
    {
        return extraInfo == INJECTED_INPUT_MARKER || (extraInfo.ToInt64() & LLKHF_INJECTED) != 0;
    }
} 