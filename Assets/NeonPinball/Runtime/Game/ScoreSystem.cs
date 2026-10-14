using System;

namespace NeonPinball
{
    /// <summary>Puan, çarpan (1X–4X), combo zinciri, special çarpanı (TS ScoreSystem.ts).</summary>
    public class ScoreSystem
    {
        public int score;
        public int multiplier = 1;
        public int setsDone;
        public int chain, maxChain;
        public double comboTimer;
        public double specialFactor = 1;
        string lastComboId = "";
        readonly double comboWindow;

        public ScoreSystem(double comboWindowSec) { comboWindow = comboWindowSec; }

        public double ComboFactor => chain <= 1 ? 1 : 1 + (chain - 1) * ComboC.Step;
        public double ComboRemaining => chain > 0 ? Math.Max(0, comboTimer / comboWindow) : 0;

        /// <summary>Zincir değiştiyse yeni değer, değişmediyse 0.</summary>
        public int RegisterHit(string id)
        {
            int changed = 0;
            if (chain > 0 && comboTimer > 0)
            {
                if (id != lastComboId && chain < ComboC.MaxLevel) { chain++; changed = chain; }
            }
            else chain = 1;
            lastComboId = id;
            comboTimer = comboWindow;
            if (chain > maxChain) maxChain = chain;
            return changed;
        }

        // JS Math.round: .5 yukarı yuvarlanır (negatif olmayan değerler için Math.Floor(x+0.5)).
        static int JsRound(double v) => (int)Math.Floor(v + 0.5);

        public int Award(double basePts)
        {
            int p = JsRound(basePts * multiplier * ComboFactor * specialFactor);
            score += p; return p;
        }

        public int AwardBonus(double basePts)
        {
            int p = JsRound(basePts * multiplier);
            score += p; return p;
        }

        public void AddRaw(int pts) { score += pts; }

        /// <summary>Çarpan arttıysa yeni değer, yoksa 0.</summary>
        public int CompleteSet()
        {
            setsDone++;
            int next = Math.Min(MultC.Max, 1 + setsDone / MultC.SetsPerStep);
            if (next > multiplier) { multiplier = next; return next; }
            return 0;
        }

        public bool Update(double dt)
        {
            if (chain == 0) return false;
            comboTimer -= dt;
            if (comboTimer <= 0)
            {
                bool had = chain >= 2;
                chain = 0; comboTimer = 0; lastComboId = "";
                return had;
            }
            return false;
        }

        public void ResetForNewBall()
        {
            multiplier = 1; setsDone = 0; chain = 0; comboTimer = 0; lastComboId = ""; specialFactor = 1;
        }
    }
}
