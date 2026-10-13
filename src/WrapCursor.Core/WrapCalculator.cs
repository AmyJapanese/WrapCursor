using System.Drawing;
namespace WrapCursor.Core;
public static class WrapCalculator
{
    public static Point? GetTarget(IReadOnlyList<Rectangle> monitors, Point previous, Point current)
    {
        var valid = monitors.Where(r => r.Width > 0 && r.Height > 0).ToArray();
        if (valid.Length == 0) return null;
        long dx=(long)current.X-previous.X, dy=(long)current.Y-previous.Y;
        if (!valid.Any(r => r.Contains(current)))
        {
            var raw=current;
            current=valid.Select(r => new Point(Math.Clamp(raw.X,r.Left,r.Right-1),Math.Clamp(raw.Y,r.Top,r.Bottom-1)))
                .MinBy(p => Math.Pow((double)p.X-raw.X,2)+Math.Pow((double)p.Y-raw.Y,2));
        }
        var rows = valid.Where(r => current.Y >= r.Top && current.Y < r.Bottom).ToArray();
        if (rows.Length > 0)
        {
            var left = rows.MinBy(r => r.Left);
            var right = rows.MaxBy(r => r.Right);
            if (dx > 0 && current.X >= right.Right - 1)
                return Different(new Point(left.Left + Inset(left.Width),current.Y), current);
            if (dx < 0 && current.X <= left.Left)
                return Different(new Point(right.Right - 1 - Inset(right.Width),current.Y), current);
        }
        var columns = valid.Where(r => current.X >= r.Left && current.X < r.Right).ToArray();
        if (columns.Length > 0)
        {
            var top = columns.MinBy(r => r.Top);
            var bottom = columns.MaxBy(r => r.Bottom);
            if (dy > 0 && current.Y >= bottom.Bottom - 1)
                return Different(new Point(current.X,top.Top + Inset(top.Height)),current);
            if (dy < 0 && current.Y <= top.Top)
                return Different(new Point(current.X,bottom.Bottom - 1 - Inset(bottom.Height)),current);
        }
        return null;
    }
    private static Point? Different(Point target, Point current) => target == current ? null : target;
    private static int Inset(int size) => size > 2 ? 1 : 0;
}
