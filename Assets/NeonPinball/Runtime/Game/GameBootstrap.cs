// Oyun döngüsü sahibi: motoru sürer, görünümü günceller, girişi okur. Sahne yalnızca bir kamera + bu bileşenden oluşur.
using UnityEngine;

namespace NeonPinball
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        public static int StartLevel = 1;
        public bool showDebug;

        public GameEngine Engine { get; private set; }
        public TableView View { get; private set; }
        public TableLayout Layout { get; private set; }
        Camera cam;
        readonly InputController input = new InputController();
        float smoothDt = 1f / 60f;
        float liteTimer;
        int lastW, lastH;

        void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            I18n.SetLanguage(I18n.DetectDeviceLanguage());
            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            StartRun(StartLevel);
        }

        public void StartRun(int levelId)
        {
            if (View != null) Destroy(View.gameObject);
            Engine?.Dispose();
            var level = LevelDb.Get(levelId);
            Engine = new GameEngine(level, (uint)Random.Range(1, int.MaxValue));
            var go = new GameObject("Table");
            View = go.AddComponent<TableView>();
            View.Init(Engine);
            View.debug = showDebug;
            lastW = 0;
        }

        void Update()
        {
            if (Engine == null) return;
            if (Screen.width != lastW || Screen.height != lastH)
            {
                lastW = Screen.width; lastH = Screen.height;
                Layout = UiMetrics.Layout(Screen.width, Screen.height, UiMetrics.SafeInsetsPx());
            }
            float dt = Time.unscaledDeltaTime;
            // Uyarlanabilir ucuz mod: kare süresi uzun sürerse halo katmanları kapanır
            smoothDt = Mathf.Lerp(smoothDt, dt, 0.05f);
            liteTimer += dt;
            if (liteTimer > 2f)
            {
                liteTimer = 0;
                if (!View.lite && smoothDt > 1f / 40f) View.lite = true;
                else if (View.lite && smoothDt < 1f / 55f) View.lite = false;
            }
            bool run = Engine.IsRunning;
            input.Poll(Engine, Layout, run);
            Engine.Update(dt);
            View.Advance(dt);
            View.ApplyLayout(cam, Layout);
            View.BuildDynamic();
        }
    }
}
