// GameEngine — bir level denemesinin tüm kuralları (TS GameEngine.ts'in C# karşılığı).
// Unity'den bağımsızdır (MonoBehaviour yok): fizik dünyasını sabit adımla sürer, olayları kurallara çevirir,
// sonucu TEK yerden karara bağlar. Level tamamlama, aynı karedeki drain'e karşı her zaman öncelik kazanır.
using System;
using System.Collections.Generic;

namespace NeonPinball
{
    public enum GameStatus { Ready, Playing, LevelComplete, GameOver }
    public enum GameOverReason { Balls, Time }
    public enum GEvent { Physics, Score, Combo, ComboLost, SetComplete, Multiplier, SpecialStart, SpecialEnd, ExtraBall, BallSaved, BallLost, BallReady, RampEnter, RampDone, CoreCharged, CoreHit, LevelComplete, GameOver }

    public class LevelResult
    {
        public int levelId, score, bonus, ballsUsed, ballsLeft, stars, maxCombo; public bool bonusAchieved; public double timeSec;
    }

    public struct GameEvent
    {
        public GEvent type; public PhysicsEvent phys; public int points, chain, value, ballsLeft; public double x, y;
        public string id, kind, group; public LevelResult result; public GameOverReason reason; public int score, ballsUsed;
    }

    public class HudSnapshot
    {
        public int score, ballsLeft, multiplier, chain; public double comboRemaining, specialRemaining; public bool specialActive;
        public bool hasTimer; public double timeLeft; public bool ballSaveActive; public double ballSaveLeft; public GameStatus status; public double plungerCharge;
    }

    public class GameEngine
    {
        public readonly LevelConfig level;
        public readonly TableDef table;
        public readonly PhysicsWorld world;
        public readonly ScoreSystem scoring;
        public readonly GameStats stats = new GameStats();

        public GameStatus status = GameStatus.Ready;
        public bool paused;
        public int ballsLeft;
        public LevelResult result;
        public GameOverReason? gameOverReason;

        // Masa durumu (render okur)
        public readonly Dictionary<string, bool> targetLit = new Dictionary<string, bool>();
        public readonly Dictionary<string, float> targetFlash = new Dictionary<string, float>();
        public readonly Dictionary<string, bool> dropDown = new Dictionary<string, bool>();
        public readonly Dictionary<string, bool> laneLit = new Dictionary<string, bool>();
        public readonly Dictionary<string, float> bumperFlash = new Dictionary<string, float>();
        public readonly Dictionary<string, float> slingFlash = new Dictionary<string, float>();
        public readonly Dictionary<string, float> rampGlow = new Dictionary<string, float>();
        public int coreCharge;
        public double coreFlash, specialTimeLeft, plungerCharge, elapsed;
        public int ballsLost, extraBallsAwarded;
        public bool hasTimer; public double timeLeft;

        public event Action<GameEvent> Events;

        double accumulator, plungerHold, ballSaveTimer;
        bool plungerHeld, saveUsed, drainPending, outcomeLocked, activeSpecial;
        readonly List<(double at, Action run)> timers = new List<(double, Action)>();
        int setTarget, setDrop, setLane;
        readonly Dictionary<string, int> segmentIndex = new Dictionary<string, int>();
        readonly List<string> keyScratch = new List<string>();

        public GameEngine(LevelConfig level, uint seed = 1)
        {
            this.level = level;
            table = TableBuilder.Build(level);
            world = new PhysicsWorld(table, seed, level.gravity, level.flipperScale);
            scoring = new ScoreSystem(level.comboWindowSec);
            ballsLeft = level.ballCount;
            hasTimer = level.hasTimer; timeLeft = level.timerSec;

            foreach (var t in level.targets) { targetLit[t.id] = false; targetFlash[t.id] = 0; }
            foreach (var d in level.dropTargets) dropDown[d.id] = false;
            foreach (var l in level.lanes) laneLit[l.id] = false;
            foreach (var b in level.bumpers) bumperFlash[b.id] = 0;
            foreach (var s in table.slings) slingFlash[s.id] = 0;
            foreach (var r in level.ramps) rampGlow[r.id] = 0;
            for (int i = 0; i < world.segments.Count; i++) if (world.segments[i].kind == SegmentKind.Drop) segmentIndex[world.segments[i].id] = i;
            world.onEvent = HandlePhysics;
        }

        void Emit(GameEvent e) { Events?.Invoke(e); }

