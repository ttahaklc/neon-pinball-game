// Uygulama denetleyicisi: durum makinesi + oyun döngüsü + arayüz katmanları + ses/titreşim/kayıt bağları.
// Sahne yalnızca bir kamera + bu bileşenden oluşur (SAP benzetmesi: Fiori Launchpad kabuğu; ekranlar durumdan türetilir).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UIElements;

namespace NeonPinball
{
    public sealed class AppController : MonoBehaviour
    {
        /// <summary>Test / ekran görüntüsü kancaları.</summary>
        public static bool HeadlessUi;
        public AppContext Ctx { get; } = new AppContext();
        public UiRoot Ui_ => ui;
        public Camera Cam => cam;
        public GameEngine Engine => engine;

        Camera cam; UiRoot ui; HudView hud; FxView fx;
        GameEngine engine; TableView view; TableLayout layout;
        readonly InputController input = new InputController();

        // menü kahramanı (canlı masa)
        GameEngine menuEngine; TableView menuView; TableLayout menuLayout; double menuT; float[] menuFrames; int menuFps = 30;

        bool tutorialOpen, tutorialShown, pickerOpen, newBest, lastRun, layoutDirty = true, sceneDirty = true;
        int bestScore; string endlessVariant;
        float hudClock, splashT, loadingT, smoothDt = 1f / 60f, liteTimer, fpsClock; int frames, fps;
        float pendingT = -1; Action pendingAction;
        int lastScreenW, lastScreenH; Rect lastSafe;
        AppState shownState = (AppState)(-1); int shownRun = -1;

        // ---------------------------------------------------------------- Başlangıç

        void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
#if UNITY_IOS
            if (!Application.isEditor)
            {
                // Alt kenardan kaydırma (ana ekrana dön) oyun sırasında ilk harekette uygulamayı duraklatıyordu: sistem hareketlerini ertele,
                // home çubuğunu gizle. Kullanıcı çıkmak için kenardan iki kez kaydırır (oyunlarda standart davranış).
                UnityEngine.iOS.Device.hideHomeButton = true;
                UnityEngine.iOS.Device.deferSystemGesturesMode = UnityEngine.iOS.SystemGestureDeferMode.All;
            }
#endif

            SaveStore.Load();
            I18n.SetLanguage(SaveStore.EffectiveLanguage());
            Haptics.Enabled = SaveStore.Data.settings.haptics;
            Fonts.Init();
            AudioService.Create(transform);
            AudioService.SetSfx(SaveStore.Data.settings.sfx); AudioService.SetMusic(SaveStore.Data.settings.music);

            cam = Camera.main;
            if (cam == null) { var go = new GameObject("Main Camera") { tag = "MainCamera" }; cam = go.AddComponent<Camera>(); }
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Pal.Ink; cam.orthographic = true;
            var al = cam.GetComponent<AudioListener>(); if (al == null && FindFirstObjectByType<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>(); es.AddComponent<InputSystemUIInputModule>();
            }

            RefreshEnv(true);
            ui = UiRoot.Create(transform);
            ui.ApplyScale();
            fx = new FxView();
            ui.Fx.Add(fx.Root);
            Thumbs.Build();
            menuFrames = LoadMenuFrames();

            input.Blocked = IsUiAt;
            input.OnFlipperPress = _ => { AudioService.Play(Sfx.Flipper); Haptics.Light(); };
            I18n.Changed += OnLanguageChanged;
            SaveStore.Changed += OnSaveChanged;
            Render();
#if UNITY_EDITOR
            if (System.IO.File.Exists(ShotDriver.PlanPath)) gameObject.AddComponent<ShotDriver>();
#endif
        }

        void OnDestroy() { I18n.Changed -= OnLanguageChanged; SaveStore.Changed -= OnSaveChanged; SaveStore.Flush(); }

        float[] LoadMenuFrames()
        {
            var ta = Resources.Load<TextAsset>("Data/menu_ball");
            if (ta == null) return null;
            var d = JsonUtility.FromJson<MenuBall>(ta.text);
            menuFps = d.fps; return d.frames;
        }
        [Serializable] class MenuBall { public int fps; public float[] frames; }

        bool IsUiAt(Vector2 screenPos)
        {
            var panel = ui.Root.panel; if (panel == null) return false;
            var pp = RuntimePanelUtils.ScreenToPanel(panel, screenPos);
            return panel.Pick(pp) != null;
        }

        // ---------------------------------------------------------------- Ortam / düzen

