using System;
using System.Collections.Generic;

namespace NeonPinball
{
    public class GameStats
    {
        public int targetSets, targetHits, rampHits, dropHits, dropSets, bumperHits, spinnerSpins, laneSets, specialModes, coreHits;

        public int Get(string kind)
        {
            switch (kind)
            {
                case "targetSets": return targetSets;
                case "targetHits": return targetHits;
                case "rampHits": return rampHits;
                case "dropHits": return dropHits;
                case "dropSets": return dropSets;
                case "bumperHits": return bumperHits;
                case "spinnerSpins": return spinnerSpins;
                case "laneSets": return laneSets;
                case "specialModes": return specialModes;
                case "coreHits": return coreHits;
                default: return 0;
            }
        }
    }

    public struct ObjectiveProgress { public string kind; public int goal, value; public bool done; }

    public static class Objectives
    {
        public static ObjectiveProgress ProgressOf(ObjectiveDef def, int score, int maxCombo, GameStats stats)
        {
            int value = def.kind == "score" ? score : def.kind == "comboReach" ? maxCombo : stats.Get(def.kind);
            return new ObjectiveProgress { kind = def.kind, goal = def.goal, value = Math.Min(value, def.goal), done = value >= def.goal };
        }

        public static bool AllComplete(List<ObjectiveDef> defs, int score, int maxCombo, GameStats stats)
        {
            for (int i = 0; i < defs.Count; i++) if (!ProgressOf(defs[i], score, maxCombo, stats).done) return false;
            return true;
        }

        public static bool BonusAchieved(BonusObjective bonus, int ballsLeft, int maxCombo, int score)
        {
            switch (bonus.kind)
            {
                case "ballsLeft": return ballsLeft >= bonus.value;
                case "combo": return maxCombo >= bonus.value;
                case "score": return score >= bonus.value;
                default: return false;
            }
        }

        public static int ComputeStars(int score, double twoStarScore, BonusObjective bonus, int ballsLeft, int maxCombo)
        {
            int stars = 1;
            if (score >= twoStarScore) stars++;
            if (BonusAchieved(bonus, ballsLeft, maxCombo, score)) stars++;
            return stars;
        }
    }
}
