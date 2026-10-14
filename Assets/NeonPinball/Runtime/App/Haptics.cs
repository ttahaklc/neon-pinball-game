// Titreşim servisi: ayara bağlı, aşırı çağrıya karşı kısıtlı (55 ms), hataya dayanıklı. iOS: UIImpactFeedbackGenerator; Android: Vibrator.
using System.Runtime.InteropServices;
using UnityEngine;

namespace NeonPinball
{
    public static class Haptics
    {
        public static bool Enabled = true;
        static float last = -10;
        const float MinGap = 0.055f;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void NeonHaptic_Init();
        [DllImport("__Internal")] static extern void NeonHaptic_Impact(int style);
        [DllImport("__Internal")] static extern void NeonHaptic_Notify(int type);
        static bool inited;
        static void Ensure() { if (!inited) { inited = true; NeonHaptic_Init(); } }
        static void Impact(int s) { Ensure(); NeonHaptic_Impact(s); }
        static void Notify(int t) { Ensure(); NeonHaptic_Notify(t); }
#elif UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vib; static bool tried; static int sdk;
        static void Ensure()
        {
            if (tried) return; tried = true;
            try
            {
                using (var v = new AndroidJavaClass("android.os.Build$VERSION")) sdk = v.GetStatic<int>("SDK_INT");
                using (var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var act = up.GetStatic<AndroidJavaObject>("currentActivity"))
                    vib = act.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            catch { vib = null; }
        }
        static void Buzz(long ms, int amp)
        {
            Ensure(); if (vib == null) return;
            try
            {
                if (sdk >= 26)
                    using (var ve = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var e = ve.CallStatic<AndroidJavaObject>("createOneShot", ms, amp)) vib.Call("vibrate", e);
                else vib.Call("vibrate", ms);
            }
            catch { }
        }
        static void Impact(int s) { Buzz(s == 0 ? 8 : s == 1 ? 16 : 30, s == 0 ? 70 : s == 1 ? 140 : 240); }
        static void Notify(int t) { Buzz(t == 0 ? 45 : 70, t == 0 ? 200 : 255); }
#else
        static void Impact(int s) { }
        static void Notify(int t) { }
#endif

        static bool Gate(bool force = false)
        {
            if (!Enabled) return false;
            float now = Time.unscaledTime;
            if (!force && now - last < MinGap) return false;
            last = now; return true;
        }

        public static void Light() { if (Gate()) Impact(0); }
        public static void Medium() { if (Gate()) Impact(1); }
        public static void Heavy() { if (Gate(true)) Impact(2); }
        public static void Success() { if (Gate(true)) Notify(0); }
        public static void Error() { if (Gate(true)) Notify(1); }
    }
}
