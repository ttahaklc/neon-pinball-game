// Oyun içi HUD (TS GameHud): skor, kalan toplar, hedef ilerlemesi, rozetler (top koruması, çarpan, süre, özel mod, kombo), duraklat.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NeonPinball
{
    public sealed class HudView
    {
        public VisualElement Root;
        readonly LevelConfig level;
        readonly Action onPause;
        readonly int totalBalls;
        Label scoreValue, objLabel, recordLabel;
        VisualElement pips, chips, barFill, landObjs;
        int lastKey = int.MinValue, lastBalls = -1, lastChipKey = int.MinValue;
        int record;

        public HudView(LevelConfig level, int record, Action onPause)
        {
            this.level = level; this.record = record; this.onPause = onPause; totalBalls = level.ballCount;
            Build();
        }

        public void Rebuild() { Root.Clear(); Build(); lastKey = int.MinValue; lastBalls = -1; lastChipKey = int.MinValue; }

        public void SetRecord(int r) { record = r; lastKey = int.MinValue; }

        // ---------- Parçalar ----------

        static VisualElement ChipEl(string text, string tone)
        {
            bool fill = tone == "brass";
            var c = new VisualElement { pickingMode = PickingMode.Ignore };
            c.style.height = Ui.S(24); Ui.Pad(c, 0, Ui.S(9), 0, Ui.S(9)); Ui.Radius(c, Ui.S(12));
            c.style.justifyContent = Justify.Center; c.style.flexShrink = 0;
            c.style.backgroundColor = fill ? Pal.Brass : Pal.Chip;
            if (!fill) Ui.Border(c, 1.5f, tone == "alert" ? Pal.Red : Pal.HairlineHi);
            c.Add(Ui.OneLine(text, Ui.S(12), fill ? Pal.OnBrass : tone == "alert" ? Pal.Red : Pal.Bone, Face.Body, true, TextAnchor.MiddleCenter));
            return c;
        }

        void BuildPips(int left)
        {
            pips.Clear();
            int n = Mathf.Max(totalBalls, left);
            for (int i = 0; i < n; i++)
            {
                var d = Ui.Dot(Ui.S(11), Pal.H("#DDE3F5"), i < left);
                if (i > 0) d.style.marginLeft = Ui.S(5);
                pips.Add(d);
            }
        }

        VisualElement PauseButton()
        {
            var b = new VisualElement();
            b.style.width = Ui.S(44); b.style.height = Ui.S(44); Ui.Radius(b, Ui.S(22)); b.style.flexShrink = 0;
            b.style.backgroundColor = Pal.Chip; Ui.Border(b, 1.5f, Pal.HairlineHi);
            b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center;
            var bars = Ui.Box(FlexDirection.Row); bars.style.alignItems = Align.Center;
            for (int i = 0; i < 2; i++)
            {
                var bar = new VisualElement { pickingMode = PickingMode.Ignore };
                bar.style.width = Ui.S(4); bar.style.height = Ui.S(15); Ui.Radius(bar, 1); bar.style.backgroundColor = Pal.Bone;
                if (i == 1) bar.style.marginLeft = Ui.S(4);
                bars.Add(bar);
            }
            b.Add(bars);
            Ui.Pressable(b, () => onPause?.Invoke());
            return b;
        }

        void Build()
        {
            if (Root == null) { Root = new VisualElement { name = "hud", pickingMode = PickingMode.Ignore }; Ui.Fill(Root); }
            pips = Ui.Row(); pips.style.flexShrink = 0;
            chips = Ui.Row(); chips.style.flexShrink = 0;
            var rows = TableLayout.HudRows(Ui.K);
            bool endless = level.endless;
            var anchorStart = Ui.Rtl ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;

            if (!UiEnv.Landscape)
            {
                var col = Ui.Box(); col.style.position = Position.Absolute;
                col.style.left = UiEnv.InLeft + UiEnv.Pad; col.style.right = UiEnv.InRight + UiEnv.Pad; col.style.top = UiEnv.InTop + Ui.S(6);
                var r1 = Ui.Row(); r1.style.height = (float)rows.row1;
                var sc = Ui.Box(); sc.style.flexGrow = 1; sc.style.flexShrink = 1; sc.style.minWidth = 0;
                sc.Add(Ui.OneLine(I18n.T("score"), Ui.S(11), Pal.Muted, Face.Body, false, anchorStart));
                scoreValue = Ui.OneLine("0", Ui.S(30), Pal.Bone, Face.Body, false, anchorStart, Ui.S(18));
                sc.Add(scoreValue);
                r1.Add(sc);
                var pipWrap = Ui.Box(); pipWrap.style.paddingLeft = Ui.S(10); pipWrap.style.paddingRight = Ui.S(10); pipWrap.Add(pips);
                r1.Add(pipWrap);
                r1.Add(PauseButton());
                col.Add(r1);
                var r2 = Ui.Row(); r2.style.height = (float)rows.row2; r2.style.marginTop = (float)rows.gap;
                var left = Ui.Box(); left.style.flexGrow = 1; left.style.flexShrink = 1; left.style.minWidth = 0;
                if (endless) { recordLabel = Ui.OneLine("", Ui.S(13), Pal.Bone, Face.Body, false, anchorStart, Ui.S(10)); left.Add(recordLabel); }
                else
                {
                    objLabel = Ui.OneLine("", Ui.S(13), Pal.Bone, Face.Body, false, anchorStart, Ui.S(10)); left.Add(objLabel);
                    var track = new VisualElement { pickingMode = PickingMode.Ignore };
                    track.style.height = 2; track.style.marginTop = Ui.S(5); track.style.backgroundColor = Pal.Hairline; Ui.Radius(track, 1);
                    barFill = new VisualElement { pickingMode = PickingMode.Ignore };
                    barFill.style.height = 2; barFill.style.backgroundColor = Pal.Brass; Ui.Radius(barFill, 1); barFill.style.width = Length.Percent(0);
                    if (Ui.Rtl) barFill.style.alignSelf = Align.FlexEnd;
                    track.Add(barFill); left.Add(track);
                }
                r2.Add(left);
                chips.style.marginLeft = Ui.S(10);
                r2.Add(chips);
                col.Add(r2);
                Root.Add(col);
            }
            else
            {
                float leftW = 150, rightW = 170;
                var l = Ui.Box(); l.style.position = Position.Absolute; l.style.left = UiEnv.InLeft + Ui.S(14); l.style.top = Mathf.Max(UiEnv.InTop, 12); l.style.width = leftW;
                l.Add(Ui.OneLine(endless ? I18n.T("endless") : I18n.T("levelN", ("n", level.id)), Ui.S(12), Pal.Muted, Face.Body, false, anchorStart));
                l.Add(Ui.Text(endless ? I18n.T(level.themeId) : I18n.LevelName(level.id), Ui.S(18), Pal.Bone, Face.Display, true, anchorStart));
                Ui.Gap(l, Ui.S(14));
                l.Add(Ui.OneLine(I18n.T("score"), Ui.S(12), Pal.Muted, Face.Body, false, anchorStart));
                scoreValue = Ui.OneLine("0", Ui.S(30), Pal.Bone, Face.Body, false, anchorStart, Ui.S(16)); l.Add(scoreValue);
                Ui.Gap(l, Ui.S(10)); l.Add(pips);
                Root.Add(l);
                var r = Ui.Box(); r.style.position = Position.Absolute; r.style.right = UiEnv.InRight + Ui.S(14); r.style.top = Mathf.Max(UiEnv.InTop, 12); r.style.width = rightW;
                r.style.alignItems = Align.FlexEnd;
                r.Add(PauseButton());
                Ui.Gap(r, Ui.S(12));
                landObjs = Ui.Box(); landObjs.style.alignSelf = Align.Stretch; r.Add(landObjs);
                Ui.Gap(r, Ui.S(12));
                chips.style.flexDirection = FlexDirection.Column; chips.style.alignItems = Align.FlexEnd; r.Add(chips);
                Root.Add(r);
            }
        }

        /// <summary>Görünen bir değer değişince günceller (10 Hz çağrılır).</summary>
        public void Refresh(GameEngine e)
        {
            var h = e.GetHud();
            var objs = level.endless ? null : e.GetObjectives();
            int key = h.score;
            key = key * 31 + h.ballsLeft; key = key * 31 + h.multiplier; key = key * 31 + h.chain;
            key = key * 31 + (h.specialActive ? Mathf.CeilToInt((float)h.specialRemaining) : -1);
            key = key * 31 + (h.hasTimer ? Mathf.CeilToInt((float)Math.Max(0, h.timeLeft)) : -1);
            key = key * 31 + Mathf.CeilToInt((float)h.ballSaveLeft);
            key = key * 31 + (int)h.status;
            if (objs != null) foreach (var o in objs) key = key * 31 + o.value * 2 + (o.done ? 1 : 0);
            key = key * 31 + record;
            if (key == lastKey) return;
            lastKey = key;

            scoreValue.text = I18n.FormatNumber(h.score);
            if (h.ballsLeft != lastBalls) { lastBalls = h.ballsLeft; BuildPips(h.ballsLeft); }

            // Rozetler (en fazla 3): top koruması, çarpan, süre, özel mod, kombo
            var list = new List<(string t, string tone)>(5);
            if (h.ballSaveLeft > 0) list.Add((I18n.T("saveChip") + " " + Mathf.CeilToInt((float)h.ballSaveLeft), "line"));
            if (h.multiplier > 1) list.Add((h.multiplier + "×", "brass"));
            if (h.hasTimer)
            {
                int sec = Mathf.Max(0, Mathf.CeilToInt((float)h.timeLeft));
                list.Add((sec / 60 + ":" + (sec % 60).ToString("00"), sec <= 20 ? "alert" : "line"));
            }
            if (h.specialActive) list.Add((I18n.T("special") + " " + Mathf.CeilToInt((float)h.specialRemaining), "brass"));
            if (h.chain >= 2) list.Add((I18n.T("combo") + " " + Mathf.Min(5, h.chain), "line"));
            if (list.Count > 3) list.RemoveRange(3, list.Count - 3);
            int ck = list.Count;
            foreach (var c in list) ck = ck * 17 + c.t.GetHashCode();
            if (ck != lastChipKey)
            {
                lastChipKey = ck;
                chips.Clear();
                bool col = UiEnv.Landscape;
                for (int i = 0; i < list.Count; i++)
                {
                    var el = ChipEl(list[i].t, list[i].tone);
                    if (i > 0) { if (col) el.style.marginTop = Ui.S(6); else el.style.marginLeft = Ui.S(6); }
                    chips.Add(el);
                }
            }

            string rec = I18n.T("record") + " " + I18n.FormatNumber(Math.Max(record, h.score));
            bool beaten = h.score > record && record > 0;
            if (level.endless)
            {
                if (recordLabel != null) { recordLabel.text = rec; recordLabel.style.color = beaten ? Pal.BrassHi : Pal.Bone; }
                if (landObjs != null) { landObjs.Clear(); var t = Ui.Text(rec, Ui.S(13), beaten ? Pal.BrassHi : Pal.Bone, Face.Body, false, TextAnchor.MiddleRight); landObjs.Add(t); }
                return;
            }
            if (!UiEnv.Landscape)
            {
                ObjectiveProgress first = objs[0]; int open = 0; bool got = false;
                foreach (var o in objs) if (!o.done) { open++; if (!got) { first = o; got = true; } }
                objLabel.text = Strings.Objective(first.kind, first.value, first.goal) + (open > 1 ? "  +" + (open - 1) : "");
                objLabel.style.color = first.done ? Pal.Green : Pal.Bone;
                float pct = Mathf.Clamp01(first.value / (float)Mathf.Max(1, first.goal));
                barFill.style.width = Length.Percent(pct * 100);
            }
            else
            {
                landObjs.Clear();
                foreach (var o in objs)
                    landObjs.Add(Ui.Text(Strings.Objective(o.kind, o.value, o.goal), Ui.S(13), o.done ? Pal.Green : Pal.Bone, Face.Body, false, TextAnchor.MiddleRight));
            }
        }
    }

    public static class Strings
    {
        public static string Objective(string kind, int value, int goal) => I18n.T("objective." + kind, ("value", I18n.FormatNumber(value)), ("goal", I18n.FormatNumber(goal)));
        public static string Goal(string kind, int goal) => I18n.T("objectiveGoal." + kind, ("goal", I18n.FormatNumber(goal)));
        public static string Bonus(LevelConfig lv) => I18n.T("bonusObj." + lv.stars.bonus.kind, ("value", I18n.FormatNumber(lv.stars.bonus.value)));
    }
}