        public void Dispose() { Events = null; timers.Clear(); world.onEvent = _ => { }; }

        // ---------- Komutlar ----------

        public bool IsRunning => !paused && (status == GameStatus.Ready || status == GameStatus.Playing);

        public void Pause()
        {
            paused = true; plungerHeld = false; plungerHold = 0; plungerCharge = 0;
            world.SetFlipper(true, false); world.SetFlipper(false, false);
        }

        public void Resume() { paused = false; }

        public void SetFlipper(bool left, bool pressed)
        {
            if (!IsRunning) return;
            var f = world.flippers[left ? 0 : 1];
            bool rising = pressed && !f.pressed;
            world.SetFlipper(left, pressed);
            if (rising && status == GameStatus.Playing) RotateLanes(left ? -1 : 1);
        }

        public void PlungerPress()
        {
            if (!IsRunning || status != GameStatus.Ready) return;
            plungerHeld = true; plungerHold = 0; plungerCharge = 0;
        }

        public void PlungerRelease()
        {
            if (!plungerHeld) return;
            plungerHeld = false;
            if (status != GameStatus.Ready || paused) { plungerCharge = 0; return; }
            bool tap = plungerHold * 1000 < PlungerC.TapThresholdMs;
            // Kısa dokunuş = normal güç; daha uzun basış güçlenir ama dokunuş gücünün altına düşmez (yavaş dokunuşta zayıf atış olmasın).
            double power = tap ? level.launchStrength : Math.Min(1, Math.Max(level.launchStrength, plungerHold / PlungerC.ChargeTime));
            plungerCharge = 0; plungerHold = 0;
            world.Launch(power);
        }

        public bool PlungerActive => plungerHeld;

        // ---------- Döngü ----------

        public void Update(double realDt)
        {
            if (!IsRunning) return;
            double dt = Math.Min(Math.Max(realDt, 0), PhysC.MaxFrameDt);
            accumulator += dt;
            if (plungerHeld && status == GameStatus.Ready)
            {
                plungerHold += dt;
                plungerCharge = Math.Min(1, plungerHold / PlungerC.ChargeTime);
            }
            while (accumulator >= PhysC.FixedDt)
            {
                accumulator -= PhysC.FixedDt;
                StepOnce(PhysC.FixedDt);
                if (!IsRunning) { accumulator = 0; break; }
            }
        }

        void StepOnce(double dt)
        {
            world.Step(dt);
            elapsed += dt;
            Tick(dt);
            ResolveOutcome();
        }

        void Decay(Dictionary<string, float> map, float amount)
        {
            if (map.Count == 0) return;
            keyScratch.Clear(); keyScratch.AddRange(map.Keys);
            for (int i = 0; i < keyScratch.Count; i++)
            {
                float v = map[keyScratch[i]];
                if (v > 0) map[keyScratch[i]] = Math.Max(0, v - amount);
            }
        }

        void Tick(double dt)
        {
            if (status == GameStatus.Playing)
            {
                if (scoring.Update(dt)) Emit(new GameEvent { type = GEvent.ComboLost });
                if (ballSaveTimer > 0) ballSaveTimer = Math.Max(0, ballSaveTimer - dt);
                if (hasTimer) timeLeft = Math.Max(0, timeLeft - dt);
                if (specialTimeLeft > 0)
                {
                    specialTimeLeft -= dt;
                    if (specialTimeLeft <= 0) EndSpecial();
                }
            }
            float bf = (float)(6 * dt);
            Decay(bumperFlash, bf);
            Decay(slingFlash, bf);
            Decay(targetFlash, bf * 0.6f);
            Decay(rampGlow, (float)(dt * 0.8));
            if (coreFlash > 0) coreFlash = Math.Max(0, coreFlash - bf);

            if (timers.Count > 0)
            {
                for (int i = timers.Count - 1; i >= 0; i--)
                {
                    if (i < timers.Count && elapsed >= timers[i].at)
                    {
                        var t = timers[i];
                        timers.RemoveAt(i);
                        t.run();
                    }
                }
            }
        }

        void After(double seconds, Action run) { timers.Add((elapsed + seconds, run)); }

        // ---------- Fizik olayları → kurallar ----------

