// Cihaza göre ölçekleme (TS useUi): 390×800 referans. Piksel yoğunluğu (D) ve arayüz ölçeği (k).
using System;
using UnityEngine;

namespace NeonPinball
{
    /// <summary>Ekran boyutu / güvenli alan; test ve ekran görüntüsü için geçersiz kılınabilir.</summary>
    public static class Scr
    {
        public static int OverrideW, OverrideH; public static Insets? OverrideInsets;
        public static int W => OverrideW > 0 ? OverrideW : Screen.width;
        public static int H => OverrideH > 0 ? OverrideH : Screen.height;
        public static Insets SafeInsets()
        {
            if (OverrideInsets.HasValue) return OverrideInsets.Value;
            var r = Screen.safeArea;
            return new Insets { left = r.xMin, right = Screen.width - r.xMax, bottom = r.yMin, top = Screen.height - r.yMax };
        }
    }

    public static class UiMetrics
    {
        /// <summary>1 mantıksal nokta (dp/pt) kaç piksel.</summary>
        /// <summary>Test / ekran görüntüsü için sabit yoğunluk (0 = otomatik).</summary>
        public static float ForceDensity;

        public static float Density()
        {
            if (ForceDensity > 0) return ForceDensity;
            float shortPx = Mathf.Min(Scr.W, Scr.H);
            float dpi = Screen.dpi;
#if UNITY_EDITOR
            dpi = 0;
#endif
            if (dpi <= 0) return Mathf.Clamp(shortPx / 390f, 1f, 4f);
            bool tablet = shortPx / dpi >= 4.0f;   // kısa kenar >= 4 inç → tablet
#if UNITY_IOS
            float d = dpi / (tablet ? 132f : 163f);   // iOS "nokta" yoğunluğu: iPhone 163, iPad 132 ppi
#else
            float d = dpi / 160f;
#endif
            return Mathf.Clamp(Mathf.Round(d * 4f) / 4f, 1f, 4f);
        }

        public static float K(float widthDp, float heightDp)
        {
            bool land = widthDp > heightDp;
            float s = Mathf.Min(widthDp, heightDp), l = Mathf.Max(widthDp, heightDp);
            return Mathf.Clamp(Mathf.Min(s / 390f, l / (land ? 780f : 800f)), 0.78f, 1.25f);
        }

        public static Insets SafeInsetsPx() => Scr.SafeInsets();

        public static TableLayout Layout(int wPx, int hPx, Insets insetsPx)
        {
            float d = Density();
            float k = K(wPx / d, hPx / d);
            // TableLayout piksel cinsinden çalışır; HUD satır yükseklikleri dp → px
            return TableLayout.Compute(wPx, hPx, insetsPx, k * d);
        }
    }
}
