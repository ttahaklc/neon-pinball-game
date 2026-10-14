// Tam ekran ekranlar: ana menü, bölüm seçimi, ayarlar, Sınırsız mod masa seçici (TS MenuScreens).
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NeonPinball
{
    public delegate void ScreenDispatch(AppAction a, int levelId = 0);

    public static class Screens
    {
        public const string PrivacyUrl = "https://ttahaklc.github.io/neon-pinball/privacy.html";
        static TextAnchor Start => Ui.Rtl ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        static TextAnchor StartUp => Ui.Rtl ? TextAnchor.UpperRight : TextAnchor.UpperLeft;

        /// <summary>Tam ekran, opak zemin (altındaki masa/menü görünmesin).</summary>
        static VisualElement Page()
        {
            var p = new VisualElement { name = "page", pickingMode = PickingMode.Position };
            Ui.Fill(p);
            p.style.backgroundColor = Pal.Ink;
            return p;
        }

        static VisualElement ImageBox(RenderTexture rt, float w, float h)
        {
            var v = new VisualElement { pickingMode = PickingMode.Ignore };
            v.style.width = w; v.style.height = h; v.style.flexShrink = 0;
            if (rt != null) v.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rt));
            return v;
        }

        // ---------------------------------------------------------------- Ana menü

        /// <summary>Üstteki masa (kamera) bu bloğun üstündeki alana çizilir; blok altta durur.</summary>
        public static float MenuBottomBlockHeight() => Ui.S(UiEnv.Landscape ? 0 : 300) + Mathf.Max(UiEnv.InBottom, 16) + Ui.S(8);

        public static VisualElement MainMenu(ScreenDispatch dispatch, Action openEndless)
        {
            var page = new VisualElement { name = "menu", pickingMode = PickingMode.Ignore };
            Ui.Fill(page);
            int next = SaveStore.NextPlayable();
            int gems = SaveStore.TotalGems();
            int bestEndless = Mathf.Max(SaveStore.Data.endlessClassic, SaveStore.Data.endlessModern);
            bool land = UiEnv.Landscape;

            var title = Ui.Box(); title.style.alignItems = land ? (Ui.Rtl ? Align.FlexEnd : Align.FlexStart) : Align.Center;
            var tAnchor = land ? Start : TextAnchor.MiddleCenter;
            var name = Ui.OneLine(I18n.T("appName"), Ui.S(land ? 40 : 46), Pal.Bone, Face.Display, true, tAnchor, Ui.S(24));
            name.style.alignSelf = Align.Stretch; title.Add(name);
            var tag = Ui.OneLine(I18n.T("tagline"), Ui.S(14), Pal.Muted, Face.Body, false, tAnchor, Ui.S(10)); tag.style.marginTop = Ui.S(4); tag.style.alignSelf = Align.Stretch; title.Add(tag);
            var gemRow = Ui.Row(); gemRow.style.marginTop = Ui.S(12);
            gemRow.Add(Ui.Diamond(Ui.S(16), true));
            var gl = Ui.OneLine(gems + " / " + Game.TotalLevels * 3, Ui.S(15), Pal.BrassHi); gl.style.minHeight = Ui.S(24); gl.style.flexShrink = 0; gl.style.marginLeft = Ui.S(6); gl.style.marginRight = Ui.S(6); gemRow.Add(gl);
            title.Add(gemRow);

            var buttons = Ui.Box(); buttons.style.width = Length.Percent(100); buttons.style.maxWidth = Ui.S(360);
            buttons.Add(Ui.Button(I18n.T("play"), () => dispatch(AppAction.SelectLevel, next), Ui.Btn.Primary, I18n.T("levelN", ("n", next)) + " · " + I18n.LevelName(next)));
            Ui.Gap(buttons, Ui.S(12));
            buttons.Add(Ui.Button(I18n.T("endless"), openEndless, Ui.Btn.Secondary, bestEndless > 0 ? I18n.T("record") + " " + I18n.FormatNumber(bestEndless) : I18n.T("endlessSub")));
            Ui.Gap(buttons, Ui.S(12));
            var r = Ui.Row(); r.style.alignItems = Align.Stretch;
            var b1 = Ui.Button(I18n.T("levels"), () => dispatch(AppAction.OpenLevels)); b1.style.flexGrow = 1; b1.style.flexBasis = 0;
            var b2 = Ui.Button(I18n.T("settings"), () => dispatch(AppAction.OpenSettings), Ui.Btn.Secondary); b2.style.flexGrow = 1; b2.style.flexBasis = 0; b2.style.marginLeft = Ui.S(12);
            r.Add(b1); r.Add(b2); buttons.Add(r);

            if (land)
            {
                // sağ yarı: başlık + düğmeler; sol yarı: masa (kamera)
                var side = Ui.Box(); side.style.position = Position.Absolute; side.style.right = UiEnv.InRight + UiEnv.Pad; side.style.left = UiEnv.W / 2;
                side.style.top = 0; side.style.bottom = 0; side.style.justifyContent = Justify.Center;
                side.Add(title); Ui.Gap(side, Ui.S(24)); side.Add(buttons);
                page.Add(side);
            }
            else
            {
                float bottomPad = Mathf.Max(UiEnv.InBottom, 16) + Ui.S(8);
                var fade = Ui.VGradient(Pal.A(Pal.Ink, 0), Pal.Ink); fade.style.position = Position.Absolute; fade.style.left = 0; fade.style.right = 0;
                fade.style.bottom = Ui.S(300) + bottomPad - 0; fade.style.height = Ui.S(70);
                page.Add(fade);
                var block = Ui.Box(); block.style.position = Position.Absolute; block.style.left = 0; block.style.right = 0; block.style.bottom = 0;
                block.style.height = Ui.S(300) + bottomPad; block.style.backgroundColor = Pal.Ink; block.style.alignItems = Align.Center;
                Ui.Pad(block, Ui.S(8), UiEnv.Pad + UiEnv.InRight, bottomPad, UiEnv.Pad + UiEnv.InLeft);
                block.pickingMode = PickingMode.Position;
                block.Add(title); Ui.Gap(block, Ui.S(20)); block.Add(buttons);
                page.Add(block);
            }
            return page;
        }

        // ---------------------------------------------------------------- Sınırsız mod masa seçici

        public static VisualElement EndlessPicker(Action<string> onPick, Action onClose)
        {
            var scrim = Overlays.Body(out var content);
            var sheet = Ui.Sheet(); sheet.style.alignItems = Align.Center;
            sheet.Add(Ui.Text(I18n.T("endless"), Ui.S(26), Pal.Bone, Face.Display, true, TextAnchor.MiddleCenter));
            Ui.Gap(sheet, Ui.S(8));
            sheet.Add(Ui.Text(I18n.T("endlessSub"), Ui.S(14), Pal.Muted, Face.Body, false, TextAnchor.MiddleCenter));
            Ui.Gap(sheet, Ui.S(14));
            var hl = Ui.Hairline(); hl.style.width = Ui.S(80); hl.style.alignSelf = Align.Center; sheet.Add(hl);
            Ui.Gap(sheet, Ui.S(14));
            sheet.Add(Ui.Text(I18n.T("tableStyle"), Ui.S(13), Pal.BrassHi, Face.Body, false, TextAnchor.MiddleCenter));
            Ui.Gap(sheet, Ui.S(14));
            float gap = Ui.S(12);
            float cardW = Mathf.Clamp(Mathf.Floor((UiEnv.ContentW - Ui.S(44) - gap) / 2), 120, Ui.S(190));
            var row = Ui.Row(); row.style.alignItems = Align.Stretch;
            string[] variants = { "classic", "modern" };
            for (int i = 0; i < 2; i++)
            {
                string v = variants[i]; int best = SaveStore.EndlessBest(v);
                var card = new VisualElement(); card.style.width = cardW; Ui.Radius(card, Ui.S(16)); 
                Ui.Border(card, 1.5f, Pal.HairlineHi); card.style.backgroundColor = Pal.SurfaceHi; card.style.flexShrink = 0;
                if (i > 0) card.style.marginLeft = gap;
                var img = ImageBox(Thumbs.For(v), cardW - 3, Mathf.Round(cardW * 1.25f)); img.style.borderTopLeftRadius = Ui.S(15); img.style.borderTopRightRadius = Ui.S(15); card.Add(img);
                var info = Ui.Box(); Ui.Pad(info, Ui.S(12), Ui.S(12), Ui.S(12), Ui.S(12));
                info.Add(Ui.OneLine(I18n.T(v), Ui.S(20), Pal.Bone, Face.Display, true, Start));
                info.Add(Ui.Text(I18n.T(v + "Desc"), Ui.S(12), Pal.Muted, Face.Body, false, StartUp));
                var rec = Ui.OneLine(best > 0 ? I18n.T("record") + " " + I18n.FormatNumber(best) : I18n.T("noRecord"), Ui.S(13), best > 0 ? Pal.BrassHi : Pal.Faint, Face.Body, false, Start);
                rec.style.marginTop = Ui.S(4); info.Add(rec);
                card.Add(info);
                Ui.Pressable(card, () => onPick(v));
                row.Add(card);
            }
            sheet.Add(row);
            Ui.Gap(sheet, Ui.S(14));
            sheet.Add(Ui.Button(I18n.T("cancel"), onClose, Ui.Btn.Quiet));
            content.Add(sheet);
            return scrim;
        }

        // ---------------------------------------------------------------- Başlık çubuğu

        static VisualElement Header(string title, Action onBack)
        {
            var h = Ui.Row();
            Ui.Pad(h, Mathf.Max(UiEnv.InTop, 10), UiEnv.Pad + UiEnv.InRight, Ui.S(8), UiEnv.Pad + UiEnv.InLeft);
            h.style.flexShrink = 0;
            var back = new VisualElement(); back.style.width = Ui.S(48); back.style.height = Ui.S(48); back.style.justifyContent = Justify.Center;
            back.style.alignItems = Ui.Rtl ? Align.FlexEnd : Align.FlexStart; back.style.flexShrink = 0;
            back.Add(Ui.Chevron(Ui.S(22), Pal.Bone, Ui.Rtl));
            Ui.Pressable(back, onBack);
            h.Add(back);
            var t = Ui.OneLine(title, Ui.S(26), Pal.Bone, Face.Display, true, TextAnchor.MiddleCenter, Ui.S(16)); t.style.flexGrow = 1; t.style.flexShrink = 1; t.style.minWidth = 0;
            h.Add(t);
            var sp = new VisualElement { pickingMode = PickingMode.Ignore }; sp.style.width = Ui.S(48); sp.style.flexShrink = 0; h.Add(sp);
            return h;
        }

        static ScrollView Scroller()
        {
            var sv = new ScrollView(ScrollViewMode.Vertical);
            sv.verticalScrollerVisibility = ScrollerVisibility.Hidden; sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            sv.touchScrollBehavior = ScrollView.TouchScrollBehavior.Elastic;
            sv.style.flexGrow = 1;
            sv.contentContainer.style.alignItems = Align.Center;
            return sv;
        }

        // ---------------------------------------------------------------- Bölüm seçimi

        public static VisualElement LevelSelect(ScreenDispatch dispatch)
        {
            var page = Page(); page.style.flexDirection = FlexDirection.Column;
            page.Add(Header(I18n.T("levels"), () => dispatch(AppAction.Back)));
            var sv = Scroller();
            Ui.Pad(sv.contentContainer, 0, UiEnv.Pad + UiEnv.InRight, Mathf.Max(UiEnv.InBottom, 16) + Ui.S(20), UiEnv.Pad + UiEnv.InLeft);
            page.Add(sv);

            int next = SaveStore.NextPlayable();
            int focusWorld = (Mathf.Min(next, Game.TotalLevels) - 1) / Game.LevelsPerWorld;
            float gap = Ui.S(10);
            float inner = Mathf.Min(UiEnv.W - UiEnv.InLeft - UiEnv.InRight - UiEnv.Pad * 2, 640);
            int cols = UiEnv.Landscape ? 10 : 5;
            float tile = Mathf.Floor((inner - gap * (cols - 1)) / cols);
            VisualElement focusEl = null;

            var list = Ui.Box(); list.style.width = inner;
            for (int w = 0; w < Game.TotalLevels / Game.LevelsPerWorld; w++)
            {
                int first = w * Game.LevelsPerWorld + 1;
                var th = LevelDb.Theme(LevelDb.Get(first).themeId);
                int wg = 0; for (int i = 0; i < Game.LevelsPerWorld; i++) wg += SaveStore.Stars(first + i);
                bool open = SaveStore.IsUnlocked(first);
                var sec = Ui.Box(); sec.style.marginTop = Ui.S(w == 0 ? 8 : 26); sec.style.opacity = open ? 1f : 0.55f;
                var head = Ui.Row(); head.style.marginBottom = Ui.S(10);
                head.Add(ImageBox(Thumbs.World[w], Ui.S(30), Ui.S(30)));
                var nm = Ui.OneLine(I18n.WorldName(w), Ui.S(22), Pal.Bone, Face.Display, true, Start, Ui.S(14)); nm.style.flexGrow = 1; nm.style.flexShrink = 1; nm.style.minWidth = 0;
                nm.style.marginLeft = Ui.S(10); nm.style.marginRight = Ui.S(10);
                head.Add(nm); head.Add(Ui.OneLine(wg + "/30", Ui.S(14), Pal.Muted));
                sec.Add(head);
                var line = new VisualElement { pickingMode = PickingMode.Ignore }; line.style.height = 2; line.style.backgroundColor = Pal.A(Pal.H(th.primary), 0.5f); line.style.marginBottom = Ui.S(12);
                sec.Add(line);
                var grid = Ui.Box(Ui.Rtl ? FlexDirection.RowReverse : FlexDirection.Row); grid.style.flexWrap = Wrap.Wrap;
                for (int i = 0; i < Game.LevelsPerWorld; i++)
                {
                    int id = first + i;
                    var t = Tile(id, tile, id == next, dispatch);
                    t.style.marginRight = Ui.Rtl ? 0 : ((i % cols) == cols - 1 ? 0 : gap); t.style.marginLeft = Ui.Rtl ? ((i % cols) == cols - 1 ? 0 : gap) : 0;
                    t.style.marginBottom = gap;
                    grid.Add(t);
                }
                sec.Add(grid);
                list.Add(sec);
                if (w == focusWorld) focusEl = sec;
            }
            sv.Add(list);
            if (focusEl != null && focusWorld > 0)
            {
                var fe = focusEl;
                sv.RegisterCallback<GeometryChangedEvent>(e => { sv.scrollOffset = new Vector2(0, Mathf.Max(0, fe.layout.y - Ui.S(8))); });
            }
            return page;
        }

        static VisualElement Tile(int id, float size, bool isNext, ScreenDispatch dispatch)
        {
            bool unlocked = SaveStore.IsUnlocked(id);
            var p = SaveStore.Progress(id);
            bool done = p != null && p.completed;
            var t = new VisualElement(); t.style.width = size; t.style.height = Mathf.Round(size * 1.12f); Ui.Radius(t, Ui.S(12));
            t.style.alignItems = Align.Center; t.style.justifyContent = Justify.Center; t.style.flexShrink = 0;
            t.style.backgroundColor = done ? Pal.SurfaceHi : unlocked ? Pal.Surface : new Color(0, 0, 0, 0);
            Ui.Border(t, isNext ? 3f : 1.5f, isNext ? Pal.Brass : unlocked ? Pal.HairlineHi : Pal.Hairline);
            if (unlocked)
            {
                t.Add(Ui.OneLine(id.ToString(), Mathf.Round(size * 0.36f), isNext ? Pal.BrassHi : Pal.Bone, Face.Body, false, TextAnchor.MiddleCenter));
                var g = Ui.Gems(p?.stars ?? 0, Mathf.Max(8, Mathf.Round(size * 0.17f))); g.style.marginTop = Ui.S(4); t.Add(g);
                Ui.PressableTap(t, () => dispatch(AppAction.SelectLevel, id));
            }
            else t.Add(Ui.Lock(Mathf.Round(size * 0.34f)));
            return t;
        }

        // ---------------------------------------------------------------- Ayarlar

        public static VisualElement Settings(ScreenDispatch dispatch, Action rebuild)
        {
            var page = Page(); page.style.flexDirection = FlexDirection.Column;
            page.Add(Header(I18n.T("settings"), () => dispatch(AppAction.Back)));
            var sv = Scroller();
            Ui.Pad(sv.contentContainer, 0, UiEnv.Pad + UiEnv.InRight, Mathf.Max(UiEnv.InBottom, 16) + Ui.S(24), UiEnv.Pad + UiEnv.InLeft);
            page.Add(sv);
            var st = SaveStore.Data.settings;

            var sh = Ui.Sheet(); sh.style.paddingTop = Ui.S(8); sh.style.paddingBottom = Ui.S(8); sh.style.marginBottom = Ui.S(16);
            var how = Ui.Row(); how.style.minHeight = Ui.S(56); how.pickingMode = PickingMode.Position;
            var hl1 = Ui.OneLine(I18n.T("tutorialTitle"), Ui.S(17), Pal.Bone, Face.Body, false, Start); hl1.style.flexGrow = 1; hl1.style.flexShrink = 1; hl1.style.minWidth = 0;
            how.Add(hl1); how.Add(Ui.Chevron(Ui.S(16), Pal.Muted, !Ui.Rtl));
            VisualElement howPage = null;
            Ui.Pressable(how, () => { howPage = HowToPlay(() => howPage.RemoveFromHierarchy()); page.Add(howPage); });
            sh.Add(how);
            sv.Add(sh);

            var s1 = Ui.Sheet(); s1.style.paddingTop = Ui.S(8); s1.style.paddingBottom = Ui.S(8); s1.style.marginBottom = Ui.S(16);
            s1.Add(LanguagePicker(rebuild));
            sv.Add(s1);

            var s2 = Ui.Sheet(); s2.style.paddingTop = Ui.S(8); s2.style.paddingBottom = Ui.S(8);
            s2.Add(Ui.Switch(I18n.T("music"), st.music, v => { SaveStore.SetMusic(v); AudioService.SetMusic(v); }));
            s2.Add(Ui.Hairline());
            s2.Add(Ui.Switch(I18n.T("sfx"), st.sfx, v => { SaveStore.SetSfx(v); AudioService.SetSfx(v); }));
            s2.Add(Ui.Hairline());
            s2.Add(Ui.Switch(I18n.T("haptics"), st.haptics, v => { SaveStore.SetHaptics(v); Haptics.Enabled = v; }));
            s2.Add(Ui.Hairline());
            s2.Add(Ui.Switch(I18n.T("debug"), st.debug, v => SaveStore.SetDebug(v), I18n.T("debugHint") + ". " + I18n.T("unlockAll") + "."));
            sv.Add(s2);

            var s3 = Ui.Sheet(); s3.style.marginTop = Ui.S(16);
            s3.Add(Ui.Text(I18n.T("levels") + ": " + SaveStore.CompletedCount() + "/" + Game.TotalLevels + " · " + I18n.T("gems") + ": " + SaveStore.TotalGems() + "/" + Game.TotalLevels * 3, Ui.S(14), Pal.Muted, Face.Body, false, StartUp));
            s3.Add(Ui.Text(I18n.T("best") + ": " + I18n.FormatNumber(SaveStore.BestOverall()), Ui.S(13), Pal.Faint, Face.Body, false, StartUp));
            Ui.Gap(s3, Ui.S(10));
            s3.Add(Ui.Button(I18n.T("resetProgress"), () => ShowReset(page, rebuild), Ui.Btn.Danger));
            sv.Add(s3);

            var s4 = Ui.Sheet(); s4.style.marginTop = Ui.S(16); s4.style.paddingTop = Ui.S(8); s4.style.paddingBottom = Ui.S(8);
            var pr = Ui.Row(); pr.style.minHeight = Ui.S(56); pr.pickingMode = PickingMode.Position;
            var pl = Ui.OneLine(I18n.T("privacyPolicy"), Ui.S(17), Pal.Bone, Face.Body, false, Start); pl.style.flexGrow = 1; pl.style.flexShrink = 1; pl.style.minWidth = 0;
            pr.Add(pl); pr.Add(Ui.Chevron(Ui.S(16), Pal.Muted, !Ui.Rtl));
            Ui.Pressable(pr, () => Application.OpenURL(PrivacyUrl));
            s4.Add(pr);
            s4.Add(Ui.Hairline());
            var vr = Ui.Row(); vr.style.minHeight = Ui.S(48);
            var vl = Ui.OneLine(I18n.T("version"), Ui.S(15), Pal.Muted, Face.Body, false, Start); vl.style.flexGrow = 1; vl.style.flexShrink = 1; vl.style.minWidth = 0;
            vr.Add(vl);
            var vv = Ui.OneLine(Application.version, Ui.S(15), Pal.Faint, Face.Body, false, Ui.Rtl ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight); vv.style.flexShrink = 0;
            vr.Add(vv);
            s4.Add(vr);
            sv.Add(s4);
            return page;
        }

        // ---------------------------------------------------------------- Nasıl oynanır

        static readonly string[][] HowSections =
        {
            new[] { "howLaunchT", "howLaunch" }, new[] { "howFlippersT", "howFlippers" }, new[] { "howComboT", "howCombo" },
            new[] { "howMultT", "howMult" }, new[] { "howSpecialT", "howSpecial" }, new[] { "howSaveT", "howSave" },
            new[] { "howGemsT", "howGems" }, new[] { "howEndlessT", "howEndless" },
        };

        public static VisualElement HowToPlay(Action onBack)
        {
            var page = Page(); page.style.flexDirection = FlexDirection.Column;
            page.Add(Header(I18n.T("tutorialTitle"), onBack));
            var sv = Scroller();
            Ui.Pad(sv.contentContainer, 0, UiEnv.Pad + UiEnv.InRight, Mathf.Max(UiEnv.InBottom, 16) + Ui.S(24), UiEnv.Pad + UiEnv.InLeft);
            page.Add(sv);
            for (int i = 0; i < HowSections.Length; i++)
            {
                var card = Ui.Sheet(); card.style.marginBottom = Ui.S(12);
                var head = Ui.Row(Ui.S(10), Align.Center);
                var num = Ui.OneLine((i + 1).ToString(), Ui.S(14), Pal.OnBrass, Face.Body, true, TextAnchor.MiddleCenter);
                var badge = new VisualElement { pickingMode = PickingMode.Ignore }; badge.style.width = Ui.S(26); badge.style.height = Ui.S(26); badge.style.flexShrink = 0;
                Ui.Radius(badge, Ui.S(13)); badge.style.backgroundColor = Pal.Brass; badge.style.alignItems = Align.Center; badge.style.justifyContent = Justify.Center;
                badge.Add(num); head.Add(badge);
                var t = Ui.Text(I18n.T(HowSections[i][0]), Ui.S(19), Pal.Bone, Face.Display, true, Start); t.style.flexShrink = 1; t.style.flexGrow = 1; t.style.marginLeft = Ui.S(10); t.style.marginRight = Ui.S(10);
                head.Add(t);
                card.Add(head);
                Ui.Gap(card, Ui.S(10));
                card.Add(Ui.Text(I18n.T(HowSections[i][1]), Ui.S(15), Pal.Muted, Face.Body, false, StartUp));
                sv.Add(card);
            }
            return page;
        }

        static void ShowReset(VisualElement page, Action rebuild)
        {
            var scrim = Overlays.Body(out var content);
            var sheet = Ui.Sheet();
            sheet.Add(Ui.Text(I18n.T("resetTitle"), Ui.S(24), Pal.Bone, Face.Display, true, Start));
            Ui.Gap(sheet, Ui.S(14));
            sheet.Add(Ui.Text(I18n.T("resetBody"), Ui.S(15), Pal.Muted, Face.Body, false, StartUp));
            Ui.Gap(sheet, Ui.S(14));
            sheet.Add(Ui.Button(I18n.T("confirm"), () => { SaveStore.ResetProgress(); rebuild(); }, Ui.Btn.Danger));
            Ui.Gap(sheet, Ui.S(10));
            sheet.Add(Ui.Button(I18n.T("cancel"), () => scrim.RemoveFromHierarchy(), Ui.Btn.Quiet));
            content.Add(sheet);
            page.Add(scrim);
        }

        static VisualElement LanguagePicker(Action rebuild)
        {
            string chosen = SaveStore.Data.settings.language;
            var info = I18n.Info();
            bool auto = chosen == "auto";
            var wrap = Ui.Box();
            var list = Ui.Box(); list.style.display = DisplayStyle.None;

            var head = Ui.Row(); head.style.minHeight = Ui.S(56); head.pickingMode = PickingMode.Position;
            var l1 = Ui.OneLine(I18n.T("language"), Ui.S(17), Pal.Bone, Face.Body, false, Start); l1.style.flexGrow = 1; l1.style.flexShrink = 1; l1.style.minWidth = 0;
            head.Add(l1);
            var cur = Ui.OneLine(auto ? I18n.T("languageAuto") + " · " + info.native : info.native, Ui.S(16), Pal.BrassHi, Face.Body, false, Ui.Rtl ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
            cur.style.flexShrink = 1; cur.style.marginLeft = Ui.S(12); cur.style.marginRight = Ui.S(8);
            head.Add(cur);
            var chev = Ui.ChevronDown(Ui.S(14), Pal.Muted); head.Add(chev);
            bool open = false;
            Ui.Pressable(head, () => { open = !open; list.style.display = open ? DisplayStyle.Flex : DisplayStyle.None; chev.parent.Remove(chev); chev = Ui.ChevronDown(Ui.S(14), Pal.Muted, open); head.Add(chev); });
            wrap.Add(head);

            Action<string, string, bool> addOpt = (label, code, selected) =>
            {
                list.Add(Ui.Hairline());
                var r = Ui.Row(); r.style.minHeight = Ui.S(50); r.pickingMode = PickingMode.Position;
                var t = Ui.OneLine(label, Ui.S(16), selected ? Pal.BrassHi : Pal.Bone, Face.Body, false, Start); t.style.flexGrow = 1; t.style.flexShrink = 1; t.style.minWidth = 0;
                r.Add(t);
                if (selected) r.Add(Ui.Diamond(Ui.S(12), true));
                Ui.Pressable(r, () => { SaveStore.SetLanguage(code); I18n.SetLanguage(SaveStore.EffectiveLanguage()); rebuild(); });
                list.Add(r);
            };
            addOpt(I18n.T("languageAuto"), "auto", auto);
            foreach (var l in I18n.Languages) addOpt(l.native, l.code, !auto && l.code == I18n.Current);
            wrap.Add(list);
            return wrap;
        }
    }
}
