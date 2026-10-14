// Masanın değişmeyen kısmı: zemin, dünya deseni, duvarlar, direkler, rampa gövdeleri, launch lane ve elemanların "boşta" hâli.
// Üç ayrı mesh: playfield maskesi, maskeye kırpılan (zemin+desen) ve kırpılmayan (duvar, eleman). TS'deki clipPath karşılığı stencil ile yapılır.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonPinball
{
    public class StaticLayer
    {
        public readonly MeshBatch mask = new MeshBatch();
        public readonly MeshBatch clipped = new MeshBatch();
        public readonly MeshBatch solid = new MeshBatch();

        static Vector2 P(double x, double y) => new Vector2((float)x, (float)y);

        public static List<Vector2> PlayfieldPolygon()
        {
            var pts = new List<Vector2>();
            double cx = TableBuilder.ArchCx, cy = TableBuilder.ArchCy, R = TableBuilder.ArchR;
            const int n = 44;
            pts.Add(P(cx + R, cy));
            for (int i = 1; i <= n; i++)
            {
                double a = (double)i / n * Math.PI;
                pts.Add(P(cx + R * Math.Cos(a), cy - R * Math.Sin(a)));
            }
            pts.Add(P(TableBuilder.Left, TableC.Height));
            pts.Add(P(cx + R, TableC.Height));
            return pts;
        }

        public void Build(TableDef table)
        {
            mask.Clear(); clipped.Clear(); solid.Clear();
            var th = table.theme;
            bool classic = th.look == "classic";

            var gm = new Gfx(mask);
            var poly = PlayfieldPolygon();
            gm.FillConvex(poly, Hex.C("#ffffff"), false);

            var gc = new Gfx(clipped) { flat = classic };
            gc.VGradient(0, 0, 1, TableC.Height, th.bg, new[] { 0.0, 0.55, 1.0 });
            Decor.Paint(gc, th, new Region(0, 0, 1, TableC.Height));
            DrawCoreDecor(gc, table);
            var t = Hex.C("#00000000"); var dark = Hex.C("#000000aa");
            gc.RadialGradient(0.5, 0.9, 1.1, t, t, dark, dark, 0.55, 1.0, 0, 0, 1.0, false);
            DrawLaneBed(gc);
            DrawRampBodies(gc, table);

            var gs = new Gfx(solid) { flat = classic };
            DrawWalls(gs, table);
            DrawPosts(gs, table);
            foreach (var s in table.slings) Elements.Sling(gs, th, s, 0);
            foreach (var b in table.level.bumpers) Elements.Bumper(gs, th, b, 0);
            foreach (var tg in table.level.targets) Elements.Target(gs, th, tg, false, 0);
            foreach (var l in table.level.lanes) Elements.LaneLight(gs, th, l, false);
            gs.VGradient(TableBuilder.Left, FlipperC.PivotY + 0.05, TableBuilder.Right - TableBuilder.Left, TableC.Height - FlipperC.PivotY - 0.05,
                new[] { "#ff3b4a00", "#ff3b4a33" }, new[] { 0.0, 1.0 });
        }

        static void DrawWalls(Gfx g, TableDef table)
        {
            var th = table.theme; bool classic = th.look == "classic";
            string rail = classic ? th.secondary : th.Swatch("cyan").glow;
            string railCore = classic ? "#ffe2b0" : th.Swatch("cyan").core;
            foreach (var w in table.walls)
            {
                string style = w.style ?? "wall";
                if (style == "hidden") continue;
                if (style == "rail") g.GlowLine(w.a.x, w.a.y, w.b.x, w.b.y, rail, railCore, 0.007, 0.8);
                else if (style == "guide") g.GlowLine(w.a.x, w.a.y, w.b.x, w.b.y, th.wall, "#e8e0ff", 0.011, 0.9);
                else g.GlowLine(w.a.x, w.a.y, w.b.x, w.b.y, th.wall, "#e8e0ff", 0.009, 0.85);
            }
        }

        static void DrawLaneBed(Gfx g)
        {
            g.Rect(PlungerC.LaneMinX, 0.5, PlungerC.LaneMaxX - PlungerC.LaneMinX, PlungerC.RestY + 0.03 - 0.5, "#000000", 0.45);
            for (double y = 0.6; y < 1.5; y += 0.09) g.Line(PlungerC.RestX, y, PlungerC.RestX, y + 0.04, 0.004, "#ffffff", 0.08);
        }

        static void DrawPosts(Gfx g, TableDef table)
        {
            var th = table.theme;
            foreach (var p in table.level.posts)
            {
                var n = th.Swatch(string.IsNullOrEmpty(p.color) ? "violet" : p.color);
                g.GlowCircle(p.x, p.y, p.r, n.glow, 0.5);
                g.Circle(p.x, p.y, p.r, "#9aa3c7");
                g.Ring(p.x, p.y, p.r, 0.004, n.glow, 0.9);
                g.Circle(p.x - p.r * 0.3, p.y - p.r * 0.3, p.r * 0.35, "#ffffff", 0.75);
            }
        }

        static void DrawRampBodies(Gfx g, TableDef table)
        {
            var th = table.theme;
            var pts = new List<Vector2>();
            foreach (var r in table.level.ramps)
            {
                var n = th.Swatch(r.color);
                pts.Clear();
                foreach (var p in r.path) pts.Add(P(p.x, p.y));
                g.Strip(pts, r.width * 1.5, n.dark, 0.8);
                g.Strip(pts, r.width * 1.5, n.glow, 0.12);
                foreach (double off in new[] { -r.width * 0.75, r.width * 0.75 })
                {
                    var rp = new List<Vector2>();
                    for (int i = 0; i < r.path.Count; i++)
                    {
                        var a = r.path[Math.Max(0, i - 1)]; var b = r.path[Math.Min(r.path.Count - 1, i + 1)];
                        double dx = b.x - a.x, dy = b.y - a.y, l = Math.Sqrt(dx * dx + dy * dy); if (l == 0) l = 1;
                        rp.Add(P(r.path[i].x + (-dy / l) * off, r.path[i].y + (dx / l) * off));
                    }
                    if (!(th.look == "classic")) g.Strip(rp, 0.012, n.glow, 0.25);
                    g.Strip(rp, 0.004, th.look == "classic" ? "#d6dcea" : n.core, 0.9);
                }
            }
        }

        static void DrawCoreDecor(Gfx g, TableDef table)
        {
            var th = table.theme; var core = table.level.hasCore ? table.level.core : null;
            double cx = core != null ? core.x : TableBuilder.CenterX, cy = core != null ? core.y : 0.86;
            double r = core != null ? core.r * 1.9 : 0.16;
            bool classic = th.look == "classic";
            g.Ring(cx, cy, r, 0.012, th.core, classic ? 0.28 : 0.1);
            g.Ring(cx, cy, r * 0.72, 0.006, th.core, classic ? 0.3 : 0.14);
            for (int i = 0; i < 12; i++)
            {
                double a = (double)i / 12 * Math.PI * 2;
                g.Line(cx + Math.Cos(a) * r * 0.78, cy + Math.Sin(a) * r * 0.78, cx + Math.Cos(a) * r * 1.08, cy + Math.Sin(a) * r * 1.08, 0.004, th.core, classic ? 0.3 : 0.14);
            }
        }

        /// <summary>Arka cam: tüm ekranı dünya deseniyle kaplar; üstte silik dünya amblemi.</summary>
        public static void BuildScene(MeshBatch into, ThemeDef th, Region region, double ex, double ey, double er)
        {
            into.Clear();
            var g = new Gfx(into);
            g.VGradient(region.x, region.y, region.w, region.h, new[] { th.outer[0], th.bg[1], th.outer[1] }, new[] { 0.0, 0.5, 1.0 });
            Decor.Paint(g, th, region);
            Glyphs.Draw(g, th.glyph, ex, ey, er, th.primary, 0.16);
            g.Ring(ex, ey, er * 1.25, 0.004, th.primary, 0.1);
            g.VGradient(region.x, region.y, region.w, region.h, new[] { "#000000aa", "#00000000", "#00000066" }, new[] { 0.0, 0.4, 1.0 });
        }
    }
}