        static Rect SafeKey() { var i = Scr.SafeInsets(); return new Rect((float)i.left, (float)i.bottom, (float)i.right, (float)i.top); }

        bool RefreshEnv(bool force = false)
        {
            var safe = SafeKey();
            bool changed = UiEnv.Refresh(Scr.W, Scr.H, UiMetrics.SafeInsetsPx());
            lastScreenW = Scr.W; lastScreenH = Scr.H; lastSafe = safe;
            return changed || force;
        }

        void ComputeLayouts()
        {
            var insets = UiMetrics.SafeInsetsPx();
            layout = TableLayout.Compute(Scr.W, Scr.H, insets, Ui.K * UiEnv.D);
            if (ctx.state == AppState.MainMenu)
            {
                int w = Scr.W, h = Scr.H;
                if (UiEnv.Landscape)
                {
                    menuLayout = HeroLayout(w, h, w / 2f, h, (Mathf.Max(UiEnv.InTop, 8)) * UiEnv.D);
                    
                }
                else
                {
                    float bottomPx = Screens.MenuBottomBlockHeight() * UiEnv.D;
                    float topPx = (UiEnv.InTop + Ui.S(8)) * UiEnv.D;
                    float heroH = Mathf.Max(100, h - bottomPx - topPx);
                    menuLayout = HeroLayout(w, h, w, heroH, topPx);
                    
                }
            }
        }
        Rect menuRect = new Rect(0, 0, 1, 1);
        AppContext ctx => Ctx;

        static TableLayout HeroLayout(float w, float h, float areaW, float areaH, float top)
        {
            double scale = Math.Min(areaH / 1.74, areaW * 0.96);
            return new TableLayout { width = w, height = h, scale = scale, ox = (areaW - scale) / 2, oy = top, hudTop = 0, insets = new Insets(), landscape = w > h };
        }

        // ---------------------------------------------------------------- Durum

        public void Dispatch(AppAction a, int levelId = 0)
        {
            var prev = ctx.state;
            LevelResult res = null;
            bool ok = AppMachine.Reduce(ctx, a, levelId, res);
            if (ok) OnTransition(prev, a);
        }

        void DispatchResult(LevelResult r) { var p = ctx.state; if (AppMachine.Reduce(ctx, AppAction.LevelComplete, 0, r)) OnTransition(p, AppAction.LevelComplete); }
        void DispatchOver(GameOverReason reason, int score) { var p = ctx.state; if (AppMachine.Reduce(ctx, AppAction.GameOver, 0, null, reason, score)) OnTransition(p, AppAction.GameOver); }

        void OnTransition(AppState prev, AppAction a)
        {
            pickerOpen = false;
            if (a == AppAction.SelectLevel || a == AppAction.Restart || a == AppAction.NextLevel) { StartRun(ctx.levelId); loadingT = 0; }
            if (a == AppAction.Menu) { DestroyRun(); }
            if (a == AppAction.Loaded && !SaveStore.Data.tutorialSeen && !tutorialShown) { tutorialShown = true; tutorialOpen = true; }
            layoutDirty = true; sceneDirty = true;
            Render();
        }

        // ---------------------------------------------------------------- Koşu (oyun) yaşam döngüsü

        void StartRun(int levelId)
        {
            DestroyMenu();
            bool keepTutorial = tutorialShown;
            DestroyRun();
            tutorialShown = keepTutorial;
            var level = LevelDb.Get(levelId);
            engine = new GameEngine(level, (uint)UnityEngine.Random.Range(1, int.MaxValue));
            var go = new GameObject("Table");
            view = go.AddComponent<TableView>();
            view.Init(engine);
            view.OnPopup += (x, y, t, hex, big) => fx.Popup(x, y, t, hex, big);
            view.OnBanner += (t, hex) => fx.Banner(t, hex);
            engine.Events += OnEngineEvent;
            endlessVariant = level.endless ? level.themeId : null;
            bestScore = level.endless ? SaveStore.EndlessBest(endlessVariant) : (SaveStore.Progress(level.id)?.best ?? 0);
            newBest = false; tutorialOpen = false; pendingT = -1; pendingAction = null; lastRun = false;
            engine.Pause();
            hud = new HudView(level, bestScore, () => Dispatch(AppAction.Pause));
            ui.Hud.Clear(); ui.Hud.Add(hud.Root);
            fx.Clear();
            input.Clear(engine);
        }

