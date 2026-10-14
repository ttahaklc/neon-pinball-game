// Masa üstü katman: skor baloncukları, duyurular ("TOP KURTARILDI!"), fırlatma ipucu, hata ayıklama göstergesi.
// Metin UI Toolkit ile çizilir: Arapça/Hintçe şekillendirme ve CJK yedek fontu doğru çalışır.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NeonPinball
{
    public sealed class FxView
    {
        public VisualElement Root;
        TableLayout L;
        sealed class Pop { public Label el; public float life, x, y, scale; public bool alive; }
        readonly List<Pop> pops = new List<Pop>();
        Label banner; float bannerT = -1; Color bannerColor;
        VisualElement hint; Label debug;

        public FxView()
        {
            Root = new VisualElement { name = "fx", pickingMode = PickingMode.Ignore };
            Ui.Fill(Root);
            for (int i = 0; i < 8; i++)
            {
                var l = Ui.Text("", 16, Color.white, Face.Body, true, TextAnchor.MiddleCenter);
                l.style.position = Position.Absolute; l.style.display = DisplayStyle.None; l.style.width = 200; l.style.whiteSpace = WhiteSpace.NoWrap;
                Root.Add(l); pops.Add(new Pop { el = l });
            }
            banner = Ui.OneLine("", 20, Color.white, Face.Body, true, TextAnchor.MiddleCenter, 12);
            banner.style.position = Position.Absolute; banner.style.display = DisplayStyle.None;
            Root.Add(banner);
            hint = Ui.Box(); hint.style.position = Position.Absolute; hint.style.display = DisplayStyle.None; hint.style.alignItems = Align.Center;
            Root.Add(hint);
            debug = Ui.Text("", 11, Pal.H("#7fff9a"), Face.Body, false, TextAnchor.UpperLeft);
            debug.style.position = Position.Absolute; debug.style.backgroundColor = new Color(0, 0, 0, 0.6f); debug.style.display = DisplayStyle.None;
            Ui.Pad(debug, 6, 6, 6, 6); Ui.Radius(debug, 6);
            Root.Add(debug);
        }

        public void SetLayout(TableLayout l)
        {
            L = l;
            float sc = (float)(l.scale / UiEnv.D);
            float cx = (float)((l.ox + TableBuilder.CenterX * l.scale) / UiEnv.D), y = (float)((l.oy + 0.78 * l.scale) / UiEnv.D);
            float size = Mathf.Max(16, sc * 0.07f), maxW = Mathf.Min(UiEnv.W - 16, sc * 0.95f);
            banner.style.fontSize = size; banner.style.width = maxW; banner.style.left = cx - maxW / 2; banner.style.top = y - size * 1.1f;
            BuildHint(sc);
            debug.style.left = UiEnv.InLeft + 8; debug.style.top = UiEnv.InTop + 100;
        }

        void BuildHint(float sc)
        {
            hint.Clear();
            float w = Mathf.Max(0.78f * sc, 240);
            hint.style.width = w;
            hint.style.left = (float)(L.ox / UiEnv.D) + 0.46f * sc - w / 2;
            hint.style.top = (float)(L.oy / UiEnv.D) + 1.46f * sc;
            var a = Ui.OneLine(I18n.T("holdToLaunch"), Ui.S(13), Pal.BrassHi, Face.Body, true, TextAnchor.MiddleCenter, Ui.S(9)); a.style.alignSelf = Align.Stretch;
            var b = Ui.OneLine(I18n.T("tapToLaunch"), Ui.S(11), Pal.Muted, Face.Body, true, TextAnchor.MiddleCenter, Ui.S(8)); b.style.alignSelf = Align.Stretch;
            hint.Add(a); hint.Add(b);
        }

        public void ShowHint(bool on) { hint.style.display = on ? DisplayStyle.Flex : DisplayStyle.None; }

        public void Popup(double x, double y, string text, string hex, bool big)
        {
            if (L == null) return;
            Pop p = null;
            foreach (var q in pops) if (!q.alive) { p = q; break; }
            if (p == null) { p = pops[0]; foreach (var q in pops) if (q.life < p.life) p = q; }
            p.alive = true; p.life = 0.9f; p.x = (float)x; p.y = (float)y; p.scale = (float)L.scale;
            p.el.text = text;
            p.el.style.fontSize = Mathf.Max(12, p.scale / UiEnv.D * (big ? 0.05f : 0.036f));
            p.el.style.color = Pal.H(hex);
            p.el.style.display = DisplayStyle.Flex;
            Place(p);
        }

        void Place(Pop p)
        {
            float k = 1 - p.life / 0.9f;
            float px = (float)((L.ox + p.x * L.scale) / UiEnv.D), py = (float)((L.oy + (p.y - k * 0.08) * L.scale) / UiEnv.D);
            float h = p.el.resolvedStyle.fontSize > 0 ? p.el.resolvedStyle.fontSize : 16;
            p.el.style.left = px - 100; p.el.style.top = py - h * 0.75f;
            var c = p.el.style.color.value; c.a = Mathf.Min(1, p.life * 2.5f); p.el.style.color = c;
        }

        public void Banner(string text, string hex)
        {
            banner.text = text; bannerColor = Pal.H(hex); bannerT = 0; banner.style.display = DisplayStyle.Flex;
            banner.style.color = new Color(bannerColor.r, bannerColor.g, bannerColor.b, 0);
        }

        public void Tick(float dt)
        {
            foreach (var p in pops)
            {
                if (!p.alive) continue;
                p.life -= dt;
                if (p.life <= 0) { p.alive = false; p.el.style.display = DisplayStyle.None; continue; }
                Place(p);
            }
            if (bannerT >= 0)
            {
                bannerT += dt;
                const float total = 1.4f, fin = 0.12f, fout = 0.38f;
                float a = bannerT < fin ? bannerT / fin : bannerT > total - fout ? Mathf.Max(0, (total - bannerT) / fout) : 1f;
                banner.style.color = new Color(bannerColor.r, bannerColor.g, bannerColor.b, a);
                if (bannerT >= total) { bannerT = -1; banner.style.display = DisplayStyle.None; }
            }
        }

        public void SetDebug(string text)
        {
            if (text == null) { debug.style.display = DisplayStyle.None; return; }
            debug.style.display = DisplayStyle.Flex; debug.text = text;
        }

        public void Clear()
        {
            foreach (var p in pops) { p.alive = false; p.el.style.display = DisplayStyle.None; }
            bannerT = -1; banner.style.display = DisplayStyle.None; hint.style.display = DisplayStyle.None;
        }
    }
}
