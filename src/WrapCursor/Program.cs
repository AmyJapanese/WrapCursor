namespace WrapCursor;
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex=new Mutex(true,@"Local\WrapCursor.App",out bool first);
        if(!first) return;
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        try { using var context=new TrayApplicationContext(); Application.Run(context); }
        catch(Exception ex) { MessageBox.Show("WrapCursorを起動できませんでした。\n"+ex.Message,"WrapCursor",MessageBoxButtons.OK,MessageBoxIcon.Error); }
        finally { mutex.ReleaseMutex(); }
    }
}
