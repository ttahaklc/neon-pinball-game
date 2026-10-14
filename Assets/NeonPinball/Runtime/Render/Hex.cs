using System.Collections.Generic;
using UnityEngine;

namespace NeonPinball
{
    public static class Hex
    {
        static readonly Dictionary<string, Color32> cache = new Dictionary<string, Color32>();

        /// <summary>#rgb, #rrggbb, #rrggbbaa.</summary>
        public static Color32 C(string hex)
        {
            if (cache.TryGetValue(hex, out var c)) return c;
            string h = hex.StartsWith("#") ? hex.Substring(1) : hex;
            if (h.Length == 3) h = "" + h[0] + h[0] + h[1] + h[1] + h[2] + h[2];
            byte r = System.Convert.ToByte(h.Substring(0, 2), 16), g = System.Convert.ToByte(h.Substring(2, 2), 16), b = System.Convert.ToByte(h.Substring(4, 2), 16);
            byte a = h.Length >= 8 ? System.Convert.ToByte(h.Substring(6, 2), 16) : (byte)255;
            c = new Color32(r, g, b, a);
            cache[hex] = c;
            return c;
        }

        public static Color32 C(string hex, double alpha)
        {
            var c = C(hex);
            double a = alpha * c.a / 255.0;
            return new Color32(c.r, c.g, c.b, (byte)Mathf.Clamp((float)(a * 255.0 + 0.5), 0, 255));
        }

        public static Color Unity(string hex) { var c = C(hex); return new Color32(c.r, c.g, c.b, c.a); }
    }
}
