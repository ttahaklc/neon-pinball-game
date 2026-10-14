// Dünya amblemleri (TS drawGlyph): tek renkli çizgi sanatı, Gfx ile (masa arka camı için).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonPinball
{
    public static class Glyphs
    {
        static Vector2 P(double x, double y) => new Vector2((float)x, (float)y);

        static void Cubic(List<Vector2> o, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            for (int i = 1; i <= 12; i++)
            {
                float t = i / 12f, u = 1 - t;
                o.Add(u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3);
            }
        }

        static void Quad(List<Vector2> o, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            for (int i = 1; i <= 12; i++) { float t = i / 12f, u = 1 - t; o.Add(u * u * p0 + 2 * u * t * p1 + t * t * p2); }
        }

        public static void Draw(Gfx g, string kind, double cx, double cy, double r, string hex, double alpha)
        {
            double w = r * 0.11;
            var pts = new List<Vector2>();
            switch (kind)
            {
                case "mountain":
                    pts.Add(P(cx - r, cy + r * 0.6)); pts.Add(P(cx - r * 0.3, cy - r * 0.5)); pts.Add(P(cx + r * 0.05, cy + r * 0.05));
                    pts.Add(P(cx + r * 0.4, cy - r * 0.2)); pts.Add(P(cx + r, cy + r * 0.6));
                    g.Strip(pts, w, hex, alpha, true);
                    break;
                case "wave":
                    for (int k = -1; k <= 1; k++)
                    {
                        double y = cy + k * r * 0.5;
                        pts.Clear(); pts.Add(P(cx - r, y));
                        Cubic(pts, P(cx - r, y), P(cx - r * 0.5, y - r * 0.4), P(cx - r * 0.2, y + r * 0.4), P(cx + r * 0.2, y));
                        Cubic(pts, P(cx + r * 0.2, y), P(cx + r * 0.5, y - r * 0.35), P(cx + r * 0.75, y + r * 0.3), P(cx + r, y));
                        g.Strip(pts, w, hex, alpha);
                    }
                    break;
                case "flame":
                    pts.Add(P(cx, cy - r));
                    Cubic(pts, P(cx, cy - r), P(cx + r * 0.2, cy - r * 0.4), P(cx + r * 0.85, cy - r * 0.1), P(cx + r * 0.55, cy + r * 0.5));
                    Cubic(pts, P(cx + r * 0.55, cy + r * 0.5), P(cx + r * 0.4, cy + r * 0.95), P(cx - r * 0.4, cy + r * 0.95), P(cx - r * 0.55, cy + r * 0.45));
                    Cubic(pts, P(cx - r * 0.55, cy + r * 0.45), P(cx - r * 0.7, cy), P(cx - r * 0.2, cy - r * 0.3), P(cx, cy - r));
                    g.Strip(pts, w, hex, alpha);
                    break;
                case "snowflake":
                    for (int i = 0; i < 3; i++)
                    {
                        double a = i * Math.PI / 3;
                        g.Line(cx - Math.Cos(a) * r, cy - Math.Sin(a) * r, cx + Math.Cos(a) * r, cy + Math.Sin(a) * r, w, hex, alpha);
                    }
                    g.Ring(cx, cy, r * 0.3, w * 0.8, hex, alpha);
                    break;
                case "bolt":
                    pts.Add(P(cx + r * 0.2, cy - r)); pts.Add(P(cx - r * 0.55, cy + r * 0.1)); pts.Add(P(cx - r * 0.05, cy + r * 0.1));
                    pts.Add(P(cx - r * 0.25, cy + r)); pts.Add(P(cx + r * 0.6, cy - r * 0.2)); pts.Add(P(cx + r * 0.08, cy - r * 0.2));
                    g.Strip(pts, w, hex, alpha, true);
                    break;
                case "sun":
                    for (int i = 0; i < 8; i++)
                    {
                        double a = (double)i / 8 * Math.PI * 2;
                        g.Line(cx + Math.Cos(a) * r * 0.62, cy + Math.Sin(a) * r * 0.62, cx + Math.Cos(a) * r, cy + Math.Sin(a) * r, w, hex, alpha);
                    }
                    g.Ring(cx, cy, r * 0.4, w, hex, alpha);
                    break;
                case "eye":
                    pts.Add(P(cx - r, cy));
                    Quad(pts, P(cx - r, cy), P(cx, cy - r * 0.95), P(cx + r, cy));
                    Quad(pts, P(cx + r, cy), P(cx, cy + r * 0.95), P(cx - r, cy));
                    g.Strip(pts, w, hex, alpha);
                    g.Circle(cx, cy, r * 0.28, hex, alpha);
                    break;
                case "chip":
                    pts.Add(P(cx - r * 0.6, cy - r * 0.6)); pts.Add(P(cx + r * 0.6, cy - r * 0.6)); pts.Add(P(cx + r * 0.6, cy + r * 0.6)); pts.Add(P(cx - r * 0.6, cy + r * 0.6));
                    g.Strip(pts, w, hex, alpha, true);
                    for (int i = -1; i <= 1; i++)
                    {
                        g.Line(cx + i * r * 0.35, cy - r * 0.6, cx + i * r * 0.35, cy - r, w, hex, alpha);
                        g.Line(cx + i * r * 0.35, cy + r * 0.6, cx + i * r * 0.35, cy + r, w, hex, alpha);
                        g.Line(cx - r * 0.6, cy + i * r * 0.35, cx - r, cy + i * r * 0.35, w, hex, alpha);
                        g.Line(cx + r * 0.6, cy + i * r * 0.35, cx + r, cy + i * r * 0.35, w, hex, alpha);
                    }
                    g.Circle(cx, cy, r * 0.18, hex, alpha);
                    break;
                case "planet":
                    g.Ring(cx, cy, r * 0.58, w, hex, alpha);
                    g.Ellipse(cx, cy, r, r * 0.2, -20 * Math.PI / 180, w * 0.8, hex, alpha);
                    break;
                case "ring":
                    g.Ring(cx, cy, r * 0.9, w, hex, alpha);
                    g.Ring(cx, cy, r * 0.55, w, hex, alpha);
                    g.Circle(cx, cy, r * 0.18, hex, alpha);
                    break;
            }
        }
    }
}