        void DestroyRun()
        {
            if (view != null) Destroy(view.gameObject);
            engine?.Dispose();
            view = null; engine = null; hud = null;
            ui.Hud.Clear(); fx.Clear();
            tutorialOpen = false; tutorialShown = false; pendingT = -1; pendingAction = null;
        }

        void DestroyMenu()
        {
            if (menuView != null) Destroy(menuView.gameObject);
            menuEngine?.Dispose();
            menuView = null; menuEngine = null;
            cam.rect = new Rect(0, 0, 1, 1);
        }

        void EnsureMenu()
        {
            if (menuEngine != null) return;
            menuEngine = new GameEngine(LevelDb.Get(Game.EndlessModern), 1);
            menuEngine.Pause();
            var go = new GameObject("MenuTable");
            menuView = go.AddComponent<TableView>();
            menuView.Init(menuEngine);
            menuT = 0;
        }

        // ---------------------------------------------------------------- Olaylar (ses, titreşim, sonuç kaydı)

        void OnEngineEvent(GameEvent e)
        {
            switch (e.type)
            {
                case GEvent.Physics:
                {
                    var p = e.phys;
                    switch (p.type)
                    {
                        case PhysType.Launch: AudioService.Play(Sfx.Launch); Haptics.Heavy(); break;
                        case PhysType.Bumper: AudioService.Play(Sfx.Bumper); Haptics.Medium(); break;
                        case PhysType.Core: AudioService.Play(Sfx.Bumper); Haptics.Heavy(); break;
                        case PhysType.Sling: AudioService.Play(Sfx.Bumper, 0.7f); Haptics.Light(); break;
                        case PhysType.Target: case PhysType.Drop: AudioService.Play(Sfx.Target); Haptics.Light(); break;
                        case PhysType.Flipper: if (p.speed > 0.8) Haptics.Light(); break;
                        case PhysType.Wall: case PhysType.Post: if (p.speed > 0.7) AudioService.Play(Sfx.Wall, Mathf.Min(1f, (float)p.speed / 3f)); break;
                        case PhysType.Spin: AudioService.Play(Sfx.Spinner); break;
                        case PhysType.Lane: AudioService.Play(Sfx.Target, 0.8f); break;
                        case PhysType.RampEnter: AudioService.Play(Sfx.Ramp); Haptics.Medium(); break;
                        case PhysType.Drain: AudioService.Play(Sfx.Drain); Haptics.Error(); break;
                    }
                    break;
                }
                case GEvent.Combo: AudioService.Play(Sfx.Combo); break;
                case GEvent.SetComplete: case GEvent.Multiplier: AudioService.Play(Sfx.Score); Haptics.Success(); break;
                case GEvent.SpecialStart: AudioService.Play(Sfx.Special); Haptics.Success(); break;
                case GEvent.ExtraBall: AudioService.Play(Sfx.ExtraBall); Haptics.Success(); break;
                case GEvent.BallSaved: AudioService.Play(Sfx.ExtraBall, 0.6f); break;
                case GEvent.LevelComplete:
                {
                    AudioService.Play(Sfx.LevelComplete); Haptics.Success();
                    var r = e.result;
                    newBest = SaveStore.Record(engine.level.id, r.score, true, r.stars);
                    bestScore = SaveStore.Progress(engine.level.id)?.best ?? r.score;
                    pendingT = TimingC.LevelCompleteDelayMs / 1000f; pendingAction = () => DispatchResult(r);
                    break;
                }
                case GEvent.GameOver:
                {
                    AudioService.Play(Sfx.GameOver); Haptics.Error();
                    if (endlessVariant != null) { newBest = SaveStore.RecordEndless(endlessVariant, e.score); bestScore = SaveStore.EndlessBest(endlessVariant); }
                    else { newBest = SaveStore.Record(engine.level.id, e.score, false, 0); bestScore = SaveStore.Progress(engine.level.id)?.best ?? e.score; }
                    var reason = e.reason; int score = e.score;
                    pendingT = TimingC.GameOverDelayMs / 1000f; pendingAction = () => DispatchOver(reason, score);
                    break;
                }
            }
        }

        // ---------------------------------------------------------------- Arayüz kurulumu

        void OnLanguageChanged() { if (ui != null) { layoutDirty = true; Render(true); hud?.Rebuild(); } }
        void OnSaveChanged()
        {
            Haptics.Enabled = SaveStore.Data.settings.haptics;
        }

