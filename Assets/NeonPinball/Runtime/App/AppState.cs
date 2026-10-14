// Uygulama durum makinesi (TS stateMachine.ts): izinli geçişler tablosu + geri yığını (navigation stack).
// SAP benzetmesi: workflow status + izinli geçişler; Fiori'de "Geri"nin navigasyon geçmişini kullanması.
using System.Collections.Generic;

namespace NeonPinball
{
    public enum AppState { Boot, MainMenu, LevelSelect, Loading, Playing, Paused, GameOver, LevelComplete, Settings }
    public enum AppAction { BootDone, OpenLevels, OpenSettings, Back, SelectLevel, Loaded, Pause, Resume, Restart, NextLevel, GameOver, LevelComplete, Menu }

    public sealed class AppContext
    {
        public AppState state = AppState.Boot;
        public int levelId = 1;
        public int runId;
        public readonly List<AppState> stack = new List<AppState>();
        public LevelResult result;
        public bool hasGameOver; public GameOverReason overReason; public int overScore;
    }

    public static class AppMachine
    {
        static readonly Dictionary<AppState, AppAction[]> Allowed = new Dictionary<AppState, AppAction[]>
        {
            { AppState.Boot, new[] { AppAction.BootDone } },
            { AppState.MainMenu, new[] { AppAction.OpenLevels, AppAction.OpenSettings, AppAction.SelectLevel } },
            { AppState.LevelSelect, new[] { AppAction.SelectLevel, AppAction.Menu, AppAction.OpenSettings, AppAction.Back } },
            { AppState.Loading, new[] { AppAction.Loaded, AppAction.Menu, AppAction.Pause } },
            { AppState.Playing, new[] { AppAction.Pause, AppAction.GameOver, AppAction.LevelComplete, AppAction.Restart } },
            { AppState.Paused, new[] { AppAction.Resume, AppAction.Restart, AppAction.Menu, AppAction.OpenSettings, AppAction.OpenLevels } },
            { AppState.GameOver, new[] { AppAction.Restart, AppAction.Menu, AppAction.OpenLevels, AppAction.OpenSettings } },
            { AppState.LevelComplete, new[] { AppAction.NextLevel, AppAction.Restart, AppAction.Menu, AppAction.OpenLevels, AppAction.OpenSettings } },
            { AppState.Settings, new[] { AppAction.Back } },
        };

        static bool Live(AppState s) => s == AppState.Loading || s == AppState.Playing || s == AppState.Paused || s == AppState.GameOver || s == AppState.LevelComplete;

        /// <summary>Oyun ekranı bu durumlarda (ve oyundan açılan ayarlar / bölüm listesinde) canlı kalır.</summary>
        public static bool IsInGame(AppContext c)
        {
            if (Live(c.state)) return true;
            if (c.state != AppState.Settings && c.state != AppState.LevelSelect) return false;
            foreach (var s in c.stack) if (Live(s)) return true;
            return false;
        }

        /// <summary>Geçiş uygulandıysa true.</summary>
        public static bool Reduce(AppContext c, AppAction a, int levelId = 0, LevelResult result = null, GameOverReason reason = GameOverReason.Balls, int score = 0)
        {
            if (System.Array.IndexOf(Allowed[c.state], a) < 0) return false;
            switch (a)
            {
                case AppAction.BootDone: c.state = AppState.MainMenu; break;
                case AppAction.OpenLevels: c.stack.Add(c.state); c.state = AppState.LevelSelect; break;
                case AppAction.OpenSettings: c.stack.Add(c.state); c.state = AppState.Settings; break;
                case AppAction.Back:
                    {
                        var prev = c.stack.Count > 0 ? c.stack[c.stack.Count - 1] : AppState.MainMenu;
                        if (c.stack.Count > 0) c.stack.RemoveAt(c.stack.Count - 1);
                        c.state = prev; break;
                    }
                case AppAction.SelectLevel: c.state = AppState.Loading; c.levelId = levelId; c.runId++; c.result = null; c.hasGameOver = false; c.stack.Clear(); break;
                case AppAction.Loaded: c.state = AppState.Playing; break;
                case AppAction.Pause: c.state = AppState.Paused; break;
                case AppAction.Resume: c.state = AppState.Playing; break;
                case AppAction.Restart: c.state = AppState.Loading; c.runId++; c.result = null; c.hasGameOver = false; c.stack.Clear(); break;
                case AppAction.NextLevel: c.state = AppState.Loading; c.levelId = levelId; c.runId++; c.result = null; c.hasGameOver = false; c.stack.Clear(); break;
                case AppAction.GameOver: c.state = AppState.GameOver; c.hasGameOver = true; c.overReason = reason; c.overScore = score; break;
                case AppAction.LevelComplete: c.state = AppState.LevelComplete; c.result = result; break;
                case AppAction.Menu: c.state = AppState.MainMenu; c.result = null; c.hasGameOver = false; c.stack.Clear(); break;
            }
            return true;
        }
    }
}