        void HandlePhysics(PhysicsEvent e)
        {
            Emit(new GameEvent { type = GEvent.Physics, phys = e });
            switch (e.type)
            {
                case PhysType.Launch:
                    status = GameStatus.Playing;
                    ballSaveTimer = saveUsed ? 0 : level.ballSaveSec;
                    break;
                case PhysType.LaneRest:
                    if (status == GameStatus.Playing) { status = GameStatus.Ready; ballSaveTimer = 0; }
                    break;
                case PhysType.Drain: drainPending = true; break;
                case PhysType.Bumper:
                {
                    bumperFlash[e.id] = 1;
                    stats.bumperHits++;
                    int pts = ScoreC.Bumper;
                    foreach (var b in level.bumpers) if (b.id == e.id) { if (b.points > 0) pts = b.points; break; }
                    Hit(e.id, pts, e.x, e.y, true);
                    break;
                }
                case PhysType.Sling: slingFlash[e.id] = 1; Hit(e.id, ScoreC.Sling, e.x, e.y, false); break;
                case PhysType.Target: OnTarget(e.id, e.x, e.y); break;
                case PhysType.Drop: OnDrop(e.id, e.x, e.y); break;
                case PhysType.Lane: OnLane(e.id, e.x, e.y); break;
                case PhysType.Spin: stats.spinnerSpins++; Hit(e.id, ScoreC.Spinner, e.x, e.y, false); break;
                case PhysType.RampEnter: rampGlow[e.id] = 1; Emit(new GameEvent { type = GEvent.RampEnter, id = e.id }); break;
                case PhysType.RampExit: OnRampExit(e.id, e.x, e.y); break;
                case PhysType.Core: OnCore(e.x, e.y); break;
            }
        }

        int Hit(string id, int basePts, double x, double y, bool comboEligible)
        {
            if (status != GameStatus.Playing) return 0;
            if (comboEligible)
            {
                int changed = scoring.RegisterHit(id);
                if (changed >= 2) Emit(new GameEvent { type = GEvent.Combo, chain = changed });
            }
            int points = scoring.Award(basePts);
            Emit(new GameEvent { type = GEvent.Score, points = points, x = x, y = y });
            return points;
        }

        void OnTarget(string id, double x, double y)
        {
            TargetDef def = null;
            foreach (var t in level.targets) if (t.id == id) { def = t; break; }
            if (def == null) return;
            if (targetLit[id]) { Hit(id, 50, x, y, false); return; }
            targetLit[id] = true; targetFlash[id] = 1; stats.targetHits++;
            Hit(id, ScoreC.Target, x, y, true);
            var group = level.targets.FindAll(t => t.group == def.group);
            if (group.TrueForAll(t => targetLit[t.id]))
            {
                CompleteSet("target", def.group, ScoreC.TargetSetBonus, x, y);
                stats.targetSets++;
                After(TimingC.TargetResetDelay, () => { foreach (var t in group) targetLit[t.id] = false; });
                CheckTriggers("targetSet");
            }
        }

        void OnDrop(string id, double x, double y)
        {
            TargetDef def = null;
            foreach (var t in level.dropTargets) if (t.id == id) { def = t; break; }
            if (def == null || dropDown[id]) return;
            dropDown[id] = true;
            if (segmentIndex.TryGetValue(id, out int idx)) world.segments[idx].enabled = false;
            stats.dropHits++;
            Hit(id, ScoreC.DropTarget, x, y, true);
            var group = level.dropTargets.FindAll(t => t.group == def.group);
            if (group.TrueForAll(t => dropDown[t.id]))
            {
                CompleteSet("drop", def.group, ScoreC.DropSetBonus, x, y);
                stats.dropSets++;
                After(TimingC.DropResetDelay, () =>
                {
                    foreach (var t in group)
                    {
                        dropDown[t.id] = false;
                        if (segmentIndex.TryGetValue(t.id, out int i)) world.segments[i].enabled = true;
                    }
                    PushBallOutOfDropTargets();
                });
                CheckTriggers("dropSet");
            }
        }

        void OnLane(string id, double x, double y)
        {
            LaneDef def = null;
            foreach (var l in level.lanes) if (l.id == id) { def = l; break; }
            if (def == null || status != GameStatus.Playing) return;
            if (laneLit[id]) return;
            laneLit[id] = true;
            Hit(id, ScoreC.Lane, x, y, true);
            var group = level.lanes.FindAll(l => l.group == def.group);
            if (group.Count > 1 && group.TrueForAll(l => laneLit[l.id]))
            {
                CompleteSet("lane", def.group, ScoreC.LaneSetBonus, x, y);
                stats.laneSets++;
                After(TimingC.LaneResetDelay, () => { foreach (var l in group) laneLit[l.id] = false; });
                CheckTriggers("laneSet");
            }
        }

