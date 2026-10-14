// Dinamik/Statik mesh toplayıcı: her şekil bir quad; hepsi tek malzemeyle tek draw call'da çizilir.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonPinball
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ShapeVertex
    {
        public Vector3 pos;
        public Color32 c0;
        public Vector2 uv;
        public Vector4 prm;
        public Vector4 c1, c2, c3;
        public Vector4 stops;
    }

    public static class Shp
    {
        public const float Disc = 0, Ring = 1, Capsule = 2, Strip = 3, Flat = 4, Radial = 5, Glow = 6, RoundBox = 7, Additive = 100;
    }

    public sealed class MeshBatch
    {
        ShapeVertex[] v = new ShapeVertex[4096];
        uint[] idx = new uint[8192];
        int vc, ic;
        Mesh mesh;

        public int VertexCount => vc;

        public void Clear() { vc = 0; ic = 0; }

        void Ensure(int addV, int addI)
        {
            if (vc + addV > v.Length) Array.Resize(ref v, Math.Max(v.Length * 2, vc + addV));
            if (ic + addI > idx.Length) Array.Resize(ref idx, Math.Max(idx.Length * 2, ic + addI));
        }

        /// <summary>Tablo uzayı (y aşağı) → dünya (y yukarı).</summary>
        static Vector3 W(double x, double y) => new Vector3((float)x, (float)-y, 0f);

        public int AddVertex(double x, double y, double lu, double lv, Color32 c0, float shape, float p1, float p2, float p3)
        {
            Ensure(1, 0);
            v[vc] = new ShapeVertex { pos = W(x, y), c0 = c0, uv = new Vector2((float)lu, (float)lv), prm = new Vector4(shape, p1, p2, p3) };
            return vc++;
        }

        public ref ShapeVertex Vtx(int i) => ref v[i];

        public void Tri(int a, int b, int c)
        {
            Ensure(0, 3);
            idx[ic++] = (uint)a; idx[ic++] = (uint)b; idx[ic++] = (uint)c;
        }

        /// <summary>
        /// Eksen hizalı yerel çerçeveli quad. (cx,cy) merkez, (ax,ay) yerel x yönü birim vektörü (tablo uzayı),
        /// halfW/halfH yerel yarı boyutlar. Köşe uv'leri yerel koordinattır.
        /// </summary>
        public int Quad(double cx, double cy, double ax, double ay, double halfW, double halfH,
            Color32 c0, float shape, float p1, float p2, float p3)
        {
            // yerel y yönü = (-ay, ax)
            double px = -ay, py = ax;
            int a = AddVertex(cx - ax * halfW - px * halfH, cy - ay * halfW - py * halfH, -halfW, -halfH, c0, shape, p1, p2, p3);
            int b = AddVertex(cx + ax * halfW - px * halfH, cy + ay * halfW - py * halfH, halfW, -halfH, c0, shape, p1, p2, p3);
            int c = AddVertex(cx + ax * halfW + px * halfH, cy + ay * halfW + py * halfH, halfW, halfH, c0, shape, p1, p2, p3);
            int d = AddVertex(cx - ax * halfW + px * halfH, cy - ay * halfW + py * halfH, -halfW, halfH, c0, shape, p1, p2, p3);
            Tri(a, b, c); Tri(a, c, d);
            return a;
        }

        public void SetStops(int firstVertex, Color32 c1, Color32 c2, Color32 c3, float s1, float s2, float gradScale)
        {
            Vector4 a = ToV4(c1), b = ToV4(c2), c = ToV4(c3);
            var st = new Vector4(s1, s2, 0, gradScale);
            for (int i = 0; i < 4; i++)
            {
                v[firstVertex + i].c1 = a; v[firstVertex + i].c2 = b; v[firstVertex + i].c3 = c; v[firstVertex + i].stops = st;
            }
        }

        static Vector4 ToV4(Color32 c) => new Vector4(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);

        public Mesh Upload(Mesh into = null)
        {
            if (into == null) into = mesh ?? (mesh = new Mesh { name = "NeonBatch" });
            mesh = into;
            mesh.Clear();
            if (vc == 0) return mesh;
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertexBufferParams(vc,
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.Float32, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.Float32, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord4, VertexAttributeFormat.Float32, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord5, VertexAttributeFormat.Float32, 4));
            mesh.SetVertexBufferData(v, 0, 0, vc, 0, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);
            mesh.SetIndexBufferParams(ic, IndexFormat.UInt32);
            mesh.SetIndexBufferData(idx, 0, 0, ic, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, ic), MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(1000, 1000, 1));
            return mesh;
        }
    }
}
