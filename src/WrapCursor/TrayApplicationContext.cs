using Microsoft.Win32;
using WrapCursor.Core;
namespace WrapCursor;
internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly Control dispatcher=new();
    private readonly NotifyIcon tray;
    private readonly ToolStripMenuItem enabledItem;
    private readonly WrapController controller;
    private readonly MouseHook hook;
    private bool disposed;
    internal TrayApplicationContext()
    {
        dispatcher.CreateControl();
        controller=new WrapController(MouseHook.Move) { Monitors=MonitorProvider.GetBounds() };
        enabledItem=new ToolStripMenuItem("有効") { Checked=true, CheckOnClick=false };
        enabledItem.Click += (_,_) => { controller.Enabled=!controller.Enabled; UpdateStatus(); };
        var exit=new ToolStripMenuItem("終了");
        exit.Click += (_,_) => ExitThread();
        var menu=new ContextMenuStrip(); menu.Items.Add(enabledItem); menu.Items.Add(new ToolStripSeparator()); menu.Items.Add(exit);
        tray=new NotifyIcon { Icon=SystemIcons.Application, Text="WrapCursor：有効", ContextMenuStrip=menu };
        try { hook=new MouseHook(); }
        catch { tray.Dispose(); menu.Dispose(); dispatcher.Dispose(); throw; }
        // Finish the original native mouse event before injecting its wrap move.
        // Injecting inside WH_MOUSE_LL is reentrant and the original event can
        // subsequently overwrite the injected cursor position.
        hook.Moved += (position,own) => {
            if(disposed) return;
            dispatcher.BeginInvoke((Action)(() => { if(!disposed) controller.Process(position,own); }));
        };
        hook.Failed += DisableWithNotice;
        controller.MoveFailed += DisableWithNotice;
        SystemEvents.DisplaySettingsChanged += OnDisplayChanged;
        tray.Visible=true;
    }
    private void UpdateStatus() { enabledItem.Checked=controller.Enabled; tray.Text=controller.Enabled ? "WrapCursor：有効" : "WrapCursor：無効"; }
    private void DisableWithNotice()
    {
        // Avoid doing UI work inside the low-level hook callback.
        controller.Enabled=false;
        if(!disposed) dispatcher.BeginInvoke((Action)(() => { if(disposed) return; UpdateStatus(); tray.ShowBalloonTip(4000,"WrapCursor","カーソルの移動に失敗したため無効にしました。",ToolTipIcon.Warning); }));
    }
    private void OnDisplayChanged(object? sender,EventArgs e)
    {
        if(disposed) return;
        try { dispatcher.BeginInvoke((Action)(() => { if(disposed) return; controller.Reset(); try { controller.Monitors=MonitorProvider.GetBounds(); } catch { controller.Monitors=[]; } })); }
        catch(InvalidOperationException) { }
    }
    protected override void ExitThreadCore() { Dispose(); base.ExitThreadCore(); }
    protected override void Dispose(bool disposing)
    {
        if(disposing && !disposed)
        {
            disposed=true;
            SystemEvents.DisplaySettingsChanged -= OnDisplayChanged;
            hook.Dispose(); tray.Visible=false;
            var menu=tray.ContextMenuStrip; tray.Dispose(); menu?.Dispose(); dispatcher.Dispose();
        }
        base.Dispose(disposing);
    }
}
