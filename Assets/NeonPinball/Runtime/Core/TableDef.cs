// Masa gövdesi: kemer, duvarlar, launch lane, flipper kılavuzları, sling'ler (TS levels/table.ts).
using System;
using System.Collections.Generic;

namespace NeonPinball
{
    public class SlingDef { public string id; public V2 a, b, normal, apex; }

    public class TableDef
    {
        public LevelConfig level;
        public ThemeDef theme;
        public List<WallDef> walls = new List<WallDef>();
        public List<SlingDef> slings = new List<SlingDef>();
    }

    public static class TableBuilder
    {
        public const double Left = 0.015, Right = 0.905, CenterX = 0.46, ArchR = 0.485, ArchCx = 0.5, ArchCy = 0.485;

        static V2 V(double x, double y) => new V2(x, y);
        static WallDef W(V2 a, V2 b, string style, double thickness = -1, V2? oneWay = null) =>
            new WallDef { a = a, b = b, style = style, thickness = thickness, restitution = -1, hasOneWay = oneWay.HasValue, oneWay = oneWay ?? default };

        static void LeftGuide(out V2 start, out V2 end)
        {
            double a = FlipperC.GuideDeg * Geo.Deg;
            end = V(FlipperC.LeftPivotX + FlipperC.PivotRadius * Math.Sin(a), FlipperC.PivotY - FlipperC.PivotRadius * Math.Cos(a));
            double len = (end.x - Left) / Math.Cos(a);
            start = V(Left, end.y - len * Math.Sin(a));
        }

        static List<WallDef> BaseWalls()
        {
            var walls = new List<WallDef>();
            const int segments = 44;
            V2 prev = V(ArchCx + ArchR, ArchCy);
            for (int i = 1; i <= segments; i++)
            {
                double ang = (double)i / segments * Math.PI;
                V2 p = V(ArchCx + ArchR * Math.Cos(ang), ArchCy - ArchR * Math.Sin(ang));
                walls.Add(W(prev, p, "wall", 0.006));
                prev = p;
            }
            LeftGuide(out V2 gs, out V2 ge);
            walls.Add(W(V(Left, 0.485), gs, "wall"));
            walls.Add(W(gs, ge, "guide"));
            double Mirror(double x) => 2 * CenterX - x;
            walls.Add(W(V(Right, gs.y), V(Mirror(ge.x), ge.y), "guide"));

            double laneBottom = PlungerC.RestY + 0.03;
            walls.Add(W(V(Right, laneBottom), V(Right, 0.52), "wall"));
            walls.Add(W(V(0.985, 0.485), V(0.985, laneBottom), "wall"));
            walls.Add(W(V(Right, laneBottom), V(0.985, laneBottom), "hidden"));

            V2 gateA = V(Right, 0.52), gateB = V(0.982, 0.45);
            double dx = gateB.x - gateA.x, dy = gateB.y - gateA.y, l = Geo.Hypot(dx, dy);
            double nx = dy / l, ny = -dx / l;
            walls.Add(W(gateA, gateB, "rail", -1, nx < 0 ? V(nx, ny) : V(-nx, -ny)));
            return walls;
        }

        public static TableDef Build(LevelConfig level)
        {
            var t = new TableDef { level = level, theme = LevelDb.Theme(level.themeId) };
            t.walls = BaseWalls();
            if (level.slings)
            {
                double Mirror(double x) => 2 * CenterX - x;
                V2 la = V(0.12, 1.2), lb = V(0.205, 1.4), lapex = V(0.12, 1.4);
                V2 ra = V(Mirror(la.x), la.y), rb = V(Mirror(lb.x), lb.y), rapex = V(Mirror(lapex.x), lapex.y);
                t.slings.Add(new SlingDef { id = "sling-l", a = la, b = lb, apex = lapex, normal = V(0.92, -0.391) });
                t.slings.Add(new SlingDef { id = "sling-r", a = ra, b = rb, apex = rapex, normal = V(-0.92, -0.391) });
                foreach (var (a, b, apex) in new[] { (la, lb, lapex), (ra, rb, rapex) })
                {
                    t.walls.Add(W(a, apex, "hidden"));
                    t.walls.Add(W(apex, b, "hidden"));
                }
            }
            t.walls.AddRange(level.walls);
            return t;
        }
    }
}
