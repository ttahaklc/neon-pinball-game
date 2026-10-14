// Bölüm / tema verisi. TS tarafındaki LevelConfig ile aynı alanlar; JSON'dan (Resources/Data) yüklenir.
using System;
using System.Collections.Generic;

namespace NeonPinball
{
    [Serializable] public struct V2 { public double x, y; public V2(double x, double y) { this.x = x; this.y = y; } }

    [Serializable] public class BumperDef { public string id; public double x, y, r; public string color; public int points; public double kick; }
    [Serializable] public class TargetDef { public string id, group, label; public V2 a, b; public string color; }
    [Serializable] public class RampDef
    {
        public string id; public V2 entry; public double entryRadius; public V2 entryDir;
        public double minSpeed, travelSpeed, exitSpeed; public List<V2> path; public double width; public string color; public int points;
    }
    [Serializable] public class LaneDef { public string id, group, label; public double x, y, r; public string color; }
    [Serializable] public class SpinnerDef { public string id; public double x, y, length, angle; public string color; }
    [Serializable] public class PostDef { public double x, y, r; public string color; }
    [Serializable] public class WallDef
    {
        public V2 a, b; public double restitution = -1, thickness = -1; public string style; public bool hasOneWay; public V2 oneWay;
    }
    [Serializable] public class CoreDef { public double x, y, r; }
    [Serializable] public class InsertDef { public string kind; public double x, y, size, rotation; public string color; public double phase; }
    [Serializable] public class ObjectiveDef { public string kind; public int goal; }
    [Serializable] public class SpecialModeRule { public string trigger; public int every; public double durationSec, scoreMultiplier; }
    [Serializable] public class ExtraBallRule { public string trigger; public int every, maxPerGame; }
    [Serializable] public class BonusObjective { public string kind; public double value; }
    [Serializable] public class StarsDef { public double twoStarScore; public BonusObjective bonus; }

    [Serializable]
    public class LevelConfig
    {
        public int id; public string themeId; public bool endless; public double difficulty, gravity; public int ballCount;
        public double launchStrength, ballSaveSec; public double timerSec; public bool hasTimer;
        public double comboWindowSec, flipperScale; public double scoreGoal;
        public List<ObjectiveDef> objectives;
        public StarsDef stars;
        public List<BumperDef> bumpers; public List<TargetDef> targets; public List<TargetDef> dropTargets;
        public List<RampDef> ramps; public List<LaneDef> lanes; public List<SpinnerDef> spinners; public List<PostDef> posts; public List<WallDef> walls;
        public bool slings; public CoreDef core; public bool hasCore; public List<InsertDef> inserts;
        public SpecialModeRule specialMode; public bool hasSpecial; public ExtraBallRule extraBall; public bool hasExtra;
        public int unlockRequirement;
    }

    [Serializable] public class LevelFile { public List<LevelConfig> levels; }

    [Serializable] public class SwatchEntry { public string name, core, glow, dark; }
    [Serializable]
    public class ThemeDef
    {
        public string id; public string[] bg; public string[] outer;
        public string primary, secondary, accent, wall, decor, core; public int seed; public string look, ball, glyph;
        public List<SwatchEntry> palette;

        public SwatchEntry Swatch(string color)
        {
            foreach (var s in palette) if (s.name == color) return s;
            return palette[0];
        }
    }
    [Serializable] public class ThemeFile { public List<ThemeDef> themes; }

    [Serializable] public class LangEntry { public string k, v; }
    [Serializable] public class LangFile { public string code; public List<LangEntry> entries; }

    public static class LevelDb
    {
        private static List<LevelConfig> levels;
        private static Dictionary<string, ThemeDef> themes;

        public static void Load()
        {
            if (levels != null) return;
            var lf = UnityEngine.JsonUtility.FromJson<LevelFile>(UnityEngine.Resources.Load<UnityEngine.TextAsset>("Data/levels").text);
            levels = lf.levels;
            var tf = UnityEngine.JsonUtility.FromJson<ThemeFile>(UnityEngine.Resources.Load<UnityEngine.TextAsset>("Data/themes").text);
            themes = new Dictionary<string, ThemeDef>();
            foreach (var t in tf.themes) themes[t.id] = t;
        }

        /// <summary>Test / editör: JSON metninden yükle.</summary>
        public static void LoadFromJson(string levelsJson, string themesJson)
        {
            levels = UnityEngine.JsonUtility.FromJson<LevelFile>(levelsJson).levels;
            themes = new Dictionary<string, ThemeDef>();
            foreach (var t in UnityEngine.JsonUtility.FromJson<ThemeFile>(themesJson).themes) themes[t.id] = t;
        }

        public static LevelConfig Get(int id)
        {
            Load();
            foreach (var l in levels) if (l.id == id) return l;
            throw new ArgumentException("Unknown level: " + id);
        }

        public static ThemeDef Theme(string id) { Load(); return themes[id]; }
        public static int WorldOf(int levelId) => (levelId - 1) / Game.LevelsPerWorld;
    }
}
