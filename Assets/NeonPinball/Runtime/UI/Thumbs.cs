// Küçük, statik resimler (dünya amblemleri, Sınırsız mod masa önizlemesi): açılışta bir kez RenderTexture'a çizilir,
// UI Toolkit'te arka plan resmi olarak kullanılır → çalışma anında ek maliyet yok.
using UnityEngine;

namespace NeonPinball
{
    public static class Thumbs
    {
        public static readonly RenderTexture[] World = new RenderTexture[10];
        public static RenderTexture Classic, Modern;
        static bool built;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() { built = false; Classic = null; Modern = null; for (int i = 0; i < World.Length; i++) World[i] = null; }

        static RenderTexture NewRt(int w, int h)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4, useMipMap = false, filterMode = FilterMode.Bilinear };
            rt.Create();
            return rt;
        }

        public static void Build()
        {
            if (built) return;
            built = true;
            TableView.EnsureMaterials();
            for (int w = 0; w < 10; w++) World[w] = RenderGlyph(w);
            Classic = RenderPreview(Game.EndlessClassic);
            Modern = RenderPreview(Game.EndlessModern);
        }

        public static RenderTexture For(string variant) => variant == "modern" ? Modern : Classic;

        static RenderTexture RenderGlyph(int world)
        {
            var th = LevelDb.Theme(LevelDb.Get(world * Game.LevelsPerWorld + 1).themeId);
            var go = new GameObject("GlyphStage"); go.transform.position = new Vector3(3000 + world * 10, 0, 0);
            var mf = go.AddComponent<MeshFilter>(); var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = TableView.MatNormal;
            var mesh = new Mesh();
            var mb = new MeshBatch(); var g = new Gfx(mb);
            Glyphs.Draw(g, th.glyph, 0.5, 0.5, 0.42, th.primary, 1);
            mb.Upload(mesh); mf.sharedMesh = mesh;
            var cgo = new GameObject("GlyphCam"); var cam = cgo.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 0.5f; cam.transform.position = new Vector3(3000.5f + world * 10, -0.5f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0, 0, 0, 0);
            var rt = NewRt(160, 160);
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
            Object.Destroy(cgo); Object.Destroy(go); Object.Destroy(mesh);
            return rt;
        }

        /// <summary>Masanın orta bölümü (bumper'lar, çekirdek, hedefler) kırpılmış önizleme: 1 × 1.25 oran.</summary>
        static RenderTexture RenderPreview(int levelId)
        {
            var engine = new GameEngine(LevelDb.Get(levelId), 1);
            var go = new GameObject("PreviewStage"); go.transform.position = new Vector3(6000 + levelId * 10, 0, 0);
            var view = go.AddComponent<TableView>();
            view.Init(engine);
            var cgo = new GameObject("PreviewCam"); var cam = cgo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            const int W = 420, H = 525;
            double scale = W / 0.97;
            var L = new TableLayout { width = W, height = H, scale = scale, ox = -0.015 * scale, oy = -0.3 * scale, hudTop = 0, insets = new Insets() };
            view.ApplyLayout(cam, L); cam.transform.position += new Vector3(6000 + levelId * 10, 0, 0);
            var rt = NewRt(W, H);
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
            Object.Destroy(cgo); Object.Destroy(go);
            engine.Dispose();
            return rt;
        }
    }
}
