// Masa görünümü (TS TableRenderer): statik katmanlar bir kez mesh olarak kurulur; her karede yalnızca hareket eden / yanan parçalar
// tek dinamik mesh'e yazılır. Her şey tek shader + 3 malzeme (normal, maske, maskeli) ile birkaç draw call'da çizilir.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonPinball
{
    public sealed class TableView : MonoBehaviour
    {
        public bool debug;
        /// <summary>Ucuz mod: halo katmanları yok, daha az parçacık (kare süresi yüksekse otomatik açılır).</summary>
        public bool lite;

        public event Action<double, double, string, string, bool> OnPopup;   // x, y (tablo), metin, renk, büyük mi
        public event Action<string, string> OnBanner;                        // metin, renk

        GameEngine engine;
        ThemeDef th;
        Gfx g;
        readonly MeshBatch dyn = new MeshBatch(), scene = new MeshBatch();
        readonly StaticLayer layer = new StaticLayer();
        MeshRenderer rScene, rMask, rClipped, rSolid, rDyn;
        MeshFilter fScene, fMask, fClipped, fSolid, fDyn;
        Mesh mScene, mMask, mClipped, mSolid, mDyn;
        string sceneKey = "";
        double time, rollAngle, shake;
        uint seed = 7;
        readonly List<float> trail = new List<float>();
        readonly List<Particle> particles = new List<Particle>();
        readonly Dictionary<string, double[]> rampCum = new Dictionary<string, double[]>();
        readonly Dictionary<InsertDef, Vector2[]> insertShapes = new Dictionary<InsertDef, Vector2[]>();
        readonly List<Vector2> tmp = new List<Vector2>(16);
        public double Shake => shake;

        struct Particle { public double x, y, vx, vy, life, max, size; public string hex; }

        public static Material MatNormal, MatMask, MatClipped;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() { MatNormal = null; MatMask = null; MatClipped = null; }

        public static void EnsureMaterials()
        {
            if (MatNormal != null) return;
            MatNormal = Resources.Load<Material>("Materials/NeonShapes");
            MatMask = Resources.Load<Material>("Materials/NeonShapesMask");
            MatClipped = Resources.Load<Material>("Materials/NeonShapesClipped");
            if (MatNormal == null)
            {
                var sh = Shader.Find("NeonPinball/Shapes");
                MatNormal = new Material(sh); MatMask = new Material(sh); MatClipped = new Material(sh);
                MatMask.SetFloat("_StencilRef", 1); MatMask.SetFloat("_StencilComp", 8); MatMask.SetFloat("_StencilOp", 2); MatMask.SetFloat("_ColorMask", 0);
                MatClipped.SetFloat("_StencilRef", 1); MatClipped.SetFloat("_StencilComp", 3); MatClipped.SetFloat("_StencilOp", 0);
            }
        }

        MeshRenderer MakeRenderer(string name, Material m, int order, out MeshFilter f, out Mesh mesh)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            f = go.AddComponent<MeshFilter>();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = m; r.sortingOrder = order;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            mesh = new Mesh { name = name };
            f.sharedMesh = mesh;
            return r;
        }

        public void Init(GameEngine e)
        {
            EnsureMaterials();
            engine = e; th = e.table.theme;
            g = new Gfx(dyn);
            rScene = MakeRenderer("Scene", MatNormal, -40, out fScene, out mScene);
            rMask = MakeRenderer("Mask", MatMask, -30, out fMask, out mMask);
            rClipped = MakeRenderer("Clipped", MatClipped, -29, out fClipped, out mClipped);
            rSolid = MakeRenderer("Solid", MatNormal, -20, out fSolid, out mSolid);
            rDyn = MakeRenderer("Dynamic", MatNormal, 0, out fDyn, out mDyn);

            foreach (var r in e.level.ramps)
            {
                var cum = new double[r.path.Count];
                for (int i = 1; i < r.path.Count; i++) cum[i] = cum[i - 1] + Geo.Hypot(r.path[i].x - r.path[i - 1].x, r.path[i].y - r.path[i - 1].y);
                rampCum[r.id] = cum;
            }
            layer.Build(e.table);
            layer.mask.Upload(mMask); layer.clipped.Upload(mClipped); layer.solid.Upload(mSolid);
            e.Events += OnEvent;
        }

        void OnDestroy() { if (engine != null) engine.Events -= OnEvent; }

        // ---------- Efekt besleme ----------

        double Rnd() { seed = (uint)((seed * 16807UL) % 2147483647UL); return seed / 2147483647.0; }

        void Burst(double x, double y, string hex, int n, double speed = 0.5)
        {
            int count = lite ? (n + 1) / 2 : n;
            for (int i = 0; i < count; i++)
            {
                if (particles.Count >= 70) particles.RemoveAt(0);
                double a = Rnd() * Math.PI * 2, s = speed * (0.35 + Rnd() * 0.8);
                particles.Add(new Particle { x = x, y = y, vx = Math.Cos(a) * s, vy = Math.Sin(a) * s, life = 0.45 + Rnd() * 0.35, max = 0.8, size = 0.004 + Rnd() * 0.006, hex = hex });
            }
        }

        string ColorOf(string id)
        {
            var lv = engine.level; string c = null;
            foreach (var b in lv.bumpers) if (b.id == id) c = b.color;
            if (c == null) foreach (var t in lv.targets) if (t.id == id) c = t.color;
            if (c == null) foreach (var t in lv.dropTargets) if (t.id == id) c = t.color;
            if (c == null) foreach (var r in lv.ramps) if (r.id == id) c = r.color;
            if (c == null) foreach (var l in lv.lanes) if (l.id == id) c = l.color;
            if (c == null) foreach (var s in lv.spinners) if (s.id == id) c = s.color;
            return th.Swatch(c ?? "cyan").glow;
        }

        void Say(string text, string hex) { OnBanner?.Invoke(text, hex); }

        void OnEvent(GameEvent e)
        {
            switch (e.type)
            {
                case GEvent.Physics:
                {
                    var p = e.phys;
                    if (p.type == PhysType.Bumper) Burst(p.x, p.y, ColorOf(p.id), 7);
                    else if (p.type == PhysType.Target || p.type == PhysType.Drop) Burst(p.x, p.y, ColorOf(p.id), 6, 0.4);
                    else if (p.type == PhysType.Sling) Burst(p.x, p.y, "#ffd24d", 4, 0.35);
                    else if (p.type == PhysType.Core) { Burst(p.x, p.y, "#ffffff", 14, 0.8); shake = Math.Max(shake, 0.6); }
                    else if (p.type == PhysType.RampEnter) Burst(p.x, p.y, ColorOf(p.id), 8, 0.5);
                    else if (p.type == PhysType.Drain) shake = Math.Max(shake, 0.8);
                    break;
                }
                case GEvent.Score:
                    if (e.points > 0) OnPopup?.Invoke(e.x, e.y, "+" + I18n.FormatNumber(e.points), "#ffffff", e.points >= 1000);
                    break;
                case GEvent.Combo: Say(I18n.T("comboUp", ("n", Math.Min(5, e.chain))), th.Swatch("amber").core); break;
                case GEvent.SetComplete: Say(e.kind == "drop" ? I18n.T("dropSet") : e.kind == "lane" ? I18n.T("laneSet") : I18n.T("setComplete"), th.Swatch("cyan").core); break;
                case GEvent.Multiplier: Say(I18n.T("multiplierUp", ("n", e.value)), th.Swatch("magenta").core); break;
                case GEvent.SpecialStart: Say(I18n.T("special"), th.Swatch("amber").core); shake = 0.5; break;
                case GEvent.ExtraBall: Say(I18n.T("extraBall"), th.Swatch("green").core); break;
                case GEvent.BallSaved: Say(I18n.T("ballSaved"), th.Swatch("cyan").core); break;
                case GEvent.BallLost: Say(I18n.T("ballLost"), th.Swatch("red").core); break;
                case GEvent.CoreCharged: Say(I18n.T("coreCharged"), th.Swatch("cyan").core); break;
                case GEvent.LevelComplete:
                    Say(I18n.T("levelComplete"), th.Swatch("green").core);
                    Burst(TableBuilder.CenterX, 0.9, "#ffffff", 30, 1.1);
                    break;
            }
        }

        /// <summary>Gerçek zamanlı efekt ilerlemesi (motor duraklatılsa da çağrılabilir).</summary>
        public void Advance(double dt)
        {
            time += dt;
            shake = Math.Max(0, shake - dt * 3);
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];
                p.life -= dt;
                if (p.life <= 0) { particles.RemoveAt(i); continue; }
                p.x += p.vx * dt; p.y += p.vy * dt;
                p.vx *= 0.96; p.vy = p.vy * 0.96 + 0.3 * dt;
                particles[i] = p;
            }
            var b = engine.world.ball;
            if (b.mode != BallMode.Out && b.mode != BallMode.Plunger)
            {
                rollAngle += (b.vx - b.vy * 0.3) * dt / b.r;
                trail.Add((float)b.x); trail.Add((float)b.y);
                if (trail.Count > 18) trail.RemoveRange(0, 2);
            }
            else if (trail.Count > 0) trail.Clear();
        }

        // ---------- Yerleşim ----------

        /// <summary>Kamerayı ekran yerleşimine oturtur; yerleşim değiştiyse arka camı yeniden kurar.</summary>
        public void ApplyLayout(Camera cam, TableLayout L)
        {
            string key = Mathf.RoundToInt((float)L.width) + "x" + Mathf.RoundToInt((float)L.height) + ":" + Mathf.RoundToInt((float)L.ox) + ":" + Mathf.RoundToInt((float)L.oy);
            if (key != sceneKey)
            {
                sceneKey = key;
                var region = L.ScreenRegion();
                double ex, ey, er;
                if (L.landscape) { ex = -L.ox / 2 / L.scale; ey = 0.9; er = Math.Max(0.12, L.ox / L.scale * 0.3); }
                else { ex = 0.5; ey = ((L.insets.top + L.oy) / 2 - L.oy) / L.scale; er = Math.Max(0.12, (L.oy - L.insets.top) / L.scale * 0.42); }
                StaticLayer.BuildScene(scene, th, region, ex, ey, er);
                scene.Upload(mScene);
            }
            cam.orthographic = true;
            cam.orthographicSize = (float)(L.height / (2 * L.scale));
            double sx = shake > 0 ? (Rnd() - 0.5) * shake * 0.01 : 0, sy = shake > 0 ? (Rnd() - 0.5) * shake * 0.01 : 0;
            cam.transform.position = new Vector3((float)((L.width / 2 - L.ox) / L.scale - sx), (float)(-(L.height / 2 - L.oy) / L.scale + sy), -10f);
        }

        // ---------- Dinamik çizim ----------

        public void BuildDynamic()
        {
            dyn.Clear();
            g.flat = th.look == "classic";
            g.lite = lite;
            DrawInserts();
            DrawRamps();
            DrawDynamicElements();
            DrawSpinners();
            DrawCore();
            DrawPlunger();
            if (engine.BallSaveActive) DrawBallSave();
            if (!lite) DrawTrail();
            DrawBall();
            DrawFlippers();
            if (engine.SpecialActive) DrawSpecialFrame();
            DrawParticles();
            if (debug) DrawDebug();
            dyn.Upload(mDyn);
        }

        void DrawDynamicElements()
        {
            var e = engine; var lv = e.level;
            foreach (var s in e.table.slings) { float f = e.slingFlash[s.id]; if (f > 0.02) Elements.Sling(g, th, s, f); }
            foreach (var b in lv.bumpers) { float f = e.bumperFlash[b.id]; if (f > 0.02) Elements.Bumper(g, th, b, f); }
            foreach (var t in lv.targets) { bool lit = e.targetLit[t.id]; float f = e.targetFlash[t.id]; if (lit || f > 0) Elements.Target(g, th, t, lit, f); }
            foreach (var d in lv.dropTargets) Elements.DropTarget(g, th, d, e.dropDown[d.id]);
            foreach (var l in lv.lanes) if (e.laneLit[l.id]) Elements.LaneLight(g, th, l, true);
        }

        Vector2[] InsertShape(InsertDef ins)
        {
            if (insertShapes.TryGetValue(ins, out var arr)) return arr;
            double[][] pts = ins.kind == "chevron"
                ? new[] { new[] { -1, -0.6 }, new[] { 0, 0.4 }, new[] { 1, -0.6 }, new[] { 1, -0.1 }, new[] { 0, 0.9 }, new[] { -1, -0.1 } }
                : ins.kind == "arrow"
                    ? new[] { new[] { 0, -1.0 }, new[] { 0.8, 0.2 }, new[] { 0.3, 0.2 }, new[] { 0.3, 1 }, new[] { -0.3, 1 }, new[] { -0.3, 0.2 }, new[] { -0.8, 0.2 } }
                    : new[] { new[] { 0, -1.0 }, new[] { 0.3, -0.3 }, new[] { 1, -0.3 }, new[] { 0.45, 0.15 }, new[] { 0.65, 0.9 }, new[] { 0, 0.45 }, new[] { -0.65, 0.9 }, new[] { -0.45, 0.15 }, new[] { -1, -0.3 }, new[] { -0.3, -0.3 } };
            arr = new Vector2[pts.Length];
            double cs = Math.Cos(ins.rotation), sn = Math.Sin(ins.rotation);
            for (int i = 0; i < pts.Length; i++)
                arr[i] = new Vector2((float)(ins.x + (pts[i][0] * cs - pts[i][1] * sn) * ins.size), (float)(ins.y + (pts[i][0] * sn + pts[i][1] * cs) * ins.size));
            insertShapes[ins] = arr;
            return arr;
        }

        void DrawInserts()
        {
            foreach (var ins in engine.level.inserts)
            {
                var n = th.Swatch(ins.color);
                double chase = 0.35 + 0.65 * Math.Max(0, Math.Sin(time * 3 - ins.phase * 0.7));
                double a = 0.25 + chase * 0.55, s = ins.size;
                switch (ins.kind)
                {
                    case "dot":
                        g.GlowCircle(ins.x, ins.y, s, n.glow, a * 0.6);
                        g.Circle(ins.x, ins.y, s * 0.7, n.glow, a);
                        break;
                    case "ring":
                        g.Ring(ins.x, ins.y, s, s * 0.18, n.glow, a);
                        break;
                    default:
                        // yıldız/ok/şevron: dışbükey olmayabilir → merkez yelpazesi
                        g.FillFan(ins.x, ins.y, InsertShape(ins), Hex.C(n.glow, a * 0.85));
                        break;
                }
            }
        }

        void PointOnRamp(string id, IList<V2> path, double s, out double x, out double y, out double dx, out double dy)
        {
            var cum = rampCum[id];
            double total = cum[cum.Length - 1];
            double d = ((s % total) + total) % total;
            int i = 1;
            while (i < cum.Length - 1 && cum[i] < d) i++;
            double seg = cum[i] - cum[i - 1]; if (seg == 0) seg = 1;
            double t0 = (d - cum[i - 1]) / seg;
            var a = path[i - 1]; var b = path[i];
            x = a.x + (b.x - a.x) * t0; y = a.y + (b.y - a.y) * t0; dx = b.x - a.x; dy = b.y - a.y;
        }

        void DrawRamps()
        {
            foreach (var r in engine.level.ramps)
            {
                var n = th.Swatch(r.color);
                double glow = engine.rampGlow[r.id];
                var cum = rampCum[r.id];
                double total = cum[cum.Length - 1];
                int count = Math.Max(3, (int)Math.Floor(total / 0.1 + 0.5));
                double sz = r.width * 0.32;
                for (int i = 0; i < count; i++)
                {
                    double s = (double)i / count * total + time * 0.12;
                    PointOnRamp(r.id, r.path, s, out double px, out double py, out double dx, out double dy);
                    double ang = Math.Atan2(dy, dx), c = Math.Cos(ang), sn = Math.Sin(ang);
                    double tx = px + sz * 0.6 * c, ty = py + sz * 0.6 * sn, a = 0.25 + glow * 0.6;
                    g.Line(px - sz * c + sz * sn, py - sz * sn - sz * c, tx, ty, r.width * 0.12, n.core, a);
                    g.Line(px - sz * c - sz * sn, py - sz * sn + sz * c, tx, ty, r.width * 0.12, n.core, a);
                }
                if (glow > 0.02)
                    for (int i = 1; i < r.path.Count; i++) g.Line(r.path[i - 1].x, r.path[i - 1].y, r.path[i].x, r.path[i].y, r.width * 1.5, n.glow, glow * 0.45);
                g.Ring(r.entry.x, r.entry.y, r.entryRadius, 0.004, n.glow, 0.35 + glow * 0.5);
            }
        }

        void DrawSpinners()
        {
            foreach (var sp in engine.world.spinners)
            {
                var n = th.Swatch(sp.def.color);
                double half = sp.def.length / 2, cx = Math.Cos(sp.angle) * half, cy = Math.Sin(sp.angle) * half;
                g.GlowLine(sp.def.x - cx, sp.def.y - cy, sp.def.x + cx, sp.def.y + cy, n.glow, n.core, 0.009, 0.6 + Math.Min(1, Math.Abs(sp.omega) / 20));
                g.Circle(sp.def.x, sp.def.y, 0.009, "#d8dcff");
            }
        }

        void DrawCore()
        {
            var lv = engine.level;
            if (!lv.hasCore) return;
            var core = lv.core; var e = engine;
            var cy = th.Swatch("cyan"); var am = th.Swatch("amber");
            double f = e.coreFlash; int charge = e.coreCharge;
            double r = core.r * (1 + f * 0.08);
            string main = charge > 0 ? am.glow : cy.glow;
            g.GlowCircle(core.x, core.y, r, main, 0.7 + f);
            g.Circle(core.x, core.y, r, th.look == "classic" ? "#071a2a" : "#04020e");
            g.Ring(core.x, core.y, r * 0.92, r * 0.14, charge > 0 ? am.glow : th.core, 0.95);
            double rot = time * (0.8 + charge);
            for (int i = 0; i < 3; i++) g.Arc(core.x, core.y, r * 0.7, rot + i * Math.PI * 2 / 3, 70 * Math.PI / 180, r * 0.08, cy.core, 0.8);
            for (int i = 0; i < 3; i++)
            {
                bool on = i < charge;
                g.Ring(core.x, core.y, r * (0.22 + i * 0.12), r * 0.06, on ? am.core : cy.dark, on ? 1 : 0.7);
            }
            g.Circle(core.x, core.y, r * 0.12, f > 0.05 ? "#ffffff" : cy.core, 0.9);
        }

        void DrawPlunger()
        {
            var e = engine; var ball = e.world.ball;
            double pull = e.plungerCharge * 0.022, baseY = PlungerC.RestY + 0.03;
            double knobY = PlungerC.RestY + ball.r + (ball.mode == BallMode.Plunger ? pull : 0);
            const int coils = 7;
            tmp.Clear();
            tmp.Add(new Vector2((float)PlungerC.RestX, (float)(knobY + 0.01)));
            for (int i = 1; i <= coils; i++)
            {
                double y = knobY + 0.01 + (baseY - knobY - 0.01) * i / coils;
                tmp.Add(new Vector2((float)(PlungerC.RestX + (i % 2 == 1 ? 0.014 : -0.014)), (float)y));
            }
            g.Strip(tmp, 0.003, "#aab2d6", 0.9);
            g.Rect(PlungerC.RestX - 0.026, knobY, 0.052, 0.012, "#cfd6f5", 1);
            g.Rect(PlungerC.RestX - 0.026, knobY, 0.052, 0.004, "#ffffff", 0.7);
            if (e.status == GameStatus.Ready)
            {
                double c = e.plungerCharge, top = 1.45 - c * 0.5;
                string hex = c < 0.5 ? th.Swatch("cyan").glow : c < 0.85 ? th.Swatch("amber").glow : th.Swatch("red").glow;
                g.Rect(PlungerC.LaneMinX + 0.008, 0.95, 0.012, 0.5, "#ffffff", 0.12);
                if (c > 0) g.Rect(PlungerC.LaneMinX + 0.008, top, 0.012, 1.45 - top, hex, 0.95);
                double blink = 0.4 + 0.6 * Math.Abs(Math.Sin(time * 3)), ay = 1.3 + Math.Sin(time * 6) * 0.01;
                var ac = th.Swatch("amber").core;
                g.Line(PlungerC.RestX - 0.02, ay, PlungerC.RestX, ay + 0.035, 0.006, ac, blink);
                g.Line(PlungerC.RestX + 0.02, ay, PlungerC.RestX, ay + 0.035, 0.006, ac, blink);
            }
        }

        void DrawBallSave()
        {
            double y = FlipperC.PivotY + 0.1; var cy = th.Swatch("cyan");
            g.GlowLine(0.12, y, 0.8, y, cy.glow, cy.core, 0.008, 0.5 + 0.4 * Math.Sin(time * 10));
        }

        void DrawTrail()
        {
            int n = trail.Count / 2;
            double r = engine.world.ball.r;
            string hex = engine.SpecialActive ? th.Swatch("amber").glow : th.Swatch("cyan").glow;
            for (int i = 0; i < n - 1; i++)
            {
                double k = (i + 1.0) / n;
                g.Circle(trail[i * 2], trail[i * 2 + 1], r * (0.35 + 0.55 * k), hex, 0.12 * k);
            }
        }

        void DrawBall()
        {
            var b = engine.world.ball;
            if (b.mode == BallMode.Out) return;
            double lift = b.mode == BallMode.Ramp ? 1.25 : 1;
            Skins.Ball(g, th.ball, b.x, b.y, b.r * lift, rollAngle, time, lite);
        }

        void DrawFlippers()
        {
            bool classic = th.look == "classic";
            foreach (var f in engine.world.flippers)
            {
                double dx = f.sign * Math.Cos(f.theta), dy = Math.Sin(f.theta);
                double tx = f.px + dx * f.length, ty = f.py + dy * f.length;
                double nx = -dy, ny = dx, R = FlipperC.PivotRadius, r = FlipperC.TipRadius;
                string glow = f.pressed ? th.Swatch("amber").glow : (classic ? th.secondary : th.primary);
                string core = f.pressed ? th.Swatch("amber").core : "#ffffff";
                string fill = classic ? "#e9edf7" : "#c9d1f2";
                if (!classic) g.GlowLine(f.px, f.py, tx, ty, glow, core, 0.012, f.pressed ? 1 : 0.5);
                // Gövde: iki ucunda farklı yarıçaplı kapsül (TS'deki yamuk + iki daire ile aynı siluet)
                g.Taper(f.px, f.py, R + 0.0025, tx, ty, r + 0.0025, Hex.C(glow, 0.95), false);
                g.Taper(f.px, f.py, R, tx, ty, r, Hex.C(fill), false);
                g.Line(f.px + nx * R * 0.3, f.py + ny * R * 0.3, tx + nx * r * 0.3, ty + ny * r * 0.3, 0.004, "#ffffff", 0.6);
                g.Circle(f.px, f.py, R * 0.4, classic ? th.secondary : "#3a4270");
            }
        }

        void DrawSpecialFrame()
        {
            double a = 0.25 + 0.2 * Math.Sin(time * 8);
            string hex = th.Swatch("amber").glow;
            double w = 0.03;
            g.Rect(0, 0, 1, w / 2, hex, a); g.Rect(0, TableC.Height - w / 2, 1, w / 2, hex, a);
            g.Rect(0, 0, w / 2, TableC.Height, hex, a); g.Rect(1 - w / 2, 0, w / 2, TableC.Height, hex, a);
        }

        void DrawParticles()
        {
            foreach (var p in particles)
            {
                double k = Math.Max(0, p.life / p.max);
                g.Circle(p.x, p.y, p.size * (0.5 + k), p.hex, Math.Min(1, k * 1.4));
            }
        }

        void DrawDebug()
        {
            var w = engine.world;
            foreach (var s in w.segments) if (s.enabled) g.Line(s.ax, s.ay, s.bx, s.by, 0.002, s.kind == SegmentKind.Wall ? "#ff3b4a" : "#2dff7a", 0.9);
            foreach (var c in w.circles) if (c.enabled) g.Ring(c.x, c.y, c.r, 0.002, "#ffe14d", 0.9);
            var b = w.ball;
            g.Line(b.x, b.y, b.x + b.vx * 0.05, b.y + b.vy * 0.05, 0.003, "#ffffff", 0.9);
            g.Ring(b.x, b.y, b.r, 0.002, "#ffffff", 0.9);
        }
    }
}
