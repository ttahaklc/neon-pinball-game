// Dünya arka plan desenleri (TS decor.ts). Bir kez (statik mesh'e) çizilir; bir dikdörtgen bölgeye dağılır.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonPinball
{
    public struct Region { public double x, y, w, h; public Region(double x, double y, double w, double h) { this.x = x; this.y = y; this.w = w; this.h = h; } }

    public static class Decor
    {
        static int Round(double v) => (int)Math.Floor(v + 0.5);
        static Vector2 P(double x, double y) => new Vector2((float)x, (float)y);

        public static void Paint(Gfx g, ThemeDef th, Region R)
        {
            var rng = new Rng((uint)th.seed);
            Func<double> rnd = rng.Next;
            double area = (R.w * R.h) / 1.8;
            Func<double, int> n = bas => Math.Max(3, Round(bas * area));
            Func<double> rx = () => R.x + rnd() * R.w;
            Func<double> ry = () => R.y + rnd() * R.h;
            double bottom = R.y + R.h;
            double cx = R.x + R.w / 2;
            var pts = new List<Vector2>(64);

            switch (th.decor)
            {
                case "strata":
                {
                    int cnt = n(16);
                    for (int i = 0; i < cnt; i++)
                    {
                        double y0 = R.y + (double)i / cnt * R.h;
                        pts.Clear();
                        for (double x = R.x; x <= R.x + R.w + 0.05; x += 0.05) pts.Add(P(x, y0 + Math.Sin(x * 9 + i) * 0.015 + Math.Sin(x * 23 + i * 2) * 0.006));
                        g.Strip(pts, 0.004, i % 3 == 0 ? th.secondary : th.wall, 0.09);
                    }
                    for (int i = 0, m = n(20); i < m; i++)
                    {
                        double x = rx(), y = ry(), s = 0.03 + rnd() * 0.07;
                        pts.Clear(); pts.Add(P(x, y - s)); pts.Add(P(x + s * 0.6, y + s * 0.4)); pts.Add(P(x - s * 0.5, y + s * 0.5));
                        g.FillConvex(pts, i % 2 == 1 ? th.wall : th.secondary, 0.07, false);
                    }
                    for (int i = 0, m = n(40); i < m; i++) g.Circle(rx(), ry(), 0.003 + rnd() * 0.005, th.primary, 0.14);
                    break;
                }
                case "waves":
                {
                    int rows = n(22);
                    for (int row = 0; row < rows; row++)
                    {
                        double y0 = R.y + (double)row / rows * R.h;
                        pts.Clear();
                        for (double x = R.x; x <= R.x + R.w + 0.04; x += 0.04) pts.Add(P(x, y0 + Math.Sin(x * 11 + row * 0.8) * 0.018));
                        g.Strip(pts, 0.003, row % 2 == 1 ? th.primary : th.secondary, 0.1 + 0.04 * (row % 3));
                    }
                    for (int i = 0, m = n(26); i < m; i++) g.Ring(rx(), ry(), 0.008 + rnd() * 0.022, 0.0025, th.accent, 0.18);
                    for (int i = 0; i < 4; i++)
                    {
                        double x = R.x + rnd() * R.w;
                        pts.Clear(); pts.Add(P(x, R.y)); pts.Add(P(x + 0.12, bottom)); pts.Add(P(x - 0.06, bottom));
                        g.FillConvex(pts, th.accent, 0.035, false);
                    }
                    break;
                }
                case "embers":
                {
                    g.Radial(cx, bottom, R.w * 0.9, th.primary, 0.3);
                    for (int i = 0, m = n(9); i < m; i++)
                    {
                        double x = rx(), y = bottom - rnd() * R.h * 0.6;
                        pts.Clear(); pts.Add(P(x, y));
                        for (int k = 0; k < 5; k++) { x += (rnd() - 0.5) * 0.14; y -= 0.04 + rnd() * 0.07; pts.Add(P(x, y)); }
                        g.Strip(pts, 0.012, th.secondary, 0.07);
                        g.Strip(pts, 0.004, th.secondary, 0.32);
                    }
                    for (int i = 0, m = n(70); i < m; i++) g.Circle(rx(), ry(), 0.002 + rnd() * 0.004, i % 3 != 0 ? th.secondary : th.accent, 0.2 + rnd() * 0.45);
                    break;
                }
                case "snow":
                {
                    for (int i = 0, m = n(16); i < m; i++)
                    {
                        double x = rx(), y = ry(), s = 0.02 + rnd() * 0.03;
                        for (int k = 0; k < 3; k++)
                        {
                            double a = k * Math.PI / 3 + i;
                            g.Line(x - Math.Cos(a) * s, y - Math.Sin(a) * s, x + Math.Cos(a) * s, y + Math.Sin(a) * s, 0.0028, th.accent, 0.28);
                        }
                    }
                    for (int i = 0, m = n(60); i < m; i++) g.Circle(rx(), ry(), 0.002 + rnd() * 0.003, "#ffffff", 0.15 + rnd() * 0.3);
                    for (int i = 0, m = n(10); i < m; i++)
                    {
                        double x = rx(), y = ry(), s = 0.05 + rnd() * 0.08;
                        pts.Clear(); pts.Add(P(x, y - s)); pts.Add(P(x + s * 0.5, y)); pts.Add(P(x, y + s * 0.6)); pts.Add(P(x - s * 0.45, y));
                        g.Strip(pts, 0.003, th.primary, 0.16, true);
                    }
                    break;
                }
                case "storm":
                {
                    for (int i = 0, m = n(12); i < m; i++)
                    {
                        double x = rx(), y = R.y + rnd() * R.h * 0.7;
                        for (int k = 0; k < 4; k++) g.Circle(x + (k - 1.5) * 0.06, y + (k % 2) * 0.02, 0.05 + rnd() * 0.04, th.secondary, 0.045);
                    }
                    int bolts = Math.Max(1, Round(2 * area));
                    for (int b = 0; b < bolts; b++)
                    {
                        double x = R.x + R.w * (0.2 + 0.6 * rnd()), y = R.y + 0.05;
                        pts.Clear(); pts.Add(P(x, y));
                        while (y < bottom * 0.75) { x += (rnd() - 0.5) * 0.13; y += 0.06 + rnd() * 0.06; pts.Add(P(x, y)); }
                        g.Strip(pts, 0.016, th.primary, 0.06);
                        g.Strip(pts, 0.004, th.primary, 0.3);
                    }
                    break;
                }
                case "dunes":
                {
                    g.Circle(R.x + R.w * 0.7, R.y + R.h * 0.2, 0.13, th.primary, 0.11);
                    g.Circle(R.x + R.w * 0.7, R.y + R.h * 0.2, 0.09, th.accent, 0.1);
                    for (int layer = 0; layer < 4; layer++)
                    {
                        double bas = R.y + R.h * (0.45 + layer * 0.14);
                        pts.Clear();
                        for (double x = R.x; x <= R.x + R.w + 0.05; x += 0.05) pts.Add(P(x, bas + Math.Sin(x * 5 + layer * 1.7) * 0.05));
                        g.FillBelow(pts, bottom, layer % 2 == 1 ? th.secondary : th.primary, 0.07);
                    }
                    for (int i = 0, m = n(30); i < m; i++) g.Circle(rx(), R.y + rnd() * R.h * 0.4, 0.002 + rnd() * 0.002, "#ffffff", 0.3);
                    break;
                }
                case "scales":
                {
                    double r = 0.06;
                    for (int row = 0; row * r * 0.55 < R.h + r; row++)
                        for (int col = -1; col * r * 2 < R.w + r * 2; col++)
                        {
                            double x = R.x + col * r * 2 + (row % 2) * r, y = R.y + row * r * 0.55;
                            g.Arc(x, y, r, 0, Math.PI, 0.0028, row % 4 == 0 ? th.secondary : th.primary, 0.12 + 0.04 * (row % 3));
                        }
                    g.Radial(cx, R.y + R.h * 0.5, R.w * 0.7, th.primary, 0.14);
                    break;
                }
                case "cyber":
                {
                    for (int i = 0; i <= 14; i++)
                    {
                        double x = R.x + (double)i / 14 * R.w;
                        g.Line(cx + (x - cx) * 0.2, R.y + R.h * 0.12, cx + (x - cx) * 1.9, bottom, 0.002, th.primary, 0.13);
                    }
                    for (int j = 0; j < 16; j++)
                    {
                        double t = j / 15.0, y = R.y + R.h * 0.12 + (R.h * 0.88) * t * t;
                        g.Line(R.x, y, R.x + R.w, y, 0.002, th.secondary, 0.08 + t * 0.1);
                    }
                    for (int i = 0, m = n(18); i < m; i++)
                    {
                        double x = rx(), y = ry();
                        pts.Clear(); pts.Add(P(x, y));
                        for (int k = 0; k < 4; k++)
                        {
                            if (rnd() > 0.5) x += (rnd() - 0.5) * 0.3; else y += (rnd() - 0.5) * 0.3;
                            pts.Add(P(x, y));
                        }
                        g.Strip(pts, 0.002, th.accent, 0.16);
                        g.Circle(x, y, 0.006, th.secondary, 0.35);
                    }
                    break;
                }
                case "cosmos":
                {
                    for (int i = 0, m = n(6); i < m; i++) g.Radial(rx(), ry(), 0.2 + rnd() * 0.25, i % 2 == 1 ? th.primary : th.secondary, 0.22);
                    for (int i = 0, m = n(80); i < m; i++) g.Circle(rx(), ry(), 0.0015 + rnd() * 0.003, i % 6 == 0 ? th.accent : "#ffffff", 0.25 + rnd() * 0.5);
                    double px = R.x + R.w * 0.78, py = R.y + R.h * 0.18;
                    g.Circle(px, py, 0.07, th.primary, 0.16);
                    g.Ellipse(px, py, 0.13, 0.03, -20 * Math.PI / 180, 0.004, th.accent, 0.2);
                    break;
                }
                case "core":
                {
                    double y = R.y + R.h * 0.5;
                    for (int i = 1; i <= 10; i++) g.Ring(cx, y, i * 0.085, 0.003, i % 2 == 1 ? th.primary : th.secondary, 0.1);
                    for (int i = 0; i < 24; i++)
                    {
                        double a = (double)i / 24 * Math.PI * 2;
                        g.Line(cx + Math.Cos(a) * 0.12, y + Math.Sin(a) * 0.12, cx + Math.Cos(a) * 1.4, y + Math.Sin(a) * 1.4, 0.002, th.primary, 0.07);
                    }
                    break;
                }
                case "classic":
                {
                    for (int i = 0, m = n(70); i < m; i++) g.Circle(rx(), ry(), 0.0015 + rnd() * 0.003, i % 7 == 0 ? th.secondary : "#ffffff", 0.2 + rnd() * 0.4);
                    double y = R.y + R.h * 0.48;
                    for (int i = 1; i <= 6; i++)
                        g.Arc(cx, y, i * 0.12, (200 + i * 12) * Math.PI / 180, 220 * Math.PI / 180, 0.006, i % 2 == 1 ? th.secondary : th.primary, 0.16);
                    g.Radial(cx, y, R.w * 0.55, th.primary, 0.18);
                    g.Radial(R.x + R.w * 0.2, R.y + R.h * 0.2, R.w * 0.4, "#ec4a9a", 0.12);
                    break;
                }
                case "modernGrid":
                {
                    for (int i = -12; i < 24; i++)
                    {
                        double x = R.x + i * 0.1;
                        g.Line(x, R.y, x + R.h * 0.6, bottom, 0.002, th.primary, 0.07);
                    }
                    g.Radial(R.x + R.w * 0.2, R.y + R.h * 0.3, R.w * 0.6, th.secondary, 0.16);
                    g.Radial(R.x + R.w * 0.85, R.y + R.h * 0.7, R.w * 0.6, th.primary, 0.16);
                    break;
                }
            }
        }
    }
}
