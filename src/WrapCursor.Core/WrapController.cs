using System.Drawing;
namespace WrapCursor.Core;
public sealed class WrapController
{
    private readonly Func<Point,bool> move;
    private Point? previous;
    private bool enabled = true;
    public WrapController(Func<Point,bool> move) { this.move = move; }
    public bool Enabled { get => enabled; set { enabled=value; Reset(); } }
    public Rectangle[] Monitors { get; set; } = [];
    public void Reset() { previous=null; }
    public void Process(Point position, bool ownEvent = false)
    {
        if (ownEvent || !Enabled) { previous=position; return; }
        var before = previous;
        previous=position;
        if (before is null) return;
        var target=WrapCalculator.GetTarget(Monitors,before.Value,position);
        if (target is null) return;
        previous=target;
        if (!move(target.Value)) { Enabled=false; MoveFailed?.Invoke(); }
    }
    public event Action? MoveFailed;
}
