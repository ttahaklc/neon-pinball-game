// Bindirme ekranları: bölüm tanıtımı, öğretici, duraklat, bölüm sonucu (kutlama), oyun sonu (TS GameOverlays).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NeonPinball
{
    public static class Overlays
    {
        static TextAnchor Start => Ui.Rtl ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        static TextAnchor StartUp => Ui.Rtl ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
        static readonly Color Brass = Pal.Brass;

        /// <summary>Tam ekran saydam-koyu zemin + ortalanmış, taşarsa kaydırılan gövde. Dokunuşları altındaki oyuna geçirmez.</summary>
        public static VisualElement Body(out VisualElement content)
        {
            var scrim = new VisualElement { name = "scrim", pickingMode = PickingMode.Position };
            Ui.Fill(scrim); scrim.style.backgroundColor = Pal.Scrim;
            var sv = new ScrollView(ScrollViewMode.Vertical);
            sv.verticalScrollerVisibility = ScrollerVisibility.Hidden; sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            sv.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
            Ui.Fill(sv);
            var cc = sv.contentContainer;
            cc.style.flexGrow = 1; cc.style.alignItems = Align.Center; cc.style.justifyContent = Justify.Center;
            Ui.Pad(cc, Mathf.Max(UiEnv.InTop, 16), UiEnv.Pad + UiEnv.InRight, Mathf.Max(UiEnv.InBottom, 16), UiEnv.Pad + UiEnv.InLeft);
            scrim.Add(sv);
            content = cc;
            return scrim;
        }

        // ---------------------------------------------------------------- Tanıtım

        public static VisualElement Intro(LevelConfig level)
        {
            var scrim = Body(out var c);
            var col = Ui.Box(); col.style.width = Length.Percent(100); col.style.maxWidth = Ui.S(420); col.style.alignItems = Align.Center;
            col.Add(Ui.OneLine(level.endless ? I18n.T("endless") : I18n.T("levelN", ("n", level.id)), Ui.S(14), Pal.Muted, Face.Body, false, TextAnchor.MiddleCenter));
            Ui.Gap(col, Ui.S(6));
            col.Add(Ui.Text(level.endless ? I18n.T(level.themeId) : I18n.LevelName(level.id), Ui.S(34), Pal.Bone, Face.Display, true, TextAnchor.MiddleCenter));
            var hl = Ui.Hairline(); hl.style.width = Ui.S(80); hl.style.marginTop = Ui.S(16); hl.style.marginBottom = Ui.S(16); col.Add(hl);
            if (level.endless) { col.Add(Ui.Text(I18n.T("endlessSub"), Ui.S(16), Pal.Bone, Face.Body, false, TextAnchor.MiddleCenter)); }
            foreach (var o in level.objectives)
            {
                var r = Ui.Row(); r.style.justifyContent = Justify.Center; r.style.marginTop = Ui.S(8);
                r.Add(Ui.Diamond(Ui.S(12), true));
                var t = Ui.Text(Strings.Goal(o.kind, o.goal), Ui.S(16), Pal.Bone, Face.Body, false, Start); t.style.flexShrink = 1; t.style.marginLeft = Ui.S(10); t.style.marginRight = Ui.S(10);
                r.Add(t); col.Add(r);
            }
            if (!level.endless)
            {
                var g = Ui.Text(I18n.T("gems") + ": " + Strings.Bonus(level), Ui.S(13), Pal.Muted, Face.Body, false, TextAnchor.MiddleCenter); g.style.marginTop = Ui.S(14); col.Add(g);
            }
            c.Add(col);
            return scrim;
        }

        // ---------------------------------------------------------------- Öğretici

        public static VisualElement Tutorial(Action onClose)
        {
            var scrim = Body(out var c);
            var sheet = Ui.Sheet();
            string[] steps = { "tutorial1", "tutorial2", "tutorial3", "tutorial4", "tutorial5" };
            int i = 0;
            sheet.Add(Ui.Text(I18n.T("tutorialTitle"), Ui.S(26), Pal.Bone, Face.Display, true, Start));
            Ui.Gap(sheet, Ui.S(16));
            var bars = Ui.Row(); var barEls = new List<VisualElement>();
            for (int n = 0; n < steps.Length; n++)
            {
                var b = new VisualElement { pickingMode = PickingMode.Ignore }; b.style.height = 3; b.style.flexGrow = 1; b.style.flexBasis = 0; Ui.Radius(b, 2);
                if (n > 0) b.style.marginLeft = Ui.S(6);
                barEls.Add(b); bars.Add(b);
            }
            sheet.Add(bars);
            Ui.Gap(sheet, Ui.S(16));
            var body = Ui.Text("", Ui.S(17), Pal.Bone, Face.Body, false, StartUp); body.style.minHeight = Ui.S(100);
            sheet.Add(body);
            Ui.Gap(sheet, Ui.S(16));
            var slot = Ui.Box(); sheet.Add(slot);
            Action refresh = null;
            refresh = () =>
            {
                body.text = I18n.T(steps[i]);
                for (int n = 0; n < barEls.Count; n++) barEls[n].style.backgroundColor = n <= i ? Pal.Brass : Pal.Hairline;
                slot.Clear();
                bool last = i == steps.Length - 1;
                slot.Add(Ui.Button(last ? I18n.T("tutorialStart") : I18n.T("tutorialNext"), () => { if (last) onClose(); else { i++; refresh(); } }, Ui.Btn.Primary));
                if (!last) { Ui.Gap(slot, Ui.S(10)); slot.Add(Ui.Button(I18n.T("tutorialSkip"), onClose, Ui.Btn.Quiet)); }
            };
            refresh();
            c.Add(sheet);
            return scrim;
        }

        // ---------------------------------------------------------------- Duraklat

        public static VisualElement Pause(bool endless, Action resume, Action restart, Action settings, Action levels, Action menu)
        {
            var scrim = Body(out var c);
            var sheet = Ui.Sheet(Ui.S(380));
            var t = Ui.Text(I18n.T("pause"), Ui.S(28), Pal.Bone, Face.Display, true, TextAnchor.MiddleCenter); t.style.marginBottom = Ui.S(12); sheet.Add(t);
            sheet.Add(Ui.Button(I18n.T("resume"), resume, Ui.Btn.Primary)); Ui.Gap(sheet, Ui.S(10));
            sheet.Add(Ui.Button(I18n.T("restart"), restart)); Ui.Gap(sheet, Ui.S(10));
            sheet.Add(Ui.Button(I18n.T("settings"), settings)); Ui.Gap(sheet, Ui.S(10));
            if (!endless) { sheet.Add(Ui.Button(I18n.T("levels"), levels)); Ui.Gap(sheet, Ui.S(10)); }
            sheet.Add(Ui.Button(I18n.T("menu"), menu, Ui.Btn.Quiet));
            c.Add(sheet);
            return scrim;
        }

        // ---------------------------------------------------------------- Sonuç satırları

        static VisualElement Row2(string k, string v, bool strong = false)
        {
            var r = Ui.Row(Ui.S(12), Align.Center); r.style.minHeight = Ui.S(30); r.style.justifyContent = Justify.SpaceBetween;
            var a = Ui.Text(k, Ui.S(14), Pal.Muted, Face.Body, false, Start); a.style.flexShrink = 1; r.Add(a);
            var b = Ui.OneLine(v, Ui.S(14), strong ? Pal.BrassHi : Pal.Bone, Face.Body, true, TextAnchor.MiddleRight); b.style.flexShrink = 0; b.style.marginLeft = Ui.S(12); b.style.marginRight = Ui.S(12);
            r.Add(b);
            return r;
        }

        static VisualElement StarRule(bool ok, string label, string value)
        {
            var r = Ui.Row(); r.style.minHeight = Ui.S(30);
            r.Add(Ui.Diamond(Ui.S(12), ok));
            var l = Ui.Text(label, Ui.S(14), ok ? Pal.Bone : Pal.Muted, Face.Body, false, Start); l.style.flexGrow = 1; l.style.flexShrink = 1; l.style.marginLeft = Ui.S(10); l.style.marginRight = Ui.S(10);
            r.Add(l);
            var v = Ui.OneLine(value, Ui.S(13), ok ? Pal.Green : Pal.Muted, Face.Body, true, TextAnchor.MiddleRight); v.style.flexShrink = 0;
            r.Add(v);
            return r;
        }

        // ---------------------------------------------------------------- Bölüm sonucu (kutlama)

        public static VisualElement LevelComplete(UiRoot ui, LevelConfig level, LevelResult r, int best, bool newBest, Action next, Action retry, Action levels, Action menu)
        {
            var scrim = Body(out var c);
            bool last = r.levelId >= Game.TotalLevels;
            var bonus = level.stars.bonus; double two = level.stars.twoStarScore;
            bool ok2 = r.score >= two;
            bool ok3 = Objectives.BonusAchieved(bonus, r.ballsLeft, r.maxCombo, r.score);
            int shownCombo = Mathf.Min(5, r.maxCombo);
            string bonusValue = bonus.kind == "ballsLeft" ? r.ballsLeft + " / " + bonus.value
                : bonus.kind == "combo" ? shownCombo + "× / " + bonus.value + "×"
                : I18n.FormatNumber(r.score) + " / " + I18n.FormatNumber(bonus.value);
            string title = I18n.T(r.stars == 3 ? "celebrate3" : r.stars == 2 ? "celebrate2" : "celebrate1");

            AddConfetti(ui, scrim, r.stars == 3 ? 28 : r.stars == 2 ? 18 : 10);

            var sheet = Ui.Sheet(); sheet.style.alignItems = Align.Stretch;
            var tl = Ui.OneLine(title, Ui.S(r.stars == 3 ? 32 : 28), r.stars == 3 ? Pal.BrassHi : Pal.Bone, Face.Display, true, TextAnchor.MiddleCenter, Ui.S(18));
            sheet.Add(tl);
            Pop(ui, tl, 0f, 0.45f, 0.6f);
            var gemRow = Ui.Box(FlexDirection.Row); gemRow.style.justifyContent = Justify.Center; gemRow.style.marginTop = Ui.S(12); gemRow.style.marginBottom = Ui.S(12);
            for (int i = 0; i < 3; i++)
            {
                var d = Ui.Diamond(Ui.S(40), i < r.stars); if (i > 0) d.style.marginLeft = Ui.S(6);
                d.style.opacity = 0; d.style.scale = new Scale(new Vector2(0.2f, 0.2f));
                Pop(ui, d, 0.35f + i * 0.26f, 0.5f, 0.2f);
                gemRow.Add(d);
            }
            sheet.Add(gemRow);
            var score = Ui.OneLine("0", Ui.S(44), Pal.Bone, Face.Body, false, TextAnchor.MiddleCenter, Ui.S(24));
            sheet.Add(score);
            float t0 = 0; int target = r.score;
            ui.AddOverlayTicker(dt => { t0 += dt; float k = Mathf.Min(1f, t0 / 1.1f); score.text = I18n.FormatNumber(Mathf.Round(target * (1 - Mathf.Pow(1 - k, 3)))); return k < 1f; });
            if (newBest) sheet.Add(Ui.Text(I18n.T("newBest"), Ui.S(14), Pal.BrassHi, Face.Body, true, TextAnchor.MiddleCenter));
            Ui.Gap(sheet, Ui.S(8)); sheet.Add(Ui.Hairline()); Ui.Gap(sheet, Ui.S(8));
            sheet.Add(Ui.Text(I18n.T("starsTitle"), Ui.S(12), Pal.Muted, Face.Body, false, Start));
            sheet.Add(StarRule(true, I18n.T("star1Rule"), "✓"));
            sheet.Add(StarRule(ok2, I18n.T("star2Rule", ("goal", I18n.FormatNumber(two))), I18n.FormatNumber(r.score) + " / " + I18n.FormatNumber(two)));
            sheet.Add(StarRule(ok3, Strings.Bonus(level), bonusValue));
            if (r.stars == 3) { var p = Ui.Text(I18n.T("starPerfect"), Ui.S(13), Pal.BrassHi, Face.Body, false, TextAnchor.MiddleCenter); p.style.marginTop = Ui.S(4); sheet.Add(p); }
            else
            {
                if (!ok2) { var h = Ui.Text(I18n.T("starHint2", ("n", I18n.FormatNumber(two - r.score))), Ui.S(13), Pal.Bone, Face.Body, false, StartUp); h.style.marginTop = Ui.S(4); sheet.Add(h); }
                if (!ok3) { var h = Ui.Text(I18n.T("starHint3", ("goal", Strings.Bonus(level))), Ui.S(13), Pal.Bone, Face.Body, false, StartUp); h.style.marginTop = Ui.S(2); sheet.Add(h); }
            }
            Ui.Gap(sheet, Ui.S(8)); sheet.Add(Ui.Hairline()); Ui.Gap(sheet, Ui.S(8));
            sheet.Add(Row2(I18n.T("completionBonus"), I18n.FormatNumber(r.bonus - r.ballsLeft * ScoreC.BallBonus)));
            sheet.Add(Row2(I18n.T("spareBalls") + " (" + r.ballsLeft + ") · " + I18n.T("ballBonus"), I18n.FormatNumber(r.ballsLeft * ScoreC.BallBonus)));
            sheet.Add(Row2(I18n.T("maxCombo"), shownCombo + "×"));
            sheet.Add(Row2(level.endless ? I18n.T("record") : I18n.T("best"), I18n.FormatNumber(best)));
            Ui.Gap(sheet, Ui.S(14));
            if (last) { var d = Ui.Text(I18n.T("levelsDone"), Ui.S(14), Pal.BrassHi, Face.Body, false, TextAnchor.MiddleCenter); sheet.Add(d); }
            else sheet.Add(Ui.Button(I18n.T("next"), next, Ui.Btn.Primary));
            Ui.Gap(sheet, Ui.S(10)); sheet.Add(Ui.Button(I18n.T("retry"), retry));
            if (!level.endless) { Ui.Gap(sheet, Ui.S(10)); sheet.Add(Ui.Button(I18n.T("levels"), levels, Ui.Btn.Quiet)); }
            Ui.Gap(sheet, Ui.S(10)); sheet.Add(Ui.Button(I18n.T("menu"), menu, Ui.Btn.Quiet));
            c.Add(sheet);
            return scrim;
        }

        /// <summary>Gecikmeli "pop": ölçek 0 → 1 (hafif aşım), saydamlık 0 → 1.</summary>
        static void Pop(UiRoot ui, VisualElement el, float delay, float dur, float from)
        {
            float t = -delay;
            el.style.opacity = 0;
            ui.AddOverlayTicker(dt =>
            {
                t += dt;
                if (t < 0) return true;
                float k = Mathf.Clamp01(t / dur);
                float c1 = 1.70158f, c3 = c1 + 1, e = 1 + c3 * Mathf.Pow(k - 1, 3) + c1 * Mathf.Pow(k - 1, 2);
                float s = Mathf.LerpUnclamped(from, 1f, e);
                el.style.scale = new Scale(new Vector2(s, s));
                el.style.opacity = Mathf.Clamp01(k * 3f);
                return k < 1f;
            });
        }

        /// <summary>Kutlama konfetisi: düşen pirinç/beyaz elmaslar.</summary>
        static void AddConfetti(UiRoot ui, VisualElement parent, int count)
        {
            var layer = new VisualElement { pickingMode = PickingMode.Ignore }; Ui.Fill(layer); parent.Add(layer);
            var parts = new List<(VisualElement el, float x, float size, float delay, float dur, int spin)>();
            Color[] cols = { Pal.BrassHi, Pal.Bone, Pal.Brass };
            for (int i = 0; i < count; i++)
            {
                float x = ((i * 0.6180339f) % 1f) * UiEnv.W, size = Ui.S(7 + (i * 7) % 7);
                var el = new VisualElement { pickingMode = PickingMode.Ignore };
                el.style.position = Position.Absolute; el.style.left = x; el.style.top = -20; el.style.width = size; el.style.height = size; el.style.backgroundColor = cols[i % 3];
                el.style.opacity = 0; layer.Add(el);
                parts.Add((el, x, size, (i % 9) * 0.14f, 2.2f + ((i * 331) % 1400) / 1000f, i % 2 == 0 ? 1 : -1));
            }
            float time = 0;
            ui.AddOverlayTicker(dt =>
            {
                time += dt;
                foreach (var p in parts)
                {
                    float cycle = p.dur; float tt = time - p.delay; if (tt < 0) continue;
                    if (tt > cycle * 2) { p.el.style.opacity = 0; continue; }
                    float k = (tt % cycle) / cycle;
                    float y = k * k * (UiEnv.H + 40) - 20;
                    float sway = p.spin * 18f * Mathf.Sin(k * Mathf.PI);
                    p.el.style.translate = new Translate(sway, y);
                    p.el.style.rotate = new Rotate(Angle.Degrees(45 + p.spin * 360 * k));
                    p.el.style.opacity = Mathf.Clamp01(Mathf.Min(k / 0.08f, (1 - k) / 0.15f));
                }
                return time < 9f;
            });
        }

        // ---------------------------------------------------------------- Oyun sonu

        public static VisualElement GameOver(LevelConfig level, GameOverReason reason, int score, int best, bool newBest, Action retry, Action levels, Action menu)
        {
            var scrim = Body(out var c);
            var sheet = Ui.Sheet(); sheet.style.alignItems = Align.Stretch;
            string head = level.endless ? I18n.T("endlessOver") : reason == GameOverReason.Time ? I18n.T("gameOverTime") : I18n.T("gameOver");
            sheet.Add(Ui.Text(head, Ui.S(28), level.endless ? Pal.BrassHi : Pal.Red, Face.Display, true, TextAnchor.MiddleCenter));
            var cap = Ui.Text(I18n.T("score"), Ui.S(13), Pal.Muted, Face.Body, false, TextAnchor.MiddleCenter); cap.style.marginTop = Ui.S(8); sheet.Add(cap);
            sheet.Add(Ui.OneLine(I18n.FormatNumber(score), Ui.S(44), Pal.Bone, Face.Body, false, TextAnchor.MiddleCenter, Ui.S(24)));
            if (newBest) sheet.Add(Ui.Text(I18n.T("newBest"), Ui.S(14), Pal.BrassHi, Face.Body, true, TextAnchor.MiddleCenter));
            Ui.Gap(sheet, Ui.S(10)); sheet.Add(Ui.Hairline()); Ui.Gap(sheet, Ui.S(10));
            sheet.Add(Row2(level.endless ? I18n.T("record") : I18n.T("best"), I18n.FormatNumber(best)));
            Ui.Gap(sheet, Ui.S(4));
            foreach (var o in level.objectives)
            {
                var r = Ui.Row(); r.style.marginTop = Ui.S(6);
                r.Add(Ui.Diamond(Ui.S(10), false));
                var t = Ui.Text(Strings.Goal(o.kind, o.goal), Ui.S(13), Pal.Muted, Face.Body, false, Start); t.style.flexShrink = 1; t.style.marginLeft = Ui.S(8); t.style.marginRight = Ui.S(8);
                r.Add(t); sheet.Add(r);
            }
            Ui.Gap(sheet, Ui.S(14));
            sheet.Add(Ui.Button(I18n.T("retry"), retry, Ui.Btn.Primary));
            if (!level.endless) { Ui.Gap(sheet, Ui.S(10)); sheet.Add(Ui.Button(I18n.T("levels"), levels, Ui.Btn.Quiet)); }
            Ui.Gap(sheet, Ui.S(10)); sheet.Add(Ui.Button(I18n.T("menu"), menu, Ui.Btn.Quiet));
            c.Add(sheet);
            return scrim;
        }
    }
}
