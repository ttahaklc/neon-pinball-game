// Dünya (normalize) koordinatlarından ekran pikseline dönüşüm (TS layout.ts). Masa 1 × 1.8; ekrana tek ölçek + öteleme ile oturur.
using System;
using UnityEngine;

namespace NeonPinball
{
    public struct Insets { public double top, bottom, left, right; }

    public class TableLayout
    {
        public double width, height, scale, ox, oy, hudTop, sideLeft, sideRight;
        public bool landscape;
        public Insets insets;

        const double VisibleW = 0.97, VisibleH = 1.74;

        public static (double row1, double row2, double gap) HudRows(double k) => (Math.Round(54 * k), Math.Round(40 * k), Math.Round(4 * k));

        public static TableLayout Compute(double width, double height, Insets insets, double k = 1)
        {
            bool land = width > height;
            var rows = HudRows(k);
            double hudTop = land ? insets.top : insets.top + rows.row1 + rows.row2 + rows.gap + 6;
            // Alt boşluk: home göstergesi (güvenli alan) + ek pay. Masa ekranın en altına yapışmaz; kenardan başlayan
            // sistem hareketleri (ana ekrana kaydırma) flipper/fırlatma dokunuşlarını kesip oyunu durdurmasın.
            double pad = Math.Max(0, insets.bottom) + (land ? 10 : 22) * k;
            double availH = Math.Max(50, height - hudTop - pad);
            double scale = Math.Min(width / VisibleW, availH / VisibleH);
            double tw = TableC.Width * scale;
            double ox = (width - tw) / 2;
            double oy = height - pad - VisibleH * scale;
            return new TableLayout
            {
                width = width, height = height, landscape = land, scale = scale, ox = ox, oy = oy, hudTop = hudTop,
                sideLeft = Math.Max(0, ox), sideRight = Math.Max(0, width - (ox + tw)), insets = insets
            };
        }

        public double ToWorldX(double px) => (px - ox) / scale;
        public double ToWorldY(double py) => (py - oy) / scale;

        /// <summary>Ekran bölgesinin tablo birimi karşılığı (arka cam için).</summary>
        public Region ScreenRegion() => new Region(-ox / scale, -oy / scale, width / scale, height / scale);
    }
}
