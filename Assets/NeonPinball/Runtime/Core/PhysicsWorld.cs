// Pinball fizik dünyası — TS game/physics/world.ts'in C# karşılığı.
// Unity Physics2D KULLANILMAZ: 100 bölümün dengesi bu motora göre ayarlı; sabit 240 Hz adım, double hassasiyet.
using System;
using System.Collections.Generic;

namespace NeonPinball
{
    public enum BallMode { Plunger, Free, Ramp, Out }
    public enum SegmentKind { Wall, Target, Drop, Sling }
    public enum CircleKind { Post, Bumper, Core }
    public enum PhysType { Launch, Wall, Post, Bumper, Core, Sling, Target, Drop, Flipper, Spin, Lane, RampEnter, RampExit, Drain, LaneRest, Nudge }
    public enum NudgeLevel { Soft, Strong, Rescue }

    public struct PhysicsEvent
    {
        public PhysType type; public string id; public double speed, x, y; public bool left; public NudgeLevel nudge;
    }

    public class Ball { public double x, y, vx, vy, r; public BallMode mode; public int rampIndex; public double rampS; }

    public class Segment
    {
        public string id; public SegmentKind kind; public double ax, ay, bx, by, th, e, kick; public bool enabled;
        public bool hasOneWay; public double owx, owy; public double lastEvent;
    }

    public class CircleCollider { public string id; public CircleKind kind; public double x, y, r, e, kick; public bool enabled; public double lastEvent; }

    public class FlipperBody
    {
        public bool left; public int sign; public double px, py, length, theta, omega, restTheta, activeTheta, speedUp, speedDown; public bool pressed;
    }

    public class SpinnerBody { public SpinnerDef def; public double angle, omega, accum, prevPerp; }
    public class RampBody { public RampDef def; public double[] cumulative; public double total; }

    public class PhysicsWorld
    {
        public readonly Ball ball;
        public readonly List<Segment> segments = new List<Segment>();
        public readonly List<CircleCollider> circles = new List<CircleCollider>();
        public readonly List<FlipperBody> flippers = new List<FlipperBody>();
        public readonly List<SpinnerBody> spinners = new List<SpinnerBody>();
        public readonly List<RampBody> ramps = new List<RampBody>();
        public readonly List<bool> laneInside = new List<bool>();
        public Action<PhysicsEvent> onEvent = _ => { };

        public double time;
        public double gravity;
        public int stuckNudges;
        public int nudgeSoft, nudgeStrong, nudgeRescue;

        readonly TableDef table;
        readonly Rng rng;
        double sinceLaunch, rampCooldown, stuckClock, stuckTime, lastCheckX, lastCheckY;

