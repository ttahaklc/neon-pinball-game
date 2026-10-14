// Yazı tipleri (hepsi OFL): gövde = Noto Sans + Arapça/Devanagari/Çince yedekleri; başlık = Playfair Display (kabin plaketi serifi).
// FontAsset'ler çalışma anında dinamik atlasla kurulur: yalnızca ekrana gelen glifler üretilir.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;

namespace NeonPinball
{
    public static class Fonts
    {
        public static FontAsset Body, Display;
        static bool ready;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() { ready = false; Body = null; Display = null; }

        static FontAsset Make(string res)
        {
            var f = Resources.Load<Font>("Fonts/" + res);
            if (f == null) { Debug.LogWarning("Font missing: " + res); return null; }
            var fa = FontAsset.CreateFontAsset(f, 72, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            fa.name = res;
            return fa;
        }

        public static void Init()
        {
            if (ready) return;
            ready = true;
            Body = Make("NotoSans");
            var ar = Make("NotoSansArabic");
            var hi = Make("NotoSansDevanagari");
            var sc = Make("NotoSansSC");
            var fb = new List<FontAsset>();
            if (ar != null) fb.Add(ar);
            if (hi != null) fb.Add(hi);
            if (sc != null) fb.Add(sc);
            if (Body != null) Body.fallbackFontAssetTable = fb;
            Display = Make("PlayfairDisplay");
            if (Display != null) Display.fallbackFontAssetTable = new List<FontAsset>(fb) { Body };
        }
    }
}
