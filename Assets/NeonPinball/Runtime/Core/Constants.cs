// Merkezi sabitler — TS tarafındaki config/constants.ts ile birebir aynı değerler.
// Normalize masa uzayı: x 0→1, y 0→1.8 (yukarıdan aşağı). Fizik double ile çalışır (TS ile eşdeğerlik).
using System;

namespace NeonPinball
{
    public static class TableC
    {
        public const double Width = 1, Height = 1.8, DrainY = 1.76;
    }

    public static class PhysC
    {
        public const double FixedDt = 1.0 / 240.0;
        public const double MaxFrameDt = 1.0 / 20.0;
        public const double Gravity = 2.4;
        public const double BallRadius = 0.022;
        public const double MaxSpeed = 4.4;
        public const double Drag = 0.035;
        public const double WallRestitution = 0.42;
        public const double PostRestitution = 0.55;
        public const double TargetRestitution = 0.38;
        public const double RestThreshold = 0.12;
        public const double ContactFriction = 0.015;
        public const double EventCooldown = 0.06;
        public const int CollisionIterations = 3;
    }

    public static class FlipperC
    {
        public const double Length = 0.165, PivotRadius = 0.027, TipRadius = 0.015;
        public const double RestDeg = 28, ActiveDeg = -32, SpeedUp = 19, SpeedDown = 11, Restitution = 0.36;
        public const double PivotY = 1.62, LeftPivotX = 0.255, RightPivotX = 0.665;
        public const double GuideDeg = 35, GuideWallY = 1.42;
    }

    public static class PlungerC
    {
        public const double RestX = 0.945, RestY = 1.57, LaneMinX = 0.915, LaneMaxX = 0.985;
        public const double ChargeTime = 1.0, MinSpeed = 1.5, MaxSpeed = 4.1, QuickPower = 0.85;
        public const double TapThresholdMs = 170;
    }

    public static class StuckC
    {
        public const double CheckInterval = 0.5, MinTravel = 0.012, NudgeAfter = 2.2;
        public const double NudgeSpeed = 0.7, StrongNudgeSpeed = 1.5;
        public const int TeleportAfterNudges = 4;
        public const double RescueX = 0.46, RescueY = 0.78;
    }

    public static class ScoreC
    {
        public const int Bumper = 100, Sling = 10, Target = 250, DropTarget = 300, Ramp = 500, Spinner = 50, Lane = 100, Core = 3000;
        public const int TargetSetBonus = 1000, DropSetBonus = 5000, LaneSetBonus = 1500, BallBonus = 2000, CompletionBonus = 1000;
    }

    public static class ComboC
    {
        public const double WindowSec = 3;
        public const int MaxLevel = 5;
        public const double Step = 0.5;
    }

    public static class MultC
    {
        public const int Max = 4, SetsPerStep = 2;
    }

    public static class SpinnerC
    {
        public const double SpinGain = 14, MaxOmega = 40, Decay = 2.2, ScoreEveryRad = Math.PI, BallDamping = 0.97;
    }

    public static class TimingC
    {
        public const double TargetResetDelay = 1.0, DropResetDelay = 1.2, LaneResetDelay = 0.8;
        public const int LevelIntroMs = 1100, LevelCompleteDelayMs = 900, GameOverDelayMs = 700, SplashMinMs = 900;
    }

    public static class Game
    {
        public const int TotalLevels = 100, LevelsPerWorld = 10;
        public const int EndlessClassic = 101, EndlessModern = 102;
    }
}