        public PhysicsWorld(TableDef table, uint seed = 1, double? gravityOverride = null, double? flipperScale = null)
        {
            this.table = table;
            rng = new Rng(seed);
            var lv = table.level;
            gravity = gravityOverride ?? (lv.gravity > 0 ? lv.gravity : PhysC.Gravity);
            ball = new Ball { x = PlungerC.RestX, y = PlungerC.RestY, r = PhysC.BallRadius, mode = BallMode.Plunger, rampIndex = -1 };

            foreach (var w in table.walls)
            {
                segments.Add(new Segment
                {
                    id = "wall", kind = SegmentKind.Wall, ax = w.a.x, ay = w.a.y, bx = w.b.x, by = w.b.y,
                    th = w.thickness >= 0 ? w.thickness : 0.006, e = w.restitution >= 0 ? w.restitution : PhysC.WallRestitution,
                    enabled = true, hasOneWay = w.hasOneWay, owx = w.oneWay.x, owy = w.oneWay.y, lastEvent = -1
                });
            }
            foreach (var s in table.slings)
                segments.Add(new Segment { id = s.id, kind = SegmentKind.Sling, ax = s.a.x, ay = s.a.y, bx = s.b.x, by = s.b.y, th = 0.008, e = 0.3, kick = 1.75, enabled = true, lastEvent = -1 });
            foreach (var t in lv.targets)
                segments.Add(new Segment { id = t.id, kind = SegmentKind.Target, ax = t.a.x, ay = t.a.y, bx = t.b.x, by = t.b.y, th = 0.011, e = PhysC.TargetRestitution, enabled = true, lastEvent = -1 });
            foreach (var d in lv.dropTargets)
                segments.Add(new Segment { id = d.id, kind = SegmentKind.Drop, ax = d.a.x, ay = d.a.y, bx = d.b.x, by = d.b.y, th = 0.011, e = PhysC.TargetRestitution, enabled = true, lastEvent = -1 });
            foreach (var p in lv.posts)
                circles.Add(new CircleCollider { id = "post", kind = CircleKind.Post, x = p.x, y = p.y, r = p.r, e = PhysC.PostRestitution, enabled = true, lastEvent = -1 });
            foreach (var b in lv.bumpers)
                circles.Add(new CircleCollider { id = b.id, kind = CircleKind.Bumper, x = b.x, y = b.y, r = b.r, e = 0.5, kick = b.kick > 0 ? b.kick : 1.9, enabled = true, lastEvent = -1 });
            if (lv.hasCore)
                circles.Add(new CircleCollider { id = "core", kind = CircleKind.Core, x = lv.core.x, y = lv.core.y, r = lv.core.r, e = 0.55, enabled = true, lastEvent = -1 });

            double fs = flipperScale ?? lv.flipperScale;
            flippers.Add(MakeFlipper(true, fs));
            flippers.Add(MakeFlipper(false, fs));
            foreach (var s in lv.spinners) spinners.Add(new SpinnerBody { def = s, angle = s.angle });
            foreach (var r in lv.ramps)
            {
                Geo.BuildPolyline(r.path, out var cum, out var total);
                ramps.Add(new RampBody { def = r, cumulative = cum, total = total });
            }
            for (int i = 0; i < lv.lanes.Count; i++) laneInside.Add(false);
        }

        static FlipperBody MakeFlipper(bool left, double fs) => new FlipperBody
        {
            left = left, sign = left ? 1 : -1,
            px = left ? FlipperC.LeftPivotX : FlipperC.RightPivotX, py = FlipperC.PivotY,
            length = FlipperC.Length * fs, theta = FlipperC.RestDeg * Geo.Deg, restTheta = FlipperC.RestDeg * Geo.Deg,
            activeTheta = FlipperC.ActiveDeg * Geo.Deg, speedUp = FlipperC.SpeedUp, speedDown = FlipperC.SpeedDown
        };

        // ---------- Kontrol ----------

        public void SetFlipper(bool left, bool pressed) => flippers[left ? 0 : 1].pressed = pressed;

        public void PlaceBallOnPlunger()
        {
            ball.x = PlungerC.RestX; ball.y = PlungerC.RestY; ball.vx = 0; ball.vy = 0;
            ball.mode = BallMode.Plunger; ball.rampIndex = -1;
            stuckTime = 0; stuckNudges = 0;
        }

        public void Launch(double power)
        {
            if (ball.mode != BallMode.Plunger) return;
            double p = Geo.Clamp(power, 0, 1);
            double speed = Geo.Lerp(PlungerC.MinSpeed, PlungerC.MaxSpeed, p);
            ball.mode = BallMode.Free; ball.vx = 0; ball.vy = -speed;
            sinceLaunch = 0; stuckTime = 0; lastCheckX = ball.x; lastCheckY = ball.y; stuckClock = 0;
            onEvent(new PhysicsEvent { type = PhysType.Launch, speed = speed });
        }

        // ---------- Adım ----------

