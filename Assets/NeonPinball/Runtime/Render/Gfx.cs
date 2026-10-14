// Gfx — TS'deki Skia sarmalayıcısının (gfx.ts) karşılığı. Çizim tablo uzayında (x sağa, y aşağı, 1 × 1.8) yapılır
// ve MeshBatch'e quad olarak yazılır. Parıltı, ucuz olması için üst üste yarı saydam çizgi/daire ile yapılır.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonPinball
{
    public sealed class Gfx
    {
        public MeshBatch batch;
        /// <summary>Klasik görünüm: ışıma yok, düz + metalik.</summary>
        public bool flat;
        /// <summary>Ucuz mod: halo katmanları atlanır.</summary>
        public bool lite;
        /// <summary>Kenar yumuşatma payı (tablo birimi).</summary>
        public double pad = 0.004;

        public Gfx(MeshBatch b) { batch = b; }

        // ---------- Temel şekiller ----------

        public void Circle(double x, double y, double r, string hex, double alpha = 1) => Circle(x, y, r, Hex.C(hex, alpha));

        public void Circle(double x, double y, double r, Color32 c)
        {
            if (c.a == 0 || r <= 0) return;
            double h = r + pad;
            batch.Quad(x, y, 1, 0, h, h, c, Shp.Disc, (float)r, 0, 0);
        }

        public void Ring(double x, double y, double r, double w, string hex, double alpha = 1) => Ring(x, y, r, w, Hex.C(hex, alpha));

        public void Ring(double x, double y, double r, double w, Color32 c)
        {
            if (c.a == 0) return;
            double h = r + w / 2 + pad;
            batch.Quad(x, y, 1, 0, h, h, c, Shp.Ring, (float)r, (float)w, 0);
        }

        public void Line(double x1, double y1, double x2, double y2, double w, string hex, double alpha = 1) => Line(x1, y1, x2, y2, w, Hex.C(hex, alpha));

        public void Line(double x1, double y1, double x2, double y2, double w, Color32 c) => Taper(x1, y1, w / 2, x2, y2, w / 2, c, false);

        /// <summary>İki ucunda farklı yarıçaplı dolu kapsül (flipper gövdesi için).</summary>
        public void Taper(double x1, double y1, double r1, double x2, double y2, double r2, Color32 c, bool additive)
        {
            if (c.a == 0) return;
            double dx = x2 - x1, dy = y2 - y1;
            double len = Math.Sqrt(dx * dx + dy * dy);
            double ax = 1, ay = 0;
            if (len > 1e-9) { ax = dx / len; ay = dy / len; }
            double h = len / 2;
            double rmax = Math.Max(r1, r2);
            batch.Quad((x1 + x2) / 2, (y1 + y2) / 2, ax, ay, h + rmax + pad, rmax + pad, c, Shp.Capsule + (additive ? Shp.Additive : 0), (float)h, (float)r1, (float)r2);
        }

        public void Glow(double x, double y, double r, string hex, double alpha, bool additive = true)
        {
            var c = Hex.C(hex, alpha);
            if (c.a == 0) return;
            batch.Quad(x, y, 1, 0, r, r, c, Shp.Glow + (additive ? Shp.Additive : 0), (float)r, 0, 0);
        }

        /// <summary>Polyline şerit (köşelerde miter, bindirme yok → alfa çift boyanmaz).</summary>
        public void Strip(IList<Vector2> pts, double w, string hex, double alpha = 1, bool closed = false) => Strip(pts, w, Hex.C(hex, alpha), closed);

        List<Vector2> tmp = new List<Vector2>(64);

        public void Strip(IList<Vector2> pts, double w, Color32 c, bool closed = false)
        {
            int n = pts.Count;
            if (n < 2 || c.a == 0) return;
            double hw = w / 2, e = hw + pad;
            int first = -1, prevL = -1, prevR = -1;
            int total = closed ? n + 1 : n;
            for (int i = 0; i < total; i++)
            {
                Vector2 p = pts[i % n];
                Vector2 a = closed ? pts[(i - 1 + n) % n] : pts[Mathf.Max(0, i - 1)];
                Vector2 b = closed ? pts[(i + 1) % n] : pts[Mathf.Min(n - 1, i + 1)];
                Vector2 d1 = (p - a), d2 = (b - p);
                if (d1.sqrMagnitude < 1e-12) d1 = d2; if (d2.sqrMagnitude < 1e-12) d2 = d1;
                d1.Normalize(); d2.Normalize();
                Vector2 n1 = new Vector2(-d1.y, d1.x), n2 = new Vector2(-d2.y, d2.x);
                Vector2 m = n1 + n2;
                float ml = m.magnitude;
                Vector2 nn = ml < 1e-4f ? n1 : m / ml;
                float k = ml < 1e-4f ? 1f : Mathf.Min(2.5f, 2f / ml);
                double ox = nn.x * e * k, oy = nn.y * e * k;
                int l = batch.AddVertex(p.x + ox, p.y + oy, 0, e, c, Shp.Strip, (float)hw, 0, 0);
                int r = batch.AddVertex(p.x - ox, p.y - oy, 0, -e, c, Shp.Strip, (float)hw, 0, 0);
                if (first < 0) first = l;
                if (prevL >= 0) { batch.Tri(prevL, prevR, r); batch.Tri(prevL, r, l); }
                prevL = l; prevR = r;
            }
        }

        public void Polyline(double w, string hex, double alpha, params double[] xy)
        {
            tmp.Clear();
            for (int i = 0; i + 1 < xy.Length; i += 2) tmp.Add(new Vector2((float)xy[i], (float)xy[i + 1]));
            Strip(tmp, w, hex, alpha);
        }

        /// <summary>Yay (merkez, yarıçap, başlangıç ve süpürme açısı radyan; y aşağı).</summary>
        public void Arc(double cx, double cy, double r, double start, double sweep, double w, string hex, double alpha = 1)
        {
            int n = Math.Max(6, (int)(Math.Abs(sweep) / (Math.PI / 24)));
            tmp.Clear();
            for (int i = 0; i <= n; i++)
            {
                double a = start + sweep * i / n;
                tmp.Add(new Vector2((float)(cx + Math.Cos(a) * r), (float)(cy + Math.Sin(a) * r)));
            }
            Strip(tmp, w, hex, alpha);
        }

        public void Ellipse(double cx, double cy, double rx, double ry, double rot, double w, string hex, double alpha = 1)
        {
            int n = 40;
            tmp.Clear();
            double cs = Math.Cos(rot), sn = Math.Sin(rot);
            for (int i = 0; i < n; i++)
            {
                double a = i * Math.PI * 2 / n;
                double lx = Math.Cos(a) * rx, ly = Math.Sin(a) * ry;
                tmp.Add(new Vector2((float)(cx + lx * cs - ly * sn), (float)(cy + lx * sn + ly * cs)));
            }
            Strip(tmp, w, hex, alpha, true);
        }

        public void FillEllipse(double cx, double cy, double rx, double ry, double rot, string hex, double alpha = 1)
        {
            int n = 28;
            tmp.Clear();
            double cs = Math.Cos(rot), sn = Math.Sin(rot);
            for (int i = 0; i < n; i++)
            {
                double a = i * Math.PI * 2 / n;
                double lx = Math.Cos(a) * rx, ly = Math.Sin(a) * ry;
                tmp.Add(new Vector2((float)(cx + lx * cs - ly * sn), (float)(cy + lx * sn + ly * cs)));
            }
            FillConvex(tmp, Hex.C(hex, alpha));
        }

        /// <summary>Dışbükey çokgen dolgusu (üçgen yelpazesi). Kenarlar için ince bir şerit çizilebilir.</summary>
        public void FillConvex(IList<Vector2> pts, Color32 c, bool smoothEdge = true)
        {
            int n = pts.Count;
            if (n < 3 || c.a == 0) return;
            int first = -1, prev = -1;
            for (int i = 0; i < n; i++)
            {
                int vi = batch.AddVertex(pts[i].x, pts[i].y, 0, 0, c, Shp.Flat, 0, 0, 0);
                if (first < 0) first = vi;
                if (i >= 2) batch.Tri(first, prev, vi);
                prev = vi;
            }
            if (smoothEdge) Strip(pts, 0.0015, c, true);
        }

        /// <summary>Merkeze göre yıldız-biçimli (içbükey olabilen) çokgen dolgusu.</summary>
        public void FillFan(double cx, double cy, IList<Vector2> ring, Color32 c)
        {
            int n = ring.Count;
            if (n < 3 || c.a == 0) return;
            int center = batch.AddVertex(cx, cy, 0, 0, c, Shp.Flat, 0, 0, 0);
            int first = -1, prev = -1;
            for (int i = 0; i < n; i++)
            {
                int vi = batch.AddVertex(ring[i].x, ring[i].y, 0, 0, c, Shp.Flat, 0, 0, 0);
                if (first < 0) first = vi;
                if (prev >= 0) batch.Tri(center, prev, vi);
                prev = vi;
            }
            batch.Tri(center, prev, first);
            Strip(ring, 0.0015, c, true);
        }

        public void FillConvex(IList<Vector2> pts, string hex, double alpha = 1, bool smoothEdge = true) => FillConvex(pts, Hex.C(hex, alpha), smoothEdge);

        /// <summary>Bir eğri ile alt kenar arasını doldurur (kum tepeleri).</summary>
        public void FillBelow(IList<Vector2> curve, double bottom, string hex, double alpha)
        {
            var c = Hex.C(hex, alpha);
            if (c.a == 0) return;
            int pt = -1, pb = -1;
            for (int i = 0; i < curve.Count; i++)
            {
                int t = batch.AddVertex(curve[i].x, curve[i].y, 0, 0, c, Shp.Flat, 0, 0, 0);
                int b = batch.AddVertex(curve[i].x, bottom, 0, 0, c, Shp.Flat, 0, 0, 0);
                if (pt >= 0) { batch.Tri(pt, t, b); batch.Tri(pt, b, pb); }
                pt = t; pb = b;
            }
        }

        public void Rect(double x, double y, double w, double h, string hex, double alpha = 1) => Rect(x, y, w, h, Hex.C(hex, alpha));

        public void Rect(double x, double y, double w, double h, Color32 c)
        {
            if (c.a == 0) return;
            batch.Quad(x + w / 2, y + h / 2, 1, 0, w / 2, h / 2, c, Shp.Flat, 0, 0, 0);
        }

        /// <summary>Dönük yuvarlak kutu (çip topu vb.). angle: radyan.</summary>
        public void Box(double cx, double cy, double hw, double hh, double corner, double angle, Color32 c)
        {
            if (c.a == 0) return;
            batch.Quad(cx, cy, Math.Cos(angle), Math.Sin(angle), hw + pad, hh + pad, c, Shp.RoundBox, (float)hw, (float)hh, (float)corner);
        }

        public void BoxOutline(double cx, double cy, double hw, double hh, double corner, double angle, double w, Color32 c)
        {
            // dış kutu − iç kutu yerine: dört kenarı şerit olarak çiz
            double cs = Math.Cos(angle), sn = Math.Sin(angle);
            Vector2 P(double lx, double ly) => new Vector2((float)(cx + lx * cs - ly * sn), (float)(cy + lx * sn + ly * cs));
            tmp.Clear();
            tmp.Add(P(-hw, -hh)); tmp.Add(P(hw, -hh)); tmp.Add(P(hw, hh)); tmp.Add(P(-hw, hh));
            Strip(tmp, w, c, true);
        }

        /// <summary>Dikey çok duraklı degrade dolgu (renkler hex, konumlar 0..1).</summary>
        public void VGradient(double x, double y, double w, double h, string[] hex, double[] pos, double alphaMul = 1)
        {
            for (int i = 0; i + 1 < hex.Length; i++)
            {
                double y0 = y + h * pos[i], y1 = y + h * pos[i + 1];
                Color32 c0 = Hex.C(hex[i], alphaMul), c1 = Hex.C(hex[i + 1], alphaMul);
                int a = batch.AddVertex(x, y0, 0, 0, c0, Shp.Flat, 0, 0, 0);
                int b = batch.AddVertex(x + w, y0, 0, 0, c0, Shp.Flat, 0, 0, 0);
                int c = batch.AddVertex(x + w, y1, 0, 0, c1, Shp.Flat, 0, 0, 0);
                int d = batch.AddVertex(x, y1, 0, 0, c1, Shp.Flat, 0, 0, 0);
                batch.Tri(a, b, c); batch.Tri(a, c, d);
            }
        }

        /// <summary>Radyal degrade disk. 4 renk durağı (konumlar: 0, s1, s2, 1); odak R'nin katı olarak.</summary>
        public void RadialGradient(double x, double y, double R, Color32 c0, Color32 c1, Color32 c2, Color32 c3,
            double s1, double s2, double focusX, double focusY, double gradScale, bool clip)
        {
            double h = R + pad;
            if (!clip) h = R; // kırpma yoksa quad kendi sınırıdır
            int first = batch.Quad(x, y, 1, 0, h, h, c0, Shp.Radial, (float)R, (float)focusX, (float)focusY);
            batch.SetStops(first, c1, c2, c3, (float)s1, (float)s2, (float)gradScale);
            if (!clip) for (int i = 0; i < 4; i++) batch.Vtx(first + i).stops.z = 1f;
        }

        /// <summary>Skia'daki radial(hex, alpha → hex, 0) karşılığı.</summary>
        public void Radial(double x, double y, double r, string hex, double alpha)
        {
            var a = Hex.C(hex, alpha); var z = new Color32(a.r, a.g, a.b, 0);
            RadialGradient(x, y, r, a, z, z, z, 0.5, 0.75, 0, 0, 1.0, true);
        }

        /// <summary>Gölgeli küre (4 duraklı).</summary>
        public void Sphere(double x, double y, double r, string[] stops)
        {
            RadialGradient(x, y, r, Hex.C(stops[0]), Hex.C(stops[1]), Hex.C(stops[2]), Hex.C(stops[3]), 0.3, 0.75, -0.35, -0.4, 1.5, true);
        }

        // ---------- Neon ilkelleri ----------

        public void GlowLine(double x1, double y1, double x2, double y2, string glow, string core, double w, double k = 1)
        {
            if (flat)
            {
                Line(x1, y1, x2, y2, w * 1.9, "#05030f", 0.85);
                Line(x1, y1, x2, y2, w * 1.3, glow, 1);
                Line(x1, y1, x2, y2, w * 0.4, core, 0.8);
                return;
            }
            if (lite) Line(x1, y1, x2, y2, w * 2.6, glow, 0.14 * k);
            else
            {
                Line(x1, y1, x2, y2, w * 4, glow, 0.07 * k);
                Line(x1, y1, x2, y2, w * 2.4, glow, 0.16 * k);
            }
            Line(x1, y1, x2, y2, w, glow, 0.95 * Math.Min(1, k + 0.2));
            Line(x1, y1, x2, y2, w * 0.38, core, 0.9 * Math.Min(1, k + 0.2));
        }

        public void GlowCircle(double x, double y, double r, string glow, double k = 1)
        {
            if (flat) return;
            if (lite) Circle(x, y, r * 1.5, glow, 0.14 * k);
            else
            {
                Circle(x, y, r * 1.9, glow, 0.07 * k);
                Circle(x, y, r * 1.5, glow, 0.14 * k);
                Circle(x, y, r * 1.2, glow, 0.22 * k);
            }
        }
    }
}