        void OnRampExit(string id, double x, double y)
        {
            int pts = ScoreC.Ramp;
            foreach (var r in level.ramps) if (r.id == id) { if (r.points > 0) pts = r.points; break; }
            stats.rampHits++;
            rampGlow[id] = 1;
            Hit(id, pts, x, y, true);
            Emit(new GameEvent { type = GEvent.RampDone, id = id });
            CheckTriggers("rampHits");
        }

        void OnCore(double x, double y)
        {
            coreFlash = 1;
            if (coreCharge <= 0 || status != GameStatus.Playing) return;
            coreCharge--;
            stats.coreHits++;
            Hit("core", ScoreC.Core, x, y, true);
            if (level.hasCore)
            {
                var b = world.ball;
                double dx = b.x - level.core.x, dy = b.y - level.core.y;
                double d = Geo.Hypot(dx, dy); if (d == 0) d = 1;
                const double kick = 2.6;
                double vn = (b.vx * dx + b.vy * dy) / d;
                if (vn < kick) { b.vx += (kick - vn) * (dx / d); b.vy += (kick - vn) * (dy / d); }
            }
            Emit(new GameEvent { type = GEvent.CoreHit });
        }

        void CompleteSet(string kind, string group, int bonus, double x, double y)
        {
            if (status == GameStatus.Playing)
            {
                int points = scoring.AwardBonus(bonus);
                Emit(new GameEvent { type = GEvent.Score, points = points, x = x, y = y });
            }
            if (kind == "target") setTarget++; else if (kind == "drop") setDrop++; else setLane++;
            Emit(new GameEvent { type = GEvent.SetComplete, kind = kind, group = group });
            int mult = scoring.CompleteSet();
            if (mult != 0) Emit(new GameEvent { type = GEvent.Multiplier, value = mult });
            if (level.hasCore && (kind == "target" || kind == "drop"))
            {
                coreCharge = Math.Min(3, coreCharge + 1);
                Emit(new GameEvent { type = GEvent.CoreCharged, value = coreCharge });
            }
        }

        void CheckTriggers(string trigger)
        {
            int n = trigger == "targetSet" ? setTarget : trigger == "dropSet" ? setDrop : trigger == "laneSet" ? setLane : stats.rampHits;
            if (level.hasSpecial && level.specialMode.trigger == trigger && n > 0 && level.specialMode.every > 0 && n % level.specialMode.every == 0)
                StartSpecial(level.specialMode.durationSec, level.specialMode.scoreMultiplier);
            if (level.hasExtra && level.extraBall.trigger == trigger && n > 0 && level.extraBall.every > 0 && n % level.extraBall.every == 0 && extraBallsAwarded < level.extraBall.maxPerGame)
            {
                extraBallsAwarded++; ballsLeft++;
                Emit(new GameEvent { type = GEvent.ExtraBall });
            }
        }

        void StartSpecial(double duration, double factor)
        {
            specialTimeLeft = duration; scoring.specialFactor = factor; stats.specialModes++;
            if (!activeSpecial) { activeSpecial = true; Emit(new GameEvent { type = GEvent.SpecialStart }); }
        }

        void EndSpecial()
        {
            specialTimeLeft = 0; scoring.specialFactor = 1;
            if (activeSpecial) { activeSpecial = false; Emit(new GameEvent { type = GEvent.SpecialEnd }); }
        }

        void RotateLanes(int dir)
        {
            var group = level.lanes.FindAll(l => l.group == "top");
            if (group.Count < 2) return;
            var states = new bool[group.Count];
            bool all = true, none = true;
            for (int i = 0; i < group.Count; i++) { states[i] = laneLit[group[i].id]; all &= states[i]; none &= !states[i]; }
            if (all || none) return;
            for (int i = 0; i < group.Count; i++)
            {
                int src = ((i - dir) % group.Count + group.Count) % group.Count;
                laneLit[group[i].id] = states[src];
            }
        }