        public void Step(double dt)
        {
            time += dt;
            UpdateFlippers(dt);
            UpdateSpinners(dt);
            if (rampCooldown > 0) rampCooldown -= dt;

            var b = ball;
            if (b.mode == BallMode.Ramp) { AdvanceRamp(dt); return; }
            if (b.mode != BallMode.Free) return;

            sinceLaunch += dt;
            b.vy += gravity * dt;
            double drag = 1 - PhysC.Drag * dt;
            b.vx *= drag; b.vy *= drag;
            b.x += b.vx * dt; b.y += b.vy * dt;

            for (int it = 0; it < PhysC.CollisionIterations; it++) CollideAll();

            double speed = Geo.Hypot(b.vx, b.vy);
            if (speed > PhysC.MaxSpeed)
            {
                double k = PhysC.MaxSpeed / speed;
                b.vx *= k; b.vy *= k;
            }

            UpdateSensors();
            CheckLaneRest(speed);
            CheckStuck(dt);
            CheckBounds();

            if (b.mode == BallMode.Free && b.y > TableC.DrainY)
            {
                b.mode = BallMode.Out;
                onEvent(new PhysicsEvent { type = PhysType.Drain });
            }
        }

        void UpdateFlippers(double dt)
        {
            for (int i = 0; i < flippers.Count; i++)
            {
                var f = flippers[i];
                double target = f.pressed ? f.activeTheta : f.restTheta;
                double maxStep = (f.pressed ? f.speedUp : f.speedDown) * dt;
                double delta = Geo.Clamp(target - f.theta, -maxStep, maxStep);
                f.theta += delta;
                f.omega = delta / dt;
            }
        }

        void UpdateSpinners(double dt)
        {
            for (int i = 0; i < spinners.Count; i++)
            {
                var s = spinners[i];
                if (Math.Abs(s.omega) < 0.01 && s.omega != 0) s.omega = 0;
                if (s.omega == 0) continue;
                s.angle += s.omega * dt;
                s.accum += Math.Abs(s.omega) * dt;
                while (s.accum >= SpinnerC.ScoreEveryRad)
                {
                    s.accum -= SpinnerC.ScoreEveryRad;
                    onEvent(new PhysicsEvent { type = PhysType.Spin, id = s.def.id, x = s.def.x, y = s.def.y });
                }
                s.omega *= Math.Exp(-SpinnerC.Decay * dt);
            }
        }

        // ---------- Çarpışma ----------

        void CollideAll()
        {
            var b = ball;
            for (int i = 0; i < flippers.Count; i++) CollideFlipper(flippers[i]);
            for (int i = 0; i < circles.Count; i++) CollideCircle(circles[i]);
            for (int i = 0; i < segments.Count; i++) CollideSegment(segments[i]);
            if (double.IsNaN(b.x) || double.IsInfinity(b.x) || double.IsNaN(b.y) || double.IsInfinity(b.y) ||
                double.IsNaN(b.vx) || double.IsInfinity(b.vx) || double.IsNaN(b.vy) || double.IsInfinity(b.vy))
                Rescue();
        }

        double Resolve(double nx, double ny, double pen, double e, double svx, double svy)
        {
            var b = ball;
            b.x += nx * pen; b.y += ny * pen;
            double rvx = b.vx - svx, rvy = b.vy - svy;
            double vn = rvx * nx + rvy * ny;
            if (vn >= 0) return 0;
            double approach = -vn;
            double eff = approach < PhysC.RestThreshold ? 0 : e;
            double j = -(1 + eff) * vn;
            b.vx += j * nx; b.vy += j * ny;
            double tx = -ny, ty = nx;
            double vt = rvx * tx + rvy * ty;
            b.vx -= vt * PhysC.ContactFriction * tx;
            b.vy -= vt * PhysC.ContactFriction * ty;
            return approach;
        }

