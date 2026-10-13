using System.Runtime.InteropServices;
namespace WrapCursor;
internal static class NativeMethods
{
    internal const int WH_MOUSE_LL=14, WM_MOUSEMOVE=0x200;
    internal static readonly UIntPtr MoveTag = new(0x57524150);
    internal delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);
    [StructLayout(LayoutKind.Sequential)] internal struct NativePoint { public int X,Y; }
    [StructLayout(LayoutKind.Sequential)] internal struct MouseData { public NativePoint Point; public uint MouseInfo,Flags,Time; public UIntPtr ExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] internal struct MouseInput { public int X,Y; public uint MouseData,Flags,Time; public UIntPtr ExtraInfo; }
    [StructLayout(LayoutKind.Explicit)] internal struct InputUnion { [FieldOffset(0)] public MouseInput Mouse; }
    [StructLayout(LayoutKind.Sequential)] internal struct Input { public uint Type; public InputUnion Union; }
    [DllImport("user32.dll",SetLastError=true)] internal static extern IntPtr SetWindowsHookEx(int type,HookProc callback,IntPtr module,uint thread);
    [DllImport("user32.dll")] internal static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] internal static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] internal static extern IntPtr GetModuleHandle(string? name);
    [DllImport("user32.dll",SetLastError=true)] internal static extern uint SendInput(uint count,Input[] inputs,int size);
    [DllImport("user32.dll")] internal static extern bool GetCursorPos(out NativePoint point);
}