        void PushBallOutOfDropTargets()
        {
            var b = world.ball;
            if (b.mode != BallMode.Free) return;
            foreach (var d in level.dropTargets)
            {
                if (dropDown[d.id]) continue;
                double cx = (d.a.x + d.b.x) / 2, cy = (d.a.y + d.b.y) / 2;
                if (Geo.Hypot(b.x - cx, b.y - cy) < b.r + 0.045) { b.y = cy + b.r + 0.02; b.vy = Math.Max(b.vy, 0.4); }
            }
        }

        // ---------- Sonuç kararı ----------

        void ResolveOutcome()
        {
            if (outcomeLocked) return;
            if (!level.endless && status == GameStatus.Playing && Objectives.AllComplete(level.objectives, scoring.score, scoring.maxChain, stats))
            {
                FinishLevel();
                return;
            }
            if (drainPending)
            {
                drainPending = false;
                HandleDrain();
                if (outcomeLocked) return;
            }
            if (hasTimer && timeLeft <= 0 && status == GameStatus.Playing) EndGame(GameOverReason.Time);
        }

        void HandleDrain()
        {
            if (status != GameStatus.Playing) { world.PlaceBallOnPlunger(); return; }
            if (ballSaveTimer > 0)
            {
                ballSaveTimer = 0; saveUsed = true;
                Emit(new GameEvent { type = GEvent.BallSaved });
                RespawnBall();
                return;
            }
            ballsLeft--; ballsLost++; saveUsed = false;
            EndSpecial();
            scoring.ResetForNewBall();
            coreCharge = 0;
            Emit(new GameEvent { type = GEvent.BallLost, ballsLeft = ballsLeft });
            if (ballsLeft <= 0) { EndGame(GameOverReason.Balls); return; }
            RespawnBall();
        }

        void RespawnBall()
        {
            world.PlaceBallOnPlunger();
            status = GameStatus.Ready;
            Emit(new GameEvent { type = GEvent.BallReady });
        }

        void FinishLevel()
        {
            outcomeLocked = true;
            status = GameStatus.LevelComplete;
            plungerHeld = false;
            world.SetFlipper(true, false); world.SetFlipper(false, false);
            int unused = Math.Max(0, ballsLeft - 1);
            int bonus = ScoreC.CompletionBonus + unused * ScoreC.BallBonus;
            scoring.AddRaw(bonus);
            int score = scoring.score;
            int maxCombo = scoring.maxChain;
            int stars = Objectives.ComputeStars(score, level.stars.twoStarScore, level.stars.bonus, unused, maxCombo);
            result = new LevelResult
            {
                levelId = level.id, score = score, bonus = bonus, ballsUsed = ballsLost + 1, ballsLeft = unused, stars = stars,
                maxCombo = maxCombo, bonusAchieved = stars == 3 || (stars == 2 && score < level.stars.twoStarScore), timeSec = elapsed
            };
            Emit(new GameEvent { type = GEvent.LevelComplete, result = result });
        }

        void EndGame(GameOverReason reason)
        {
            outcomeLocked = true;
            status = GameStatus.GameOver;
            gameOverReason = reason;
            plungerHeld = false;
            world.SetFlipper(true, false); world.SetFlipper(false, false);
            Emit(new GameEvent { type = GEvent.GameOver, reason = reason, score = scoring.score, ballsUsed = ballsLost + (reason == GameOverReason.Time ? 1 : 0) });
        }

        // ---------- Okuma ----------

        public int Score => scoring.score;
        public bool SpecialActive => activeSpecial;
        public bool BallSaveActive => ballSaveTimer > 0 && status == GameStatus.Playing;

        public List<ObjectiveProgress> GetObjectives()
        {
            var l = new List<ObjectiveProgress>(level.objectives.Count);
            foreach (var o in level.objectives) l.Add(Objectives.ProgressOf(o, scoring.score, scoring.maxChain, stats));
            return l;
        }

        public HudSnapshot GetHud(HudSnapshot into = null)
        {
            var h = into ?? new HudSnapshot();
            h.score = scoring.score; h.ballsLeft = ballsLeft; h.multiplier = scoring.multiplier; h.chain = scoring.chain;
            h.comboRemaining = scoring.ComboRemaining; h.specialRemaining = specialTimeLeft; h.specialActive = activeSpecial;
            h.hasTimer = hasTimer; h.timeLeft = timeLeft; h.ballSaveActive = BallSaveActive; h.ballSaveLeft = BallSaveActive ? ballSaveTimer : 0;
            h.status = status; h.plungerCharge = plungerCharge;
            return h;
        }
    }
}
