using System;
using System.Collections.Generic;

namespace NeonPinball
{
    public static class Geo
    {
        public const double Deg = Math.PI / 180.0;

        public static double Clamp(double v, double lo, double hi) => v < lo ? lo : v > hi ? hi : v;
        public static double Lerp(double a, double b, double t) => a + (b - a) * t;
        public static double Hypot(double x, double y) => Math.Sqrt(x * x + y * y);

        /// <summary>(px,py) noktasının AB segmentine en yakın noktası.</summary>
        public static void ClosestOnSegment(double px, double py, double ax, double ay, double bx, double by,
            out double cx, out double cy, out double t)
        {
            double abx = bx - ax, aby = by - ay;
            double len2 = abx * abx + aby * aby;
            t = len2 > 1e-12 ? ((px - ax) * abx + (py - ay) * aby) / len2 : 0;
            if (t < 0) t = 0; else if (t > 1) t = 1;
            cx = ax + abx * t;
            cy = ay + aby * t;
        }

        public static void BuildPolyline(IList<V2> pts, out double[] cumulative, out double total)
        {
            cumulative = new double[pts.Count];
            total = 0;
            for (int i = 1; i < pts.Count; i++)
            {
                total += Hypot(pts[i].x - pts[i - 1].x, pts[i].y - pts[i - 1].y);
                cumulative[i] = total;
            }
        }
    }

    /// <summary>mulberry32 — TS createRng ile bit-bit aynı.</summary>
    public sealed class Rng
    {
        private uint a;
        public Rng(uint seed) { a = seed; }
        public double Next()
        {
            unchecked
            {
                a += 0x6D2B79F5u;
                uint t = a;
                t = (t ^ (t >> 15)) * (t | 1u);
                t ^= t + (t ^ (t >> 7)) * (t | 61u);
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }
    }
}
