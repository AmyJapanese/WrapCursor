using System.ComponentModel;
using System.Runtime.InteropServices;
namespace WrapCursor;
internal sealed class MouseHook : IDisposable
{
    private IntPtr handle;
    private readonly NativeMethods.HookProc callback;
    internal event Action<Point,bool>? Moved;
    internal event Action? Failed;
    internal MouseHook()
    {
        callback=OnHook;
        handle=NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL,callback,NativeMethods.GetModuleHandle(null),0);
        if(handle==IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
    }
    private IntPtr OnHook(int code,IntPtr message,IntPtr data)
    {
        if(code>=0 && message==(IntPtr)NativeMethods.WM_MOUSEMOVE)
        {
            try
            {
                var mouse=Marshal.PtrToStructure<NativeMethods.MouseData>(data);
                Moved?.Invoke(new Point(mouse.Point.X,mouse.Point.Y),mouse.ExtraInfo==NativeMethods.MoveTag);
            }
            catch { try { Failed?.Invoke(); } catch { } }
        }
        return NativeMethods.CallNextHookEx(handle,code,message,data);
    }
    internal static bool Move(Point target)
    {
        var bounds=SystemInformation.VirtualScreen;
        // Absolute SendInput coordinates address the complete virtual desktop.
        int x=(int)Math.Ceiling((target.X-bounds.Left+0.5)*65536.0/bounds.Width);
        int y=(int)Math.Ceiling((target.Y-bounds.Top+0.5)*65536.0/bounds.Height);
        var input=new NativeMethods.Input { Type=0, Union=new NativeMethods.InputUnion { Mouse=new NativeMethods.MouseInput { X=Math.Clamp(x,0,65535),Y=Math.Clamp(y,0,65535),Flags=0x0001|0x8000|0x4000,ExtraInfo=NativeMethods.MoveTag } } };
        return NativeMethods.SendInput(1,[input],Marshal.SizeOf<NativeMethods.Input>())==1;
    }
    public void Dispose() { if(handle!=IntPtr.Zero) { NativeMethods.UnhookWindowsHookEx(handle); handle=IntPtr.Zero; } }
}
