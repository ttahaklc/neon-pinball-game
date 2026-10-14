// Top kaplamaları (TS skins.ts): dünyaya göre 11 görünüm. Fizik daima daireseldir; yalnızca görünüm değişir.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonPinball
{
    public static class Skins
    {
        static readonly double[] Stone = { 1.0, 0.9, 1.04, 0.88, 1.0, 0.92, 1.05, 0.9 };
        static readonly List<Vector2> ring = new List<Vector2>(16);

        static void Polygon(Gfx g, double cx, double cy, double[] radii, double scale, double rot, string hex, double alpha, string strokeHex = null, double strokeW = 0)
        {
            ring.Clear();
            int n = radii.Length;
            for (int i = 0; i < n; i++)
            {
                double a = rot + (double)i / n * Math.PI * 2;
                ring.Add(new Vector2((float)(cx + Math.Cos(a) * radii[i] * scale), (float)(cy + Math.Sin(a) * radii[i] * scale)));
            }
            g.FillFan(cx, cy, ring, Hex.C(hex, alpha));
            if (strokeHex != null) g.Strip(ring, strokeW, strokeHex, 1, true);
        }

        public static void Ball(Gfx g, string kind, double x, double y, double r, double a, double t, bool lite)
        {
            g.Circle(x + r * 0.35, y + r * 0.45, r, "#000000", 0.35);
            switch (kind)
            {
                case "steel":
                    g.Sphere(x, y, r, new[] { "#ffffff", "#b8c4e8", "#4a5580", "#1b2040" });
                    g.Ring(x, y, r, 0.002, "#e8f0ff", 0.6);
                    break;
                case "stone":
                    Polygon(g, x, y, Stone, r * 1.02, a, "#7d725f", 1, "#3b3428", r * 0.1);
                    Polygon(g, x - r * 0.12, y - r * 0.14, Stone, r * 0.6, a, "#a69a82", 0.8);
                    g.Line(x + Math.Cos(a) * r * 0.1, y + Math.Sin(a) * r * 0.1, x + Math.Cos(a + 2.2) * r * 0.8, y + Math.Sin(a + 2.2) * r * 0.8, r * 0.07, "#2a241a", 0.8);
                    break;
                case "bubble":
                    g.Circle(x, y, r, "#46c8ff", 0.3);
                    g.Ring(x, y, r * 0.96, r * 0.1, "#c8f4ff", 0.9);
                    g.Circle(x - r * 0.35, y - r * 0.38, r * 0.2, "#ffffff", 0.85);
                    g.Arc(x, y, r * 0.62, 20 * Math.PI / 180, 70 * Math.PI / 180, r * 0.08, "#ffffff", 0.6);
                    break;
                case "lava":
                    if (!lite) g.Circle(x, y, r * (1.7 + 0.12 * Math.Sin(t * 9)), "#ff6a1c", 0.22);
                    g.Circle(x, y, r, "#2a0d06");
                    for (int i = 0; i < 3; i++)
                    {
                        double ang = a + i * Math.PI * 2 / 3;
                        g.Line(x, y, x + Math.Cos(ang) * r * 0.9, y + Math.Sin(ang) * r * 0.9, r * 0.16, "#ff8a1c", 0.95);
                    }
                    g.Circle(x, y, r * 0.38, "#ffd23d", 0.9);
                    g.Ring(x, y, r, 0.003, "#ff6a1c", 0.9);
                    break;
                case "ice":
                {
                    var six = new[] { 1.08, 1.08, 1.08, 1.08, 1.08, 1.08 };
                    Polygon(g, x, y, six, r, a, "#9fdcff", 1, "#ffffff", r * 0.08);
                    Polygon(g, x, y, six, r * 0.55 / 1.08, a + Math.PI / 6, "#e8fbff", 0.7);
                    for (int i = 0; i < 3; i++)
                    {
                        double ang = a + i * Math.PI / 3;
                        g.Line(x - Math.Cos(ang) * r, y - Math.Sin(ang) * r, x + Math.Cos(ang) * r, y + Math.Sin(ang) * r, r * 0.05, "#ffffff", 0.6);
                    }
                    break;
                }
                case "volt":
                    if (!lite) g.Circle(x, y, r * 1.6, "#ffe14d", 0.14 + 0.08 * Math.Sin(t * 30));
                    g.Circle(x, y, r, "#0b1030");
                    g.Ring(x, y, r * 0.94, r * 0.12, "#ffe14d", 0.95);
                    for (int i = 0; i < 3; i++)
                    {
                        double ang = a * 2 + i * Math.PI * 2 / 3;
                        double x1 = x + Math.Cos(ang) * r * 0.2, y1 = y + Math.Sin(ang) * r * 0.2;
                        double x2 = x + Math.Cos(ang + 0.5) * r * 0.55, y2 = y + Math.Sin(ang + 0.5) * r * 0.55;
                        double x3 = x + Math.Cos(ang - 0.3) * r * 0.85, y3 = y + Math.Sin(ang - 0.3) * r * 0.85;
                        g.Line(x1, y1, x2, y2, r * 0.1, "#fff6a8", 0.95);
                        g.Line(x2, y2, x3, y3, r * 0.1, "#fff6a8", 0.95);
                    }
                    break;
                case "gold":
                    g.Sphere(x, y, r, new[] { "#fff6c8", "#ffd24d", "#a8741a", "#4a2c06" });
                    g.Box(x, y, r, r * 0.12, 0, a, Hex.C("#fff3b0", 0.35));
                    g.Ring(x, y, r, 0.003, "#ffe9a8", 0.8);
                    break;
                case "eye":
                    g.Sphere(x, y, r, new[] { "#ffe6a8", "#ffc23d", "#c8283c", "#3a0610" });
                    g.FillEllipse(x, y, r * 0.17, r * 0.68, a + Math.PI / 2 * 0, "#0a0204", 1);
                    g.Ring(x, y, r, 0.003, "#ffd24d", 0.9);
                    break;
                case "chip":
                    g.Box(x, y, r * 0.8, r * 0.8, r * 0.2, a, Hex.C("#06283a"));
                    g.BoxOutline(x, y, r * 0.8, r * 0.8, r * 0.2, a, r * 0.12, Hex.C("#18d6ff"));
                    g.Box(x, y, r * 0.3, r * 0.3, 0, a, Hex.C("#7ff6ff", 0.9));
                    foreach (double s in new[] { -0.35, 0.35 })
                    {
                        double cs = Math.Cos(a), sn = Math.Sin(a);
                        System.Action<double, double, double, double> pin = (lx1, ly1, lx2, ly2) =>
                            g.Line(x + lx1 * cs - ly1 * sn, y + lx1 * sn + ly1 * cs, x + lx2 * cs - ly2 * sn, y + lx2 * sn + ly2 * cs, r * 0.1, "#18d6ff", 1);
                        pin(s * r, -r * 0.8, s * r, -r * 1.05);
                        pin(s * r, r * 0.8, s * r, r * 1.05);
                    }
                    break;
                case "planet":
                    g.Sphere(x, y, r * 0.82, new[] { "#f0dcff", "#b07bff", "#4a2a8a", "#150a30" });
                    g.Ellipse(x, y, r * 1.15, r * 0.28, a * 0.5 - 20 * Math.PI / 180, r * 0.1, "#ffe9a8", 0.9);
                    break;
                case "plasma":
                    if (!lite) g.Circle(x, y, r * 1.8, "#ffb81c", 0.16 + 0.06 * Math.Sin(t * 12));
                    g.Circle(x, y, r * 1.15, "#ff2fd0", 0.35);
                    g.Circle(x, y, r * 0.9, "#ffffff", 1);
                    g.Circle(x, y, r * 0.55, "#fff3b0", 1);
                    g.Circle(x + Math.Cos(t * 7) * r * 1.2, y + Math.Sin(t * 7) * r * 1.2, r * 0.2, "#18d6ff", 0.95);
                    break;
            }
        }
    }
}
