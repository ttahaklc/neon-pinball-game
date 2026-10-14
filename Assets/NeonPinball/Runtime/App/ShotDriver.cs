#if UNITY_EDITOR
// Editör içi ekran görüntüsü sürücüsü: Shots/plan.json'daki adımları sırayla uygular, kamera + arayüzü RenderTexture'a çizip PNG yazar.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NeonPinball
{
    [Serializable] public class ShotStep { public string name, what, lang = "tr"; public int w = 1179, h = 2556, level = 1, top, bottom, left, right; public float d = 3, wait = 0.3f; public bool launch; }
    [Serializable] public class ShotPlan { public List<ShotStep> shots; public int seed; }

    public sealed class ShotDriver : MonoBehaviour
    {
        public static string PlanPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Shots/plan.json");
        AppController app;

        IEnumerator Start()
        {
            Application.runInBackground = true;
            app = GetComponent<AppController>();
            SaveStore.Volatile = true;
            var plan = JsonUtility.FromJson<ShotPlan>(File.ReadAllText(PlanPath));
            var log = new List<string>();
            if (plan.seed > 0)
            {
                // Mağaza görüntüleri için dolu bir ilerleme: ilk N bölüm tamam, yıldızlar karışık, rekorlar dolu
                for (int i = 1; i <= plan.seed; i++) SaveStore.Record(i, 42000 + i * 3170, true, 1 + ((i * 5 + 1) % 3));
                SaveStore.RecordEndless("classic", 186400); SaveStore.RecordEndless("modern", 241750);
                SaveStore.MarkTutorialSeen();
            }
            yield return null; yield return null;
            foreach (var pair in new[] { ("thumb_modern", Thumbs.Modern), ("thumb_classic", Thumbs.Classic), ("thumb_w0", Thumbs.World[0]) })
            {
                var t = Read(pair.Item2, pair.Item2.width, pair.Item2.height);
                File.WriteAllBytes(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Shots/" + pair.Item1 + ".png"), t.EncodeToPNG()); Destroy(t);
            }
            foreach (var s in plan.shots)
            {
                Scr.OverrideW = s.w; Scr.OverrideH = s.h; UiMetrics.ForceDensity = s.d;
                Scr.OverrideInsets = new Insets { top = s.top, bottom = s.bottom, left = s.left, right = s.right };
                SaveStore.Data.settings.language = s.lang; I18n.SetLanguage(s.lang);
                var camRt = new RenderTexture(s.w, s.h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
                var uiRt = new RenderTexture(s.w, s.h, 0, RenderTextureFormat.ARGB32);
                app.Cam.targetTexture = camRt; app.SetUiTarget(uiRt);
                app.DebugEnter(s.what, s.level);
                yield return null; yield return null; yield return null;
                if (s.launch && app.Engine != null)
                {
                    app.Engine.PlungerPress(); yield return new WaitForSeconds(0.9f); app.Engine.PlungerRelease();
                }
                yield return new WaitForSeconds(s.wait);
                yield return new WaitForEndOfFrame();
                try
                {
                    var a = Read(camRt, s.w, s.h); var b = Read(uiRt, s.w, s.h);
                    var pa = a.GetPixels32(); var pb = b.GetPixels32();
                    for (int i = 0; i < pa.Length; i++)
                    {
                        var u = pb[i]; float al = u.a / 255f;
                        // arayüz dolgusu dik alfa ile karışmış (premultiplied değil) varsayımı
                        pa[i] = new Color32((byte)(u.r * al + pa[i].r * (1 - al)), (byte)(u.g * al + pa[i].g * (1 - al)), (byte)(u.b * al + pa[i].b * (1 - al)), 255);
                    }
                    a.SetPixels32(pa); a.Apply();
                    File.WriteAllBytes(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Shots/" + s.name + ".png"), a.EncodeToPNG());
                    // ham katmanlar (hata ayıklama)
                    File.WriteAllBytes(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Shots/" + s.name + "_ui.png"), b.EncodeToPNG());
                    Destroy(a); Destroy(b);
                    log.Add("ok " + s.name);
                }
                catch (Exception e) { log.Add("ERR " + s.name + " " + e.Message); }
                app.Cam.targetTexture = null; app.SetUiTarget(null);
                camRt.Release(); uiRt.Release();
            }
            File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Shots/done.txt"), string.Join("\n", log));
            Scr.OverrideW = 0; Scr.OverrideH = 0; Scr.OverrideInsets = null; UiMetrics.ForceDensity = 0;
            UnityEditor.EditorApplication.isPlaying = false;
        }

        static Texture2D Read(RenderTexture rt, int w, int h)
        {
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.ReadPixels(new Rect(0, 0, w, h), 0, 0); t.Apply();
            RenderTexture.active = prev;
            return t;
        }
    }
}
#endif
