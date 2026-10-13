namespace WrapCursor;
internal static class MonitorProvider
{
    internal static Rectangle[] GetBounds() => Screen.AllScreens.Select(s => s.Bounds).ToArray();
}
