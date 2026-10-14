// Ekran ölçüleri (dp): piksel yoğunluğu D, arayüz ölçeği K, güvenli alan boşlukları.
using UnityEngine;

namespace NeonPinball
{
    public static class UiEnv
    {
        public static float D = 1, W = 390, H = 800, InTop, InBottom, InLeft, InRight;
        public static bool Landscape => W > H;
        public static float Pad => Ui.S(20);
        public static float ContentW => Mathf.Min(W - InLeft - InRight - Pad * 2, 520);
        public static int PxW, PxH;

        /// <summary>Ekran boyutu / yoğunluk değiştiyse true.</summary>
        public static bool Refresh(int wPx, int hPx, Insets insetsPx)
        {
            float d = UiMetrics.Density();
            float w = wPx / d, h = hPx / d;
            float k = UiMetrics.K(w, h);
            bool changed = wPx != PxW || hPx != PxH || !Mathf.Approximately(d, D) || !Mathf.Approximately(k, Ui.K);
            PxW = wPx; PxH = hPx; D = d; W = w; H = h; Ui.K = k;
            InTop = (float)insetsPx.top / d; InBottom = (float)insetsPx.bottom / d; InLeft = (float)insetsPx.left / d; InRight = (float)insetsPx.right / d;
            return changed;
        }
    }
}
