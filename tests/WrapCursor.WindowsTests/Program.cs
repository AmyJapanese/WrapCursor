using System.Reflection;
using System.Runtime.InteropServices;
using WrapCursor.Core;
class Program
{
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x,int y);
    [StructLayout(LayoutKind.Sequential)] struct MouseInput { public int X,Y; public uint Data,Flags,Time; public UIntPtr Extra; }
    [StructLayout(LayoutKind.Explicit)] struct InputUnion { [FieldOffset(0)] public MouseInput Mouse; }
    [StructLayout(LayoutKind.Sequential)] struct Input { public uint Type; public InputUnion Union; }
    [DllImport("user32.dll",SetLastError=true)] static extern uint SendInput(uint count,Input[] inputs,int size);
    static void InjectPoint(int x,int y)
    {
        var b=SystemInformation.VirtualScreen;
        var input=new Input { Union=new InputUnion { Mouse=new MouseInput { X=(int)Math.Ceiling((x-b.Left+0.5)*65536.0/b.Width),Y=(int)Math.Ceiling((y-b.Top+0.5)*65536.0/b.Height),Flags=0xC001 } } };
        if(SendInput(1,[input],Marshal.SizeOf<Input>())!=1) throw new Exception("Test input failed: "+Marshal.GetLastWin32Error());
    }
    [STAThread]
    static int Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        var assembly=Assembly.Load("WrapCursor");
        var type=assembly.GetType("WrapCursor.TrayApplicationContext")!;
        var context=(ApplicationContext)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,[],null)!;
        var controller=(WrapController)type.GetField("controller",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(context)!;
        var item=(ToolStripMenuItem)type.GetField("enabledItem",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(context)!;
        var tray=(NotifyIcon)type.GetField("tray",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(context)!;
        var liveHook=type.GetField("hook",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(context)!;
        liveHook.GetType().GetEvent("Moved",BindingFlags.Instance|BindingFlags.NonPublic)!.GetAddMethod(true)!.Invoke(liveHook,[(Action<Point,bool>)((p,own)=>Console.WriteLine($"HOOK {p} own={own}"))]);
        int failures=0;
        void Check(string name,bool ok) { Console.WriteLine((ok?"PASS ":"FAIL ")+name); if(!ok) { failures++; Console.WriteLine("Actual cursor: "+Cursor.Position); } }
        Check("started hook and tray",tray.Visible && controller.Enabled);
        item.PerformClick(); Check("disable menu",!controller.Enabled && !item.Checked);
        item.PerformClick(); Check("enable menu",controller.Enabled && item.Checked);
        Console.WriteLine("Monitors: "+string.Join("; ",Screen.AllScreens.Select(s=>s.Bounds)));
        var original=Cursor.Position;
        var bounds=Screen.PrimaryScreen!.Bounds;
        var center=new Point(bounds.Left+bounds.Width/2,bounds.Top+bounds.Height/2);
        var rows=Screen.AllScreens.Select(s=>s.Bounds).Where(r=>r.Top<=center.Y && r.Bottom>center.Y).ToArray();
        var cols=Screen.AllScreens.Select(s=>s.Bounds).Where(r=>r.Left<=center.X && r.Right>center.X).ToArray();
        var left=rows.MinBy(r=>r.Left); var right=rows.MaxBy(r=>r.Right);
        var top=cols.MinBy(r=>r.Top); var bottom=cols.MaxBy(r=>r.Bottom);
        Action[] steps = [
            () => InjectPoint(center.X,center.Y),
            () => InjectPoint(right.Right-1,center.Y),
            () => Check("physical right wrap / no bounce",Cursor.Position==new Point(left.Left+1,center.Y)),
            () => InjectPoint(center.X,center.Y),
            () => InjectPoint(left.Left,center.Y),
            () => Check("physical left wrap",Cursor.Position==new Point(right.Right-2,center.Y)),
            () => InjectPoint(center.X,center.Y),
            () => InjectPoint(center.X,bottom.Bottom-1),
            () => Check("physical bottom wrap",Cursor.Position==new Point(center.X,top.Top+1)),
            () => InjectPoint(center.X,center.Y),
            () => InjectPoint(center.X,top.Top),
            () => Check("physical top wrap",Cursor.Position==new Point(center.X,bottom.Bottom-2))
        ];
        int index=0;
        controller.MoveFailed += () => Console.WriteLine("Native move failed");
        using var timer=new System.Windows.Forms.Timer { Interval=300 };
        timer.Tick += (_,_) => {
            try { if(index<steps.Length) { steps[index++](); Console.WriteLine($"Step {index}: cursor={Cursor.Position}, enabled={controller.Enabled}"); return; } }
            catch(Exception ex) { failures++; Console.WriteLine("FAIL native: "+ex.Message); }
            timer.Stop(); controller.Enabled=false; SetCursorPos(original.X,original.Y); context.ExitThread();
        };
        timer.Start(); Application.Run(context);
        Check("tray removed",!tray.Visible);
        var hook=type.GetField("hook",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(context)!;
        Check("hook released",(IntPtr)hook.GetType().GetField("handle",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(hook)! == IntPtr.Zero);
        context.Dispose();
        if(args.Length>0)
        {
            using var mutex=new Mutex(true,@"Local\WrapCursor.App",out bool owner);
            using var second=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(args[0]) { UseShellExecute=false,CreateNoWindow=true });
            bool exited=second!.WaitForExit(5000);
            Check("published exe / duplicate startup",exited && second.ExitCode==0);
            if(!exited) second.Kill();
            if(owner) mutex.ReleaseMutex();
        }
        return failures==0?0:1;
    }
}