        /// <summary>Duruma göre katmanları yeniden kurar.</summary>
        void Render(bool force = false)
        {
            var st = ctx.state;
            ui.ApplyScale();
            ui.Screen.Clear(); ui.ClearOverlay();
            ScreenDispatch disp = (a, id) => Dispatch(a, id);
            bool inGame = AppMachine.IsInGame(ctx);

            switch (st)
            {
                case AppState.Boot: ui.Screen.Add(Splash()); break;
                case AppState.MainMenu:
                    EnsureMenu();
                    ui.Screen.Add(Screens.MainMenu(disp, () => { pickerOpen = true; Render(); }));
                    if (pickerOpen) ui.Overlay.Add(Screens.EndlessPicker(v => { pickerOpen = false; Dispatch(AppAction.SelectLevel, v == "modern" ? Game.EndlessModern : Game.EndlessClassic); }, () => { pickerOpen = false; Render(); }));
                    break;
                case AppState.LevelSelect: ui.Screen.Add(Screens.LevelSelect(disp)); break;
                case AppState.Settings: ui.Screen.Add(Screens.Settings(disp, () => { Render(); })); break;
                case AppState.Loading: if (engine != null) ui.Overlay.Add(Overlays.Intro(engine.level)); break;
                case AppState.Playing: if (tutorialOpen) ui.Overlay.Add(Overlays.Tutorial(() => { tutorialOpen = false; SaveStore.MarkTutorialSeen(); Render(); })); break;
                case AppState.Paused:
                    ui.Overlay.Add(Overlays.Pause(engine.level.endless, () => Dispatch(AppAction.Resume), () => Dispatch(AppAction.Restart),
                        () => Dispatch(AppAction.OpenSettings), () => Dispatch(AppAction.OpenLevels), () => Dispatch(AppAction.Menu)));
                    break;
                case AppState.LevelComplete:
                    if (ctx.result != null)
                        ui.Overlay.Add(Overlays.LevelComplete(ui, engine.level, ctx.result, bestScore > 0 ? bestScore : ctx.result.score, newBest,
                            () => Dispatch(AppAction.NextLevel, Mathf.Min(Game.TotalLevels, ctx.levelId + 1)), () => Dispatch(AppAction.Restart),
                            () => Dispatch(AppAction.OpenLevels), () => Dispatch(AppAction.Menu)));
                    break;
                case AppState.GameOver:
                    if (ctx.hasGameOver)
                        ui.Overlay.Add(Overlays.GameOver(engine.level, ctx.overReason, ctx.overScore, bestScore > 0 ? bestScore : ctx.overScore, newBest,
                            () => Dispatch(AppAction.Restart), () => Dispatch(AppAction.OpenLevels), () => Dispatch(AppAction.Menu)));
                    break;
            }
            // HUD yalnızca oyundayken (ayarlar / bölüm listesi tam ekranı örter)
            ui.Hud.style.display = (inGame && engine != null) ? DisplayStyle.Flex : DisplayStyle.None;
            ui.Fx.style.display = (inGame && engine != null) ? DisplayStyle.Flex : DisplayStyle.None;
            if (hud != null && inGame) hud.Rebuild();
            shownState = st; layoutDirty = true;
        }