        void CollideSegment(Segment s)
        {
            if (!s.enabled) return;
            var b = ball;
            double rr = b.r + s.th;
            if (b.x + rr < Math.Min(s.ax, s.bx) || b.x - rr > Math.Max(s.ax, s.bx) ||
                b.y + rr < Math.Min(s.ay, s.by) || b.y - rr > Math.Max(s.ay, s.by)) return;
            Geo.ClosestOnSegment(b.x, b.y, s.ax, s.ay, s.bx, s.by, out double cx, out double cy, out _);
            double dx = b.x - cx, dy = b.y - cy, d2 = dx * dx + dy * dy;
            if (d2 >= rr * rr) return;

            double nx, ny, pen;
            if (s.hasOneWay)
            {
                double sd = (b.x - s.ax) * s.owx + (b.y - s.ay) * s.owy;
                if (sd < 0) return;
                nx = s.owx; ny = s.owy; pen = Math.Max(0, rr - sd);
            }
            else
            {
                double d = Math.Sqrt(d2);
                if (d > 1e-9) { nx = dx / d; ny = dy / d; pen = rr - d; }
                else
                {
                    double sx = s.bx - s.ax, sy = s.by - s.ay;
                    double l = Geo.Hypot(sx, sy); if (l == 0) l = 1;
                    nx = -sy / l; ny = sx / l; pen = rr;
                }
            }

            double approach = Resolve(nx, ny, pen, s.e, 0, 0);
            if (approach <= 0) return;
            if (s.kick > 0 && approach > 0.1)
            {
                double vnAfter = b.vx * nx + b.vy * ny;
                if (vnAfter < s.kick) { b.vx += (s.kick - vnAfter) * nx; b.vy += (s.kick - vnAfter) * ny; }
            }
            if (time - s.lastEvent < PhysC.EventCooldown) return;
            s.lastEvent = time;
            switch (s.kind)
            {
                case SegmentKind.Wall:
                    if (approach > 0.5) onEvent(new PhysicsEvent { type = PhysType.Wall, speed = approach, x = cx, y = cy });
                    break;
                case SegmentKind.Target: onEvent(new PhysicsEvent { type = PhysType.Target, id = s.id, speed = approach, x = cx, y = cy }); break;
                case SegmentKind.Drop: onEvent(new PhysicsEvent { type = PhysType.Drop, id = s.id, speed = approach, x = cx, y = cy }); break;
                case SegmentKind.Sling: onEvent(new PhysicsEvent { type = PhysType.Sling, id = s.id, speed = approach, x = cx, y = cy }); break;
            }
        }

        void CollideCircle(CircleCollider c)
        {
            if (!c.enabled) return;
            var b = ball;
            double dx = b.x - c.x, dy = b.y - c.y, rr = b.r + c.r, d2 = dx * dx + dy * dy;
            if (d2 >= rr * rr) return;
            double d = Math.Sqrt(d2), nx, ny;
            if (d > 1e-9) { nx = dx / d; ny = dy / d; } else { nx = 0; ny = -1; }
            double approach = Resolve(nx, ny, rr - d, c.e, 0, 0);
            if (approach <= 0) return;
            if (c.kick > 0)
            {
                double vnAfter = b.vx * nx + b.vy * ny;
                if (vnAfter < c.kick) { b.vx += (c.kick - vnAfter) * nx; b.vy += (c.kick - vnAfter) * ny; }
            }
            if (time - c.lastEvent < PhysC.EventCooldown) return;
            c.lastEvent = time;
            double x = c.x + nx * c.r, y = c.y + ny * c.r;
            if (c.kind == CircleKind.Bumper) onEvent(new PhysicsEvent { type = PhysType.Bumper, id = c.id, speed = approach, x = x, y = y });
            else if (c.kind == CircleKind.Core) onEvent(new PhysicsEvent { type = PhysType.Core, id = c.id, speed = approach, x = x, y = y });
            else if (approach > 0.5) onEvent(new PhysicsEvent { type = PhysType.Post, speed = approach, x = x, y = y });
        }

