using System.Drawing;
using WrapCursor.Core;
int failures = 0, total = 0;
void Check(string name, Rectangle[] monitors, Point before, Point now, Point? expected)
{
    total++;
    var actual = WrapCalculator.GetTarget(monitors, before, now);
    if (actual != expected) { failures++; Console.WriteLine($"FAIL {name}: expected {expected}, got {actual}"); }
}
Rectangle[] one = [new(0,0,100,100)];
Check("right", one, new(98,50), new(99,50), new(1,50));
Check("left", one, new(1,50), new(0,50), new(98,50));
Check("bottom", one, new(50,98), new(50,99), new(50,1));
Check("top", one, new(50,1), new(50,0), new(50,98));
Check("stationary", one, new(99,50), new(99,50), null);
Check("inward", one, new(99,50), new(98,50), null);
Check("interior", one, new(30,50), new(40,50), null);
Rectangle[] horizontal = [new(-100,0,100,100), new(0,0,100,100)];
Check("negative", horizontal, new(98,50), new(99,50), new(-99,50));
Check("seam", horizontal, new(-1,50), new(0,50), null);
Check("vertical", [new(0,-100,100,100),new(0,0,100,100)], new(50,98),new(50,99),new(50,-99));
Check("different heights", [new(-100,0,100,50),new(0,0,100,100)],new(98,75),new(99,75),new(1,75));
Check("gap edge", [new(0,0,100,100),new(200,0,100,100)],new(98,50),new(99,50),null);
Check("gap outer", [new(0,0,100,100),new(200,0,100,100)],new(298,50),new(299,50),new(1,50));
Check("corner", one,new(98,98),new(99,99),new(1,99));
Check("none", [],new(98,50),new(99,50),null);
Check("outside", one,new(98,50),new(110,50),new(1,50));
Check("outside diagonal",one,new(98,98),new(110,110),new(1,99));
Check("already at right edge outward",one,new(99,50),new(110,50),new(1,50));
Check("width two", [new(0,0,2,100)],new(0,50),new(1,50),new(0,50));
Check("width one", [new(0,0,1,100)],new(-1,50),new(0,50),null);
void StateCheck(string name, bool condition) { total++; if (!condition) { failures++; Console.WriteLine("FAIL " + name); } }
var moves = new List<Point>();
var controller = new WrapController(p => { moves.Add(p); return true; }) { Monitors = one };
controller.Process(new(98,50)); controller.Process(new(99,50));
StateCheck("controller moves", moves.SequenceEqual(new[] { new Point(1,50) }));
moves.Clear(); controller.Process(new(1,50),true); controller.Process(new(2,50));
StateCheck("own event no bounce", moves.Count == 0);
controller.Enabled=false; controller.Process(new(98,50)); controller.Process(new(99,50));
StateCheck("disabled", moves.Count==0);
controller.Enabled=true; controller.Reset(); controller.Process(new(99,50));
StateCheck("reset first event", moves.Count==0);
controller.Process(new(98,50)); controller.Process(new(99,50));
StateCheck("reenabled", moves.Count==1);
int notifications=0;
var failing = new WrapController(_ => false) { Monitors=one };
failing.MoveFailed += () => notifications++;
failing.Process(new(98,50)); failing.Process(new(99,50));
StateCheck("failure disables and notifies", !failing.Enabled && notifications==1);
Console.WriteLine($"{total-failures}/{total} passed");
return failures == 0 ? 0 : 1;