        VisualElement Splash()
        {
            var p = new VisualElement { pickingMode = PickingMode.Position }; Ui.Fill(p); p.style.backgroundColor = Pal.Ink;
            p.style.alignItems = Align.Center; p.style.justifyContent = Justify.Center;
            // Yerel açılış ekranıyla aynı logo, aynı boyut: iki ekran arasında kayma/yanıp sönme olmaz.
            Texture2D tex = null;
            try
            {
                var ta = Resources.Load<TextAsset>("Brand/launch_logo");
                if (ta != null) { tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear }; if (!tex.LoadImage(ta.bytes)) tex = null; }
            }
            catch (System.Exception e) { Debug.LogWarning("[Splash] logo: " + e.Message); tex = null; }
            if (tex != null)
            {
                bool tablet = Mathf.Min(UiEnv.W, UiEnv.H) >= 600;
                float w = tablet ? 380 : 280, h = w * tex.height / tex.width;
                var img = new VisualElement { pickingMode = PickingMode.Ignore };
                img.style.width = w; img.style.height = h; img.style.flexShrink = 0;
                img.style.backgroundImage = new StyleBackground(Background.FromTexture2D(tex));
                img.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                p.Add(img);
            }
            else p.Add(Ui.Text(I18n.T("appName"), Ui.S(40), Pal.Bone, Face.Display, true, TextAnchor.MiddleCenter));
            return p;
        }

        // ---------------------------------------------------------------- Kare döngüsü

        string fatalShown;
        void Update()
        {
            try { UpdateInner(); }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                if (fatalShown == null && ui != null && ui.Screen != null)
                {
                    fatalShown = e.GetType().Name;
                    var st = e.ToString(); if (st.Length > 900) st = st.Substring(0, 900);
                    var t = new Label(st) { pickingMode = PickingMode.Ignore };
                    t.style.position = Position.Absolute; t.style.left = 12; t.style.right = 12; t.style.top = 60;
                    t.style.color = new Color(1f, 0.45f, 0.45f); t.style.fontSize = 11; t.style.whiteSpace = WhiteSpace.Normal;
                    ui.Screen.Add(t);
                }
            }
        }

        void UpdateInner()
        {
            float dt = Time.unscaledDeltaTime;
            if (Scr.W != lastScreenW || Scr.H != lastScreenH || SafeKey() != lastSafe)
            {
                RefreshEnv();
                ui.ApplyScale();
                Render();
            }
            SaveStore.Tick();
            HandleBack();

            var st = ctx.state;
            if (st == AppState.Boot)
            {
                splashT += dt;
                if (splashT > TimingC.SplashMinMs / 1000f) { AppMachine.Reduce(ctx, AppAction.BootDone); AudioService.StartMusic(); layoutDirty = true; Render(); }
            }
            else if (st == AppState.Loading)
            {
                loadingT += dt;
                if (loadingT > TimingC.LevelIntroMs / 1000f) Dispatch(AppAction.Loaded);
            }
            if (pendingT >= 0) { pendingT -= dt; if (pendingT < 0) { var a = pendingAction; pendingAction = null; a?.Invoke(); } }

            st = ctx.state;
            if (layoutDirty) { ComputeLayouts(); layoutDirty = false; sceneDirty = true; if (fx != null && layout != null) fx.SetLayout(layout); if (hud != null) hud.Rebuild(); }

            bool inGame = AppMachine.IsInGame(ctx) && engine != null;
            bool covered = st == AppState.Settings || st == AppState.LevelSelect || st == AppState.Boot;

            if (inGame && !covered) UpdateGame(dt, st);
            else if (st == AppState.MainMenu) UpdateMenu(dt);

            // Örtülü ya da gösterilecek masa yoksa kamerayı kapat (boşuna çizim = ısınma)
            cam.enabled = !(covered || (!inGame && st != AppState.MainMenu));
            ui.Tick(dt);
            fx.Tick(dt);

            // FPS göstergesi (hata ayıklama)
            frames++; fpsClock += dt;
            if (fpsClock >= 0.5f) { fps = Mathf.RoundToInt(frames / fpsClock); frames = 0; fpsClock = 0; }
            fx.SetDebug(SaveStore.Data.settings.debug && inGame && !covered && engine != null
                ? "FPS " + fps + "\n" + st + " / " + engine.status + "\nball " + engine.world.ball.mode + " " + engine.world.ball.x.ToString("0.00") + "," + engine.world.ball.y.ToString("0.00") +
                  "\nv " + Math.Sqrt(engine.world.ball.vx * engine.world.ball.vx + engine.world.ball.vy * engine.world.ball.vy).ToString("0.00") + (view != null && view.lite ? "  lite" : "")
                : null);
        }

        void UpdateGame(float dt, AppState st)
        {
            bool run = st == AppState.Playing && !tutorialOpen;
            if (run != lastRun)
            {
                lastRun = run;
                if (run) engine.Resume(); else engine.Pause();
                if (!run) input.Clear(engine);
                sceneDirty = true;
            }
            // Uyarlanabilir ucuz mod
            smoothDt = Mathf.Lerp(smoothDt, dt, 0.05f);
            liteTimer += dt;
            if (liteTimer > 2f)
            {
                liteTimer = 0;
                if (!view.lite && smoothDt > 1f / 40f) view.lite = true;
                else if (view.lite && smoothDt < 1f / 55f) view.lite = false;
            }
            cam.rect = new Rect(0, 0, 1, 1);
            input.Poll(engine, layout, run);
            engine.Update(dt);
            bool animate = run || engine.status == GameStatus.LevelComplete || engine.status == GameStatus.GameOver;
            if (animate) view.Advance(dt);
            if (animate || sceneDirty)
            {
                view.ApplyLayout(cam, layout);
                view.BuildDynamic();
                sceneDirty = false;
            }
            hudClock += dt;
            if (hudClock >= 0.1f) { hudClock = 0; hud?.Refresh(engine); }
            hud?.SetRecord(bestScore);
            fx.ShowHint(st == AppState.Playing && !tutorialOpen && engine.status == GameStatus.Ready);
        }

        void UpdateMenu(float dt)
        {
            if (menuEngine == null || menuLayout == null) return;
            cam.rect = new Rect(0, 0, 1, 1);
            menuT += dt;
            if (menuFrames != null && menuFrames.Length >= 4)
            {
                int n = menuFrames.Length / 4;
                int f = (int)(menuT * menuFps) % n;
                var b = menuEngine.world.ball;
                b.mode = (f < 4 || f > n - 5) ? BallMode.Out : BallMode.Free;
                b.x = menuFrames[f * 4]; b.y = menuFrames[f * 4 + 1]; b.vx = 0; b.vy = 0;
                menuEngine.world.flippers[0].theta = menuFrames[f * 4 + 2];
                menuEngine.world.flippers[1].theta = menuFrames[f * 4 + 3];
            }
            menuView.Advance(dt);
            menuView.ApplyLayout(cam, menuLayout);
            menuView.BuildDynamic();
        }


        // ---------------------------------------------------------------- Test kancaları (ekran görüntüsü sürücüsü)

        public void SetUiTarget(RenderTexture rt) { ui.SetTarget(rt); }
        public bool DebugIsRunning => engine != null;

        public void DebugEnter(string what, int level)
        {
            DestroyMenu();
            pickerOpen = false;
            ctx.stack.Clear();
            bool wantTutorial = what == "tutorial";
            switch (what)
            {
                case "menu": case "picker":
                    DestroyRun(); ctx.state = AppState.MainMenu; pickerOpen = what == "picker"; break;
                case "levels": DestroyRun(); ctx.state = AppState.LevelSelect; ctx.stack.Add(AppState.MainMenu); break;
                case "settings": DestroyRun(); ctx.state = AppState.Settings; ctx.stack.Add(AppState.MainMenu); break;
                default:
                    ctx.levelId = level; ctx.runId++;
                    StartRun(level);
                    ctx.state = AppState.Playing; tutorialShown = !wantTutorial; tutorialOpen = wantTutorial;
                    if (what == "intro") { ctx.state = AppState.Loading; loadingT = -1000; }
                    else if (what == "pause") ctx.state = AppState.Paused;
                    else if (what == "complete")
                    {
                        var lv = engine.level;
                        ctx.result = new LevelResult { levelId = level, score = (int)(lv.stars.twoStarScore * 1.15) + 120450, bonus = 3000, ballsLeft = 1, stars = 3, maxCombo = 4 };
                        ctx.state = AppState.LevelComplete; newBest = true; bestScore = ctx.result.score;
                    }
                    else if (what == "over")
                    {
                        ctx.hasGameOver = true; ctx.overReason = GameOverReason.Balls; ctx.overScore = 48250; bestScore = 91200; ctx.state = AppState.GameOver; newBest = false;
                    }
                    break;
            }
            layoutDirty = true; sceneDirty = true; lastRun = ctx.state == AppState.Playing && !tutorialOpen; if (engine != null && !lastRun) engine.Pause(); else if (engine != null) engine.Resume();
            Render();
        }

        // ---------------------------------------------------------------- Geri tuşu / uygulama yaşam döngüsü

        void HandleBack()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            switch (ctx.state)
            {
                case AppState.Settings: case AppState.LevelSelect: Dispatch(AppAction.Back); break;
                case AppState.Playing: if (tutorialOpen) { tutorialOpen = false; SaveStore.MarkTutorialSeen(); Render(); } else Dispatch(AppAction.Pause); break;
                case AppState.Paused: Dispatch(AppAction.Resume); break;
                case AppState.MainMenu: if (pickerOpen) { pickerOpen = false; Render(); } break;
            }
        }

        void OnApplicationPause(bool paused)
        {
            AudioService.Suspend(paused);
            if (paused) { SaveStore.Flush(); if (ctx.state == AppState.Playing) Dispatch(AppAction.Pause); }
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus) { SaveStore.Flush(); if (!Application.isEditor && ctx.state == AppState.Playing) Dispatch(AppAction.Pause); }
        }
    }
}
