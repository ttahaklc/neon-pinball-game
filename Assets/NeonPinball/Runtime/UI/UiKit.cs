// Arayüz yapı taşları (UI Toolkit, kodla). Tasarım dili: "pirinç ve lake" — mat koyu yüzeyler, ince çizgiler, tek vurgu rengi (pirinç).
// 1 USS birimi = 1 dp (PanelSettings.scale = piksel yoğunluğu). S(n) = cihaza göre ölçeklenmiş dp.
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NeonPinball
{
    public static class Pal
    {
        public static readonly Color Ink = H("#0B0C10"), Surface = H("#14161C"), SurfaceHi = H("#1B1E26"), Hairline = H("#2B2E39"), HairlineHi = H("#3C404E");
        public static readonly Color Brass = H("#C8A25A"), BrassHi = H("#E9D3A1"), BrassLo = H("#7A6236"), OnBrass = H("#17130A");
        public static readonly Color Bone = H("#ECE8DF"), Muted = H("#8D8F9B"), Faint = H("#4E505C");
        public static readonly Color Green = H("#6FD6A0"), Red = H("#E8707A"), Cyan = H("#6FE3F5"), Scrim = new Color(7 / 255f, 8 / 255f, 11 / 255f, 0.92f);
        public static readonly Color Chip = new Color(11 / 255f, 12 / 255f, 16 / 255f, 0.7f);
        public static Color H(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
        public static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }

    public enum Face { Body, Display }

    public static class Ui
    {
        public static float K = 1f;
        public static float S(float n) => Mathf.Round(n * K);
        public static bool Rtl => I18n.Info().rtl;
        const float Hair = 1f;

        // ---------- Metin ----------

        public static Label Text(string text, float size, Color color, Face face = Face.Body, bool bold = false, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var l = new Label(text) { pickingMode = PickingMode.Ignore };
            StyleText(l, size, color, face, bold, anchor);
            return l;
        }

        public static void StyleText(Label l, float size, Color color, Face face, bool bold, TextAnchor anchor)
        {
            var st = l.style;
            st.fontSize = size; st.color = color;
            var fa = face == Face.Display ? Fonts.Display : Fonts.Body;
            if (fa != null) st.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromSDFFont(fa));
            st.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
            st.unityTextAlign = anchor;
            st.unityTextGenerator = TextGeneratorType.Advanced;
            st.whiteSpace = WhiteSpace.Normal;
            st.marginLeft = 0; st.marginRight = 0; st.marginTop = 0; st.marginBottom = 0;
            st.paddingLeft = 0; st.paddingRight = 0; st.paddingTop = 0; st.paddingBottom = 0;
        }

        public static Label OneLine(string text, float size, Color color, Face face = Face.Body, bool bold = false, TextAnchor anchor = TextAnchor.MiddleLeft, float minSize = 0)
        {
            var l = Text(text, size, color, face, bold, anchor);
            l.style.whiteSpace = WhiteSpace.NoWrap;
            l.style.overflow = Overflow.Hidden;
            l.style.textOverflow = TextOverflow.Ellipsis;
            if (minSize > 0) l.style.unityTextAutoSize = new StyleTextAutoSize(new TextAutoSize(TextAutoSizeMode.BestFit, new Length(minSize, LengthUnit.Pixel), new Length(size, LengthUnit.Pixel)));
            return l;
        }

        // ---------- Düzen ----------

        public static VisualElement Box(FlexDirection dir = FlexDirection.Column, float gap = 0)
        {
            var v = new VisualElement();
            v.style.flexDirection = dir;
            v.pickingMode = PickingMode.Ignore;
            return v;
        }

        public static VisualElement Row(float gap = 0, Align items = Align.Center)
        {
            var v = Box(Rtl ? FlexDirection.RowReverse : FlexDirection.Row);
            v.style.alignItems = items;
            return v;
        }

        public static void Gap(VisualElement parent, float px, bool vertical = true)
        {
            var g = new VisualElement { pickingMode = PickingMode.Ignore };
            if (vertical) g.style.height = px; else g.style.width = px;
            g.style.flexShrink = 0;
            parent.Add(g);
        }

        public static void Fill(VisualElement v)
        {
            v.style.position = Position.Absolute; v.style.left = 0; v.style.right = 0; v.style.top = 0; v.style.bottom = 0;
        }

        public static void Radius(VisualElement v, float r)
        {
            v.style.borderTopLeftRadius = r; v.style.borderTopRightRadius = r; v.style.borderBottomLeftRadius = r; v.style.borderBottomRightRadius = r;
        }

        public static void Border(VisualElement v, float w, Color c)
        {
            v.style.borderLeftWidth = w; v.style.borderRightWidth = w; v.style.borderTopWidth = w; v.style.borderBottomWidth = w;
            v.style.borderLeftColor = c; v.style.borderRightColor = c; v.style.borderTopColor = c; v.style.borderBottomColor = c;
        }

        public static void Pad(VisualElement v, float t, float r, float b, float l)
        {
            v.style.paddingTop = t; v.style.paddingRight = r; v.style.paddingBottom = b; v.style.paddingLeft = l;
        }

        public static VisualElement Hairline(Color? c = null)
        {
            var h = new VisualElement { pickingMode = PickingMode.Ignore };
            h.style.height = Hair; h.style.backgroundColor = c ?? Pal.Hairline; h.style.alignSelf = Align.Stretch; h.style.flexShrink = 0;
            return h;
        }

        public static VisualElement Sheet(float maxW = 0)
        {
            var v = new VisualElement();
            v.style.width = Length.Percent(100);
            v.style.maxWidth = maxW > 0 ? maxW : S(420);
            v.style.backgroundColor = Pal.Surface;
            Radius(v, S(22)); Border(v, Hair, Pal.HairlineHi); Pad(v, S(22), S(22), S(22), S(22));
            v.style.flexShrink = 0;
            return v;
        }

        // ---------- Düğme ----------

        public enum Btn { Primary, Secondary, Quiet, Danger }

        /// <summary>Ses + titreşim dokunma anında (bırakmayı beklemez), eylem bırakınca.</summary>
        public static VisualElement Button(string label, Action onClick, Btn kind = Btn.Secondary, string sub = null, bool disabled = false)
        {
            bool primary = kind == Btn.Primary, quiet = kind == Btn.Quiet;
            var b = new VisualElement();
            b.style.minHeight = quiet ? S(44) : S(54);
            Radius(b, S(14));
            Pad(b, S(4), S(18), S(4), S(18));
            b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center;
            b.style.backgroundColor = primary ? Pal.Brass : new Color(0, 0, 0, 0);
            if (!primary && !quiet) Border(b, Hair * 1.5f, kind == Btn.Danger ? Pal.A(Pal.Red, 0.6f) : Pal.HairlineHi);
            b.style.flexShrink = 0;
            var fg = primary ? Pal.OnBrass : kind == Btn.Danger ? Pal.Red : quiet ? Pal.Muted : Pal.Bone;
            b.Add(OneLine(label, S(17), fg, Face.Body, true, TextAnchor.MiddleCenter, S(11)));
            if (!string.IsNullOrEmpty(sub)) b.Add(OneLine(sub, S(12), primary ? H3216 : Pal.Muted, Face.Body, false, TextAnchor.MiddleCenter, S(9)));
            b.style.opacity = disabled ? 0.4f : 1f;
            if (disabled) { b.pickingMode = PickingMode.Ignore; return b; }
            Pressable(b, onClick);
            return b;
        }
        static readonly Color H3216 = Pal.H("#3D3216");

        /// <summary>Liste öğeleri için: kaydırma hareketi sırasında tetiklenmez; geri bildirim bırakınca.</summary>
        public static void PressableTap(VisualElement b, Action onClick) { Pressable(b, onClick, false); }

        public static void Pressable(VisualElement b, Action onClick, bool feedback = true)
        {
            bool down = false; Vector2 start = default;
            b.RegisterCallback<PointerDownEvent>(e =>
            {
                down = true; start = e.position; b.style.opacity = 0.8f; b.style.scale = new Scale(new Vector2(0.985f, 0.985f));
                if (feedback) { AudioService.Play(Sfx.Click); Haptics.Light(); }
            });
            Action release = () => { down = false; b.style.opacity = 1f; b.style.scale = new Scale(Vector2.one); };
            b.RegisterCallback<PointerUpEvent>(e =>
            {
                if (!down) return;
                bool moved = ((Vector2)e.position - start).magnitude > 12f;
                release();
                if (moved) return;
                if (!feedback) { AudioService.Play(Sfx.Click); Haptics.Light(); }
                onClick?.Invoke();
            });
            b.RegisterCallback<PointerLeaveEvent>(e => { if (down) release(); });
            b.RegisterCallback<PointerCancelEvent>(e => { if (down) release(); });
        }

        // ---------- Küçük çizimler (Painter2D) ----------

        public sealed class Shape : VisualElement
        {
            readonly Action<Painter2D, Rect> draw;
            public Shape(float w, float h, Action<Painter2D, Rect> draw)
            {
                this.draw = draw; style.width = w; style.height = h; style.flexShrink = 0; pickingMode = PickingMode.Ignore;
                generateVisualContent += ctx => this.draw(ctx.painter2D, contentRect);
            }
        }

        /// <summary>Art-deco elmas: yıldız yerine. Dolu = kazanıldı.</summary>
        public static VisualElement Diamond(float size, bool filled, Color? color = null)
        {
            var col = color ?? Pal.Brass;
            return new Shape(size, size, (p, r) =>
            {
                float d = r.width * 0.5f, cx = r.width / 2, cy = r.height / 2;
                p.BeginPath(); p.MoveTo(new Vector2(cx, cy - d)); p.LineTo(new Vector2(cx + d, cy)); p.LineTo(new Vector2(cx, cy + d)); p.LineTo(new Vector2(cx - d, cy)); p.ClosePath();
                if (filled) { p.fillColor = col; p.Fill(); }
                else { p.strokeColor = Pal.Faint; p.lineWidth = 1.5f; p.Stroke(); }
            });
        }

        public static VisualElement Gems(int count, float size, int total = 3)
        {
            var r = Box(Rtl ? FlexDirection.RowReverse : FlexDirection.Row);
            for (int i = 0; i < total; i++) { var d = Diamond(size, i < count); if (i > 0) d.style.marginLeft = size * 0.1f; r.Add(d); }
            return r;
        }

        public static VisualElement Chevron(float size, Color color, bool pointRight = false)
        {
            return new Shape(size, size, (p, r) =>
            {
                float w = r.width, h = r.height;
                p.BeginPath();
                if (pointRight) { p.MoveTo(new Vector2(w * 0.3f, h * 0.1f)); p.LineTo(new Vector2(w * 0.8f, h * 0.5f)); p.LineTo(new Vector2(w * 0.3f, h * 0.9f)); }
                else { p.MoveTo(new Vector2(w * 0.7f, h * 0.1f)); p.LineTo(new Vector2(w * 0.2f, h * 0.5f)); p.LineTo(new Vector2(w * 0.7f, h * 0.9f)); }
                p.strokeColor = color; p.lineWidth = 2.2f; p.lineJoin = LineJoin.Miter; p.lineCap = LineCap.Round; p.Stroke();
            });
        }

        public static VisualElement ChevronDown(float size, Color color, bool up = false)
        {
            return new Shape(size, size, (p, r) =>
            {
                float w = r.width, h = r.height;
                p.BeginPath();
                if (up) { p.MoveTo(new Vector2(w * 0.1f, h * 0.7f)); p.LineTo(new Vector2(w * 0.5f, h * 0.25f)); p.LineTo(new Vector2(w * 0.9f, h * 0.7f)); }
                else { p.MoveTo(new Vector2(w * 0.1f, h * 0.3f)); p.LineTo(new Vector2(w * 0.5f, h * 0.75f)); p.LineTo(new Vector2(w * 0.9f, h * 0.3f)); }
                p.strokeColor = color; p.lineWidth = 2.2f; p.lineCap = LineCap.Round; p.Stroke();
            });
        }

        public static VisualElement Lock(float size, Color? color = null)
        {
            var c = color ?? Pal.Faint;
            return new Shape(size, size, (p, r) =>
            {
                float w = r.width, h = r.height;
                p.BeginPath(); p.MoveTo(new Vector2(w * 0.3f, h * 0.48f)); p.LineTo(new Vector2(w * 0.3f, h * 0.32f));
                p.Arc(new Vector2(w * 0.5f, h * 0.32f), w * 0.2f, Angle.Degrees(180), Angle.Degrees(360));
                p.LineTo(new Vector2(w * 0.7f, h * 0.48f));
                p.strokeColor = c; p.lineWidth = 2f; p.Stroke();
                p.BeginPath(); float x0 = w * 0.2f, x1 = w * 0.8f, y0 = h * 0.46f, y1 = h * 0.86f, rr = w * 0.08f;
                p.MoveTo(new Vector2(x0 + rr, y0)); p.LineTo(new Vector2(x1 - rr, y0)); p.ArcTo(new Vector2(x1, y0), new Vector2(x1, y0 + rr), rr);
                p.LineTo(new Vector2(x1, y1 - rr)); p.ArcTo(new Vector2(x1, y1), new Vector2(x1 - rr, y1), rr);
                p.LineTo(new Vector2(x0 + rr, y1)); p.ArcTo(new Vector2(x0, y1), new Vector2(x0, y1 - rr), rr);
                p.LineTo(new Vector2(x0, y0 + rr)); p.ArcTo(new Vector2(x0, y0), new Vector2(x0 + rr, y0), rr); p.ClosePath();
                p.fillColor = c; p.Fill();
            });
        }

        public static VisualElement Dot(float size, Color fill, bool filled, Color? stroke = null)
        {
            var d = new VisualElement { pickingMode = PickingMode.Ignore };
            d.style.width = size; d.style.height = size; Radius(d, size / 2); d.style.flexShrink = 0;
            if (filled) d.style.backgroundColor = fill; else Border(d, 1.5f, stroke ?? Pal.Faint);
            return d;
        }

        /// <summary>Dikey degrade (üst → alt) — UI Toolkit'te arka plan degradesi yok.</summary>
        public static VisualElement VGradient(Color top, Color bottom)
        {
            var v = new VisualElement { pickingMode = PickingMode.Ignore };
            v.generateVisualContent += ctx =>
            {
                var r = v.contentRect; if (r.width < 1 || r.height < 1) return;
                var md = ctx.Allocate(4, 6);
                md.SetNextVertex(new Vertex { position = new Vector3(0, 0, Vertex.nearZ), tint = top });
                md.SetNextVertex(new Vertex { position = new Vector3(r.width, 0, Vertex.nearZ), tint = top });
                md.SetNextVertex(new Vertex { position = new Vector3(r.width, r.height, Vertex.nearZ), tint = bottom });
                md.SetNextVertex(new Vertex { position = new Vector3(0, r.height, Vertex.nearZ), tint = bottom });
                md.SetAllIndices(new ushort[] { 0, 1, 2, 0, 2, 3 });
            };
            return v;
        }

        // ---------- Anahtar ----------

        public static VisualElement Switch(string label, bool value, Action<bool> onChange, string hint = null)
        {
            var row = Row(); row.style.minHeight = S(56); row.pickingMode = PickingMode.Position;
            var col = Box(); col.style.flexGrow = 1; col.style.flexShrink = 1; col.style.minWidth = 0;
            col.Add(OneLine(label, S(17), Pal.Bone, Face.Body, false, Rtl ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft));
            if (!string.IsNullOrEmpty(hint)) col.Add(Text(hint, S(12), Pal.Muted, Face.Body, false, Rtl ? TextAnchor.UpperRight : TextAnchor.UpperLeft));
            row.Add(col);
            var track = new VisualElement { pickingMode = PickingMode.Ignore };
            track.style.width = S(52); track.style.height = S(30); Radius(track, S(15)); track.style.flexShrink = 0;
            track.style.marginLeft = S(12); track.style.marginRight = S(12);
            Action<bool> paint = v =>
            {
                track.style.backgroundColor = v ? Pal.Brass : Pal.SurfaceHi;
                Border(track, v ? 0 : 1.5f, Pal.HairlineHi);
                track.Clear();
                var knob = new VisualElement { pickingMode = PickingMode.Ignore };
                knob.style.width = S(24); knob.style.height = S(24); Radius(knob, S(12));
                knob.style.backgroundColor = v ? Pal.OnBrass : Pal.Muted;
                knob.style.position = Position.Absolute; knob.style.top = (S(30) - S(24)) / 2 - (v ? 0 : 1.5f);
                bool right = v != Rtl;
                if (right) knob.style.right = S(3) - (v ? 0 : 1.5f); else knob.style.left = S(3) - 1.5f;
                track.Add(knob);
            };
            paint(value);
            row.Add(track);
            bool cur = value;
            Pressable(row, () => { cur = !cur; paint(cur); onChange(cur); });
            return row;
        }
    }
}
