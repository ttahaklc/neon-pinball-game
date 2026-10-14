// Çok dilli metin katmanı (SAP benzetmesi: i18n.properties + oturum dili SY-LANGU). 7 dil, JSON'dan (Resources/Data/lang_xx).
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NeonPinball
{
    public class LanguageInfo
    {
        public string code, native; public bool rtl; public string thousands;
        public LanguageInfo(string c, string n, bool r, string t) { code = c; native = n; rtl = r; thousands = t; }
    }

    public static class I18n
    {
        public static readonly LanguageInfo[] Languages =
        {
            new LanguageInfo("tr", "Türkçe", false, "."),
            new LanguageInfo("en", "English", false, ","),
            new LanguageInfo("zh", "简体中文", false, ","),
            new LanguageInfo("hi", "हिन्दी", false, ","),
            new LanguageInfo("es", "Español", false, "."),
            new LanguageInfo("ar", "العربية", true, ","),
            new LanguageInfo("fr", "Français", false, " "),
        };

        static readonly Dictionary<string, Dictionary<string, string>> dicts = new Dictionary<string, Dictionary<string, string>>();
        static Dictionary<string, string> active;
        public static string Current { get; private set; } = "tr";
        public static event Action Changed;

        public static bool IsLang(string code) { foreach (var l in Languages) if (l.code == code) return true; return false; }

        public static LanguageInfo Info(string code = null)
        {
            code = code ?? Current;
            foreach (var l in Languages) if (l.code == code) return l;
            return Languages[0];
        }

        static Dictionary<string, string> Load(string code)
        {
            if (dicts.TryGetValue(code, out var d)) return d;
            var ta = Resources.Load<TextAsset>("Data/lang_" + code);
            d = new Dictionary<string, string>();
            if (ta != null)
            {
                var f = JsonUtility.FromJson<LangFile>(ta.text);
                foreach (var e in f.entries) d[e.k] = e.v;
            }
            dicts[code] = d;
            return d;
        }

        /// <summary>Test için: JSON'dan doğrudan yükle.</summary>
        public static void LoadFromJson(string code, string json)
        {
            var d = new Dictionary<string, string>();
            foreach (var e in JsonUtility.FromJson<LangFile>(json).entries) d[e.k] = e.v;
            dicts[code] = d;
        }

        public static void SetLanguage(string code)
        {
            if (!IsLang(code)) code = "en";
            Current = code;
            active = Load(code);
            Changed?.Invoke();
        }

        /// <summary>Telefonun dili → desteklenen dil; yoksa İngilizce.</summary>
        public static string DetectDeviceLanguage()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Turkish: return "tr";
                case SystemLanguage.English: return "en";
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional: return "zh";
                case SystemLanguage.Hindi: return "hi";
                case SystemLanguage.Spanish: return "es";
                case SystemLanguage.Arabic: return "ar";
                case SystemLanguage.French: return "fr";
                default: return "en";
            }
        }

        static readonly Regex Param = new Regex(@"\{(\w+)\}");

        public static string T(string key, params (string k, object v)[] args)
        {
            if (active == null) SetLanguage(Current);
            if (!active.TryGetValue(key, out var raw)) return key;
            if (args == null || args.Length == 0) return raw;
            return Param.Replace(raw, m =>
            {
                foreach (var a in args) if (a.k == m.Groups[1].Value) return Convert.ToString(a.v, System.Globalization.CultureInfo.InvariantCulture);
                return m.Value;
            });
        }

        public static bool Has(string key) { if (active == null) SetLanguage(Current); return active.ContainsKey(key); }

        public static string WorldName(int world)
        {
            string k = "worldNames." + world;
            return Has(k) ? T(k) : T("worldN", ("n", world + 1));
        }

        public static string LevelName(int id) => WorldName((id - 1) / 10) + " " + ((id - 1) % 10 + 1);

        public static string FormatNumber(double n)
        {
            string sep = Info().thousands;
            string s = ((long)Math.Floor(n + 0.5)).ToString(System.Globalization.CultureInfo.InvariantCulture);
            return Regex.Replace(s, @"\B(?=(\d{3})+(?!\d))", sep);
        }
    }
}
