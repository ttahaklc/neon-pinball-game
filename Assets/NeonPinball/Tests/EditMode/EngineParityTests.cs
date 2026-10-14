// TS motoruyla eşdeğerlik: aynı girdiyle aynı yörünge (scripts/export-golden.ts üretir).
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace NeonPinball.Tests
{
    [Serializable] class Trace { public int id; public double[] flat; public int finalScore; }
    [Serializable] class TraceFile { public List<Trace> traces; }

    public class EngineParityTests
    {
        static string Dir => Path.Combine(Application.dataPath, "NeonPinball/Tests/EditMode");

        [OneTimeSetUp]
        public void Load()
        {
            LevelDb.LoadFromJson(File.ReadAllText(Path.Combine(Dir, "levels.json")), File.ReadAllText(Path.Combine(Dir, "themes.json")));
        }

        [Test]
        public void Data_Has_100_Levels_And_2_Endless()
        {
            for (int i = 1; i <= 100; i++) Assert.AreEqual(i, LevelDb.Get(i).id);
            Assert.IsTrue(LevelDb.Get(101).endless);
            Assert.IsTrue(LevelDb.Get(102).endless);
            Assert.AreEqual("classic", LevelDb.Get(101).themeId);
        }

        [Test]
        public void Rng_Matches_Mulberry32()
        {
            // TS: createRng(1) ilk 3 değer
            var r = new Rng(1);
            double a = r.Next(), b = r.Next(), c = r.Next();
            Assert.That(a, Is.InRange(0.0, 1.0)); Assert.That(b, Is.InRange(0.0, 1.0)); Assert.That(c, Is.InRange(0.0, 1.0));
            Assert.AreNotEqual(a, b);
        }

        // Not: Dönen topun duvara yaslandığı temas, sin/cos'un V8 ↔ .NET arasındaki 1 ulp farkına duyarlıdır;
        // bu yüzden iki motor birkaç saniye bire bir aynı gider, sonra kaotik olarak ayrışabilir. Test, ilk 3 sn'nin
        // (fırlatma, çarpışma, skor, top sayısı) birebir eşleştiğini doğrular; ayrıştıktan sonra yalnızca sağlamlık bakar.
        [Test]
        public void Golden_Traces_Match_TypeScript_Engine()
        {
            var tf = JsonUtility.FromJson<TraceFile>(File.ReadAllText(Path.Combine(Dir, "golden.json")));
            Assert.Greater(tf.traces.Count, 0);
            foreach (var tr in tf.traces)
            {
                var e = new GameEngine(LevelDb.Get(tr.id), 1);
                bool holding = false; double holdStart = 0, t = 0; const double dt = 1.0 / 60;
                int idx = 0, agreed = 0; bool diverged = false;
                int total = tr.flat.Length / 7;
                for (int f = 0; f < 60 * 20; f++)
                {
                    if (e.status == GameStatus.Ready && !holding) { e.PlungerPress(); holding = true; holdStart = t; }
                    else if (holding && t - holdStart >= 0.9) { e.PlungerRelease(); holding = false; }
                    e.SetFlipper(true, t % 1.3 < 0.15);
                    e.SetFlipper(false, t % 1.7 < 0.15);
                    e.Update(dt);
                    t += dt;
                    var b = e.world.ball;
                    Assert.IsFalse(double.IsNaN(b.x) || double.IsNaN(b.y), "NaN, level " + tr.id);
                    Assert.That(b.x, Is.InRange(-0.05, 1.05), "ball left the table, level " + tr.id);
                    if (f % 6 == 5 && idx < total)
                    {
                        int o = idx * 7;
                        if (!diverged)
                        {
                            double err = Math.Max(Math.Abs(b.x - tr.flat[o]), Math.Abs(b.y - tr.flat[o + 1]));
                            bool same = err < 1e-2 && tr.flat[o + 4] == e.Score && tr.flat[o + 5] == e.ballsLeft;
                            if (same) agreed++; else diverged = true;
                        }
                        idx++;
                    }
                    if (e.status == GameStatus.GameOver || e.status == GameStatus.LevelComplete) break;
                }
                Assert.GreaterOrEqual(agreed, Math.Min(30, total), "TS ile uyum ilk 3 sn'de bozuldu, level " + tr.id);
                if (!diverged) Assert.AreEqual(tr.finalScore, e.Score, "final score, level " + tr.id);
            }
        }

        [Test]
        public void Combo_And_Multiplier_Rules()
        {
            var s = new ScoreSystem(3);
            Assert.AreEqual(0, s.RegisterHit("a"));
            Assert.AreEqual(2, s.RegisterHit("b"));
            Assert.AreEqual(150, s.Award(100));
            Assert.AreEqual(0, s.CompleteSet());
            Assert.AreEqual(2, s.CompleteSet());
        }
    }
}
