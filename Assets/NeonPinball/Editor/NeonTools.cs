// Editör araçları: malzemeleri üretir, ekran görüntüsü (her tema/çözünürlük) alır.
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonPinball.EditorTools
{
    public static class NeonTools
    {
        [MenuItem("Neon Pinball/Create Materials")]
        public static string CreateMaterials()
        {
            var sh = Shader.Find("NeonPinball/Shapes");
            if (sh == null) return "shader not found";
            Directory.CreateDirectory("Assets/NeonPinball/Resources/Materials");
            Make("NeonShapes", sh, 0, 8, 0, 15);
            Make("NeonShapesMask", sh, 1, 8, 2, 0);
            Make("NeonShapesClipped", sh, 1, 3, 0, 15);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            TableView.MatNormal = null;
            return "ok";
        }

        static void Make(string name, Shader sh, float stRef, float comp, float op, float mask)
        {
            string path = "Assets/NeonPinball/Resources/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = m == null;
            if (isNew) m = new Material(sh); else m.shader = sh;
            m.SetFloat("_StencilRef", stRef); m.SetFloat("_StencilComp", comp); m.SetFloat("_StencilOp", op); m.SetFloat("_ColorMask", mask);
            if (isNew) AssetDatabase.CreateAsset(m, path); else EditorUtility.SetDirty(m);
        }


        [MenuItem("Neon Pinball/Create Panel Settings")]
        public static string CreatePanelSettings()
        {
            Directory.CreateDirectory("Assets/NeonPinball/Resources/UI");
            string path = "Assets/NeonPinball/Resources/UI/NeonPanelSettings.asset";
            var ps = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.PanelSettings>(path);
            if (ps == null) { ps = ScriptableObject.CreateInstance<UnityEngine.UIElements.PanelSettings>(); AssetDatabase.CreateAsset(ps, path); }
            ps.scaleMode = UnityEngine.UIElements.PanelScaleMode.ConstantPixelSize;
            ps.scale = 1f;
            if (ps.themeStyleSheet == null)
            {
                foreach (var g in AssetDatabase.FindAssets("t:ThemeStyleSheet"))
                {
                    var tp = AssetDatabase.GUIDToAssetPath(g);
                    var t = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.ThemeStyleSheet>(tp);
                    if (t != null && tp.Contains("Runtime")) { ps.themeStyleSheet = t; break; }
                }
            }
            EditorUtility.SetDirty(ps);
            AssetDatabase.SaveAssets();
            return "theme=" + (ps.themeStyleSheet != null ? AssetDatabase.GetAssetPath(ps.themeStyleSheet) : "none");
        }

        [MenuItem("Neon Pinball/Create Boot Scene")]
        public static string CreateBootScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.043f, 0.047f, 0.063f); cam.orthographic = true;
            camGo.AddComponent<AudioListener>();
            var app = new GameObject("App"); app.AddComponent<AppController>();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };
            return "ok";
        }

        /// <summary>Başsız ekran görüntüsü: motoru kurar, belirli süre simüle eder, bir kareyi PNG olarak yazar.</summary>
        public static string Shot(int levelId, int w, int h, string path, double simSeconds = 0, bool launch = false, int insetTop = 0)
        {
            I18n.SetLanguage("tr");
            var level = LevelDb.Get(levelId);
            var engine = new GameEngine(level, 1);
            var root = new GameObject("ShotRoot");
            var view = root.AddComponent<TableView>();
            view.Init(engine);
            var camGo = new GameObject("ShotCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            cam.targetTexture = rt;
            var L = UiMetrics.Layout(w, h, new Insets { top = insetTop });
            if (launch) { engine.PlungerPress(); engine.Update(0.9); engine.PlungerRelease(); }
            double t = 0;
            while (t < simSeconds)
            {
                engine.SetFlipper(true, (t % 1.3) < 0.15); engine.SetFlipper(false, (t % 1.7) < 0.15);
                engine.Update(1.0 / 60); view.Advance(1.0 / 60); t += 1.0 / 60;
            }
            view.Advance(1.0 / 60);
            view.ApplyLayout(cam, L);
            view.BuildDynamic();
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(tex); rt.Release(); Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camGo); Object.DestroyImmediate(root);
            engine.Dispose();
            return path;
        }
    }
}