        void CollideFlipper(FlipperBody f)
        {
            var b = ball;
            double dirX = f.sign * Math.Cos(f.theta), dirY = Math.Sin(f.theta);
            double tipX = f.px + dirX * f.length, tipY = f.py + dirY * f.length;
            double reach = f.length + FlipperC.PivotRadius + b.r;
            if (Math.Abs(b.x - f.px) > reach || Math.Abs(b.y - f.py) > reach) return;

            Geo.ClosestOnSegment(b.x, b.y, f.px, f.py, tipX, tipY, out double cx, out double cy, out double t);
            double rad = Geo.Lerp(FlipperC.PivotRadius, FlipperC.TipRadius, t);
            double rr = b.r + rad;
            double dx = b.x - cx, dy = b.y - cy, d2 = dx * dx + dy * dy;
            if (d2 >= rr * rr) return;

            double d = Math.Sqrt(d2), nx, ny;
            if (d > 1e-9) { nx = dx / d; ny = dy / d; }
            else
            {
                nx = -f.sign * Math.Sin(f.theta) * -1;
                ny = -Math.Cos(f.theta);
                double l = Geo.Hypot(nx, ny); if (l == 0) l = 1;
                nx /= l; ny /= l;
            }
            double s = t * f.length;
            double svx = f.omega * s * (-f.sign * Math.Sin(f.theta));
            double svy = f.omega * s * Math.Cos(f.theta);
            double approach = Resolve(nx, ny, rr - d, FlipperC.Restitution, svx, svy);
            if (approach > 0.25 || (Math.Abs(f.omega) > 2 && approach > 0.1))
                onEvent(new PhysicsEvent { type = PhysType.Flipper, left = f.left, speed = approach + Math.Abs(f.omega) * s, x = b.x, y = b.y });
        }

        // ---------- Sensörler ----------

        void UpdateSensors()
        {
            var b = ball;
            var lanes = table.level.lanes;
            for (int i = 0; i < lanes.Count; i++)
            {
                var l = lanes[i];
                bool inside = Geo.Hypot(b.x - l.x, b.y - l.y) < l.r;
                if (inside && !laneInside[i]) onEvent(new PhysicsEvent { type = PhysType.Lane, id = l.id, x = l.x, y = l.y });
                laneInside[i] = inside;
            }

            for (int i = 0; i < spinners.Count; i++)
            {
                var s = spinners[i];
                double ux = Math.Cos(s.def.angle), uy = Math.Sin(s.def.angle);
                double rx = b.x - s.def.x, ry = b.y - s.def.y;
                double along = rx * ux + ry * uy;
                double perp = -rx * uy + ry * ux;
                if (Math.Abs(along) < s.def.length / 2 + b.r * 0.3)
                {
                    if (s.prevPerp * perp <= 0 && Math.Abs(perp) < b.r * 1.2)
                    {
                        double vPerp = -b.vx * uy + b.vy * ux;
                        double dir = vPerp >= 0 ? 1 : -1;
                        s.omega = Geo.Clamp(s.omega * 0.5 + dir * Math.Min(Math.Abs(vPerp) * SpinnerC.SpinGain, SpinnerC.MaxOmega), -SpinnerC.MaxOmega, SpinnerC.MaxOmega);
                        b.vx *= SpinnerC.BallDamping; b.vy *= SpinnerC.BallDamping;
                    }
                    s.prevPerp = perp;
                }
                else s.prevPerp = 0;
            }

            if (rampCooldown <= 0)
            {
                double speed = Geo.Hypot(b.vx, b.vy);
                for (int i = 0; i < ramps.Count; i++)
                {
                    var r = ramps[i].def;
                    if (Geo.Hypot(b.x - r.entry.x, b.y - r.entry.y) < r.entryRadius && speed >= r.minSpeed)
                    {
                        double dot = (b.vx * r.entryDir.x + b.vy * r.entryDir.y) / (speed == 0 ? 1 : speed);
                        if (dot > 0.35)
                        {
                            b.mode = BallMode.Ramp; b.rampIndex = i; b.rampS = 0; b.vx = 0; b.vy = 0;
                            onEvent(new PhysicsEvent { type = PhysType.RampEnter, id = r.id, x = r.entry.x, y = r.entry.y });
                            return;
                        }
                    }
                }
            }
        }

