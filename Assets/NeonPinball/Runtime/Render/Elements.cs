// Masa elemanlarının çizimi (TS elements.ts): bumper, hedef, drop target, şerit, sling. neon / classic görünüm.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonPinball
{
    public static class Elements
    {
        const string Chrome = "#d6dcea", Ink = "#05030f";
        static readonly List<Vector2> tri = new List<Vector2>(3);

        public static void Bumper(Gfx g, ThemeDef th, BumperDef b, double f)
        {
            var n = th.Swatch(b.color);
            double r = b.r * (1 + f * 0.1);
            if (th.look == "classic")
            {
                g.Circle(b.x + r * 0.08, b.y + r * 0.12, r * 1.05, "#000000", 0.4);
                g.Circle(b.x, b.y, r, Chrome);
                g.Ring(b.x, b.y, r, r * 0.06, "#7e869c", 1);
                g.Circle(b.x, b.y, r * 0.8, f > 0.05 ? n.core : n.glow);
                g.Circle(b.x - r * 0.18, b.y - r * 0.22, r * 0.5, n.core, 0.55);
                g.Circle(b.x, b.y, r * 0.3, f > 0.05 ? "#ffffff" : n.dark, 0.9);
                return;
            }
            g.GlowCircle(b.x, b.y, r, n.glow, 0.5 + f * 0.9);
            g.Circle(b.x, b.y, r, "#07041a");
            g.Ring(b.x, b.y, r * 0.9, r * 0.2, n.glow, 0.95);
            g.Circle(b.x, b.y, r * 0.62, n.dark);
            g.Circle(b.x, b.y, r * 0.5 * (1 + f * 0.25), f > 0.05 ? "#ffffff" : n.core, 0.55 + f * 0.45);
            g.Ring(b.x, b.y, r, r * 0.07, n.core, 0.9);
        }

        public static void Target(Gfx g, ThemeDef th, TargetDef t, bool lit, double f)
        {
            var n = th.Swatch(t.color);
            if (th.look == "classic")
            {
                g.Line(t.a.x, t.a.y, t.b.x, t.b.y, 0.032, Ink, 1);
                g.Line(t.a.x, t.a.y, t.b.x, t.b.y, 0.024, lit || f > 0 ? n.core : n.glow, lit || f > 0 ? 1 : 0.85);
                g.Line(t.a.x, t.a.y, t.b.x, t.b.y, 0.006, "#ffffff", lit ? 0.9 : 0.35);
                return;
            }
            g.Line(t.a.x, t.a.y, t.b.x, t.b.y, 0.03, Ink, 0.9);
            g.Line(t.a.x, t.a.y, t.b.x, t.b.y, 0.026, n.dark, 1);
            if (lit || f > 0) g.GlowLine(t.a.x, t.a.y, t.b.x, t.b.y, n.glow, f > 0 ? "#ffffff" : n.core, 0.02, 1);
            else g.Line(t.a.x, t.a.y, t.b.x, t.b.y, 0.016, n.glow, 0.45);
        }

        public static void DropTarget(Gfx g, ThemeDef th, TargetDef d, bool down)
        {
            var n = th.Swatch(d.color);
            if (down) { g.Line(d.a.x, d.a.y, d.b.x, d.b.y, 0.012, n.dark, 0.5); return; }
            g.Line(d.a.x, d.a.y, d.b.x, d.b.y, 0.034, Ink, 0.9);
            g.Line(d.a.x, d.a.y, d.b.x, d.b.y, 0.028, n.glow, 1);
            g.Line(d.a.x, d.a.y, d.b.x, d.b.y, 0.012, th.look == "classic" ? Chrome : n.core, 0.9);
        }

        public static void LaneLight(Gfx g, ThemeDef th, LaneDef l, bool lit)
        {
            var n = th.Swatch(l.color);
            if (lit) g.GlowCircle(l.x, l.y, l.r, n.glow, 0.9);
            g.Circle(l.x, l.y, l.r, lit ? n.glow : n.dark, lit ? 0.95 : 0.8);
            g.Ring(l.x, l.y, l.r, 0.004, th.look == "classic" ? Chrome : n.core, lit ? 1 : 0.55);
            g.Circle(l.x, l.y, l.r * 0.35, lit ? "#ffffff" : n.glow, lit ? 0.95 : 0.35);
        }

        public static void Sling(Gfx g, ThemeDef th, SlingDef s, double f)
        {
            var m = th.Swatch("magenta");
            tri.Clear();
            tri.Add(new Vector2((float)s.a.x, (float)s.a.y)); tri.Add(new Vector2((float)s.b.x, (float)s.b.y)); tri.Add(new Vector2((float)s.apex.x, (float)s.apex.y));
            g.FillConvex(tri, th.look == "classic" ? "#3a1d6a" : m.dark, 0.95, false);
            g.Strip(tri, 0.006, m.glow, 0.7, true);
            var am = th.Swatch("amber");
            g.GlowLine(s.a.x, s.a.y, s.b.x, s.b.y, am.glow, am.core, 0.012, 0.5 + f);
        }
    }
}
