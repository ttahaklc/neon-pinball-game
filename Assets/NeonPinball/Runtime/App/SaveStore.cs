// Kalıcı veri: ayarlar + ilerleme (TS storage.ts / saveSchema.ts). Sürümlü şema, bozuk veriye dayanıklı, yazma debounce'lu.
// SAP benzetmesi: kullanıcı parametreleri (SU3) + iş ilerleme tablosu.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NeonPinball
{
    [Serializable]
    public class SettingsData
    {
        public bool music = true, sfx = true, haptics = true, debug;
        public string language = "auto";
    }

    [Serializable] public class LevelProgress { public int id, best, stars; public bool completed; }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public SettingsData settings = new SettingsData();
        public List<LevelProgress> levels = new List<LevelProgress>();
        public bool tutorialSeen;
        public int endlessClassic, endlessModern;
    }

    public static class SaveStore
    {
        public static SaveData Data { get; private set; } = new SaveData();
        public static event Action Changed;
        static bool dirty; static float dirtyAt;
        static string PathFile => Path.Combine(Application.persistentDataPath, "neon-pinball.save.json");
        /// <summary>Test / ekran görüntüsü: diske yazma.</summary>
        public static bool Volatile;

        public static void Load()
        {
            try
            {
                if (!Volatile && File.Exists(PathFile)) Data = Sanitize(JsonUtility.FromJson<SaveData>(File.ReadAllText(PathFile)));
                else Data = new SaveData();
            }
            catch { Data = new SaveData(); }
            Changed?.Invoke();
        }

        static SaveData Sanitize(SaveData d)
        {
            if (d == null) return new SaveData();
            d.settings = d.settings ?? new SettingsData();
            if (d.settings.language != "auto" && !I18n.IsLang(d.settings.language)) d.settings.language = "auto";
            d.levels = d.levels ?? new List<LevelProgress>();
            var seen = new HashSet<int>();
            d.levels.RemoveAll(l => l == null || l.id < 1 || l.id > Game.TotalLevels || !seen.Add(l.id));
            foreach (var l in d.levels) { l.best = Mathf.Max(0, l.best); l.stars = Mathf.Clamp(l.stars, 0, 3); if (l.stars > 0) l.completed = true; }
            d.endlessClassic = Mathf.Max(0, d.endlessClassic); d.endlessModern = Mathf.Max(0, d.endlessModern);
            return d;
        }

        static void Commit()
        {
            dirty = true; dirtyAt = Time.realtimeSinceStartup;
            Changed?.Invoke();
        }

        /// <summary>Her karede çağrılır: bekleyen yazmayı 250 ms sonra yapar.</summary>
        public static void Tick()
        {
            if (dirty && Time.realtimeSinceStartup - dirtyAt > 0.25f) Flush();
        }

        public static void Flush()
        {
            if (!dirty) return;
            dirty = false;
            if (Volatile) return;
            try
            {
                string tmp = PathFile + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(Data));
                if (File.Exists(PathFile)) File.Delete(PathFile);
                File.Move(tmp, PathFile);
            }
            catch (Exception e) { Debug.LogWarning("Save failed: " + e.Message); }
        }

        public static LevelProgress Progress(int id)
        {
            foreach (var l in Data.levels) if (l.id == id) return l;
            return null;
        }

        public static int Stars(int id) => Progress(id)?.stars ?? 0;
        public static bool Completed(int id) => Progress(id)?.completed ?? false;
        public static int EndlessBest(string variant) => variant == "modern" ? Data.endlessModern : Data.endlessClassic;

        public static void SetMusic(bool v) { Data.settings.music = v; Commit(); }
        public static void SetSfx(bool v) { Data.settings.sfx = v; Commit(); }
        public static void SetHaptics(bool v) { Data.settings.haptics = v; Commit(); }
        public static void SetDebug(bool v) { Data.settings.debug = v; Commit(); }
        public static void SetLanguage(string code) { Data.settings.language = code; Commit(); }
        public static void MarkTutorialSeen() { if (!Data.tutorialSeen) { Data.tutorialSeen = true; Commit(); } }

        /// <summary>Bölüm sonucu: en iyi skor her zaman, yıldız/tamamlama yalnızca bitişte.</summary>
        public static bool Record(int levelId, int score, bool completed, int stars)
        {
            var p = Progress(levelId);
            if (p == null) { p = new LevelProgress { id = levelId }; Data.levels.Add(p); }
            bool newBest = score > p.best;
            p.best = Mathf.Max(p.best, score);
            if (completed) { p.stars = Mathf.Max(p.stars, stars); p.completed = true; }
            Commit();
            return newBest;
        }

        public static bool RecordEndless(string variant, int score)
        {
            int prev = EndlessBest(variant);
            if (score <= prev) return false;
            if (variant == "modern") Data.endlessModern = score; else Data.endlessClassic = score;
            Commit();
            return true;
        }

        public static void ResetProgress()
        {
            Data.levels.Clear(); Data.endlessClassic = 0; Data.endlessModern = 0; Commit();
        }

        /// <summary>Bölüm açık mı? 1. bölüm her zaman; diğerleri gereken bölüm tamamlanınca. Hata ayıklama açıkken hepsi açık.</summary>
        public static bool IsUnlocked(int levelId, bool ignoreDebug = false)
        {
            if (levelId < 1 || levelId > Game.TotalLevels) return false;
            if (Data.settings.debug && !ignoreDebug) return true;
            var lv = LevelDb.Get(levelId);
            int req = lv.unlockRequirement > 0 ? lv.unlockRequirement : levelId - 1;
            if (levelId == 1 || req <= 0) return true;
            return Completed(req);
        }

        /// <summary>İlk tamamlanmamış açık bölüm (hepsi bittiyse sonuncu).</summary>
        public static int NextPlayable()
        {
            for (int id = 1; id <= Game.TotalLevels; id++)
                if (IsUnlocked(id, true) && !Completed(id)) return id;
            return Game.TotalLevels;
        }

        public static int TotalGems() { int n = 0; foreach (var l in Data.levels) n += l.stars; return n; }
        public static int CompletedCount() { int n = 0; foreach (var l in Data.levels) if (l.completed) n++; return n; }
        public static int BestOverall() { int b = 0; foreach (var l in Data.levels) b = Mathf.Max(b, l.best); return b; }

        /// <summary>Etkin dil: ayar 'auto' ise cihaz dili.</summary>
        public static string EffectiveLanguage() => Data.settings.language == "auto" ? I18n.DetectDeviceLanguage() : Data.settings.language;
    }
}