        void AdvanceRamp(double dt)
        {
            var b = ball;
            if (b.rampIndex < 0 || b.rampIndex >= ramps.Count) { b.mode = BallMode.Free; return; }
            var ramp = ramps[b.rampIndex];
            b.rampS += ramp.def.travelSpeed * dt;
            var path = ramp.def.path;
            if (b.rampS >= ramp.total)
            {
                V2 last = path[path.Count - 1];
                V2 prev = path.Count >= 2 ? path[path.Count - 2] : last;
                double dx = last.x - prev.x, dy = last.y - prev.y;
                double l = Geo.Hypot(dx, dy); if (l == 0) l = 1;
                b.x = last.x; b.y = last.y;
                b.vx = dx / l * ramp.def.exitSpeed; b.vy = dy / l * ramp.def.exitSpeed;
                b.mode = BallMode.Free;
                rampCooldown = 0.5;
                onEvent(new PhysicsEvent { type = PhysType.RampExit, id = ramp.def.id, x = last.x, y = last.y });
                b.rampIndex = -1;
                return;
            }
            int i = 1;
            while (i < ramp.cumulative.Length - 1 && ramp.cumulative[i] < b.rampS) i++;
            double segLen = ramp.cumulative[i] - ramp.cumulative[i - 1]; if (segLen == 0) segLen = 1;
            double t = (b.rampS - ramp.cumulative[i - 1]) / segLen;
            b.x = Geo.Lerp(path[i - 1].x, path[i].x, t);
            b.y = Geo.Lerp(path[i - 1].y, path[i].y, t);
        }

        // ---------- Güvenlik ağları ----------

        void CheckLaneRest(double speed)
        {
            var b = ball;
            if (sinceLaunch > 0.5 && b.x > PlungerC.LaneMinX && b.x < PlungerC.LaneMaxX && b.y > PlungerC.RestY - 0.1 && speed < 0.35)
            {
                PlaceBallOnPlunger();
                onEvent(new PhysicsEvent { type = PhysType.LaneRest });
            }
        }

        void CheckStuck(double dt)
        {
            var b = ball;
            stuckClock += dt;
            if (stuckClock < StuckC.CheckInterval) return;
            stuckClock = 0;
            double moved = Geo.Hypot(b.x - lastCheckX, b.y - lastCheckY);
            lastCheckX = b.x; lastCheckY = b.y;
            if (moved >= StuckC.MinTravel) { stuckTime = 0; stuckNudges = 0; return; }
            stuckTime += StuckC.CheckInterval;
            if (stuckTime < StuckC.NudgeAfter) return;
            stuckTime = StuckC.NudgeAfter - StuckC.CheckInterval;
            stuckNudges++;
            if (stuckNudges >= StuckC.TeleportAfterNudges) { Rescue(); return; }
            bool strong = stuckNudges >= 2;
            double speed = strong ? StuckC.StrongNudgeSpeed : StuckC.NudgeSpeed;
            double angle = -Math.PI / 2 + (rng.Next() - 0.5) * 1.6;
            b.vx += Math.Cos(angle) * speed; b.vy += Math.Sin(angle) * speed;
            if (strong) nudgeStrong++; else nudgeSoft++;
            onEvent(new PhysicsEvent { type = PhysType.Nudge, nudge = strong ? NudgeLevel.Strong : NudgeLevel.Soft, x = b.x, y = b.y });
        }

        void CheckBounds()
        {
            var b = ball;
            bool outside = b.x < -0.02 || b.x > TableC.Width + 0.02 || b.y < -0.1;
            if (!outside) return;
            if (b.y > FlipperC.PivotY) { b.mode = BallMode.Out; onEvent(new PhysicsEvent { type = PhysType.Drain }); return; }
            Rescue();
        }

        void Rescue()
        {
            var b = ball;
            double fx = b.x, fy = b.y;
            b.x = StuckC.RescueX; b.y = StuckC.RescueY;
            b.vx = (rng.Next() - 0.5) * 0.4; b.vy = 0.2;
            stuckNudges = 0; stuckTime = 0; nudgeRescue++;
            onEvent(new PhysicsEvent { type = PhysType.Nudge, nudge = NudgeLevel.Rescue, x = fx, y = fy });
        }
    }
}
