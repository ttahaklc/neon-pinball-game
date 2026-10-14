// UIDocument sahibi: katmanlar (HUD, efekt, tam ekran, bindirme) ve kare başına çalışan animasyon kancaları.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NeonPinball
{
    public sealed class UiRoot : MonoBehaviour
    {
        public VisualElement Root, Hud, Fx, Screen, Overlay;
        PanelSettings ps;
        readonly List<Func<float, bool>> tickers = new List<Func<float, bool>>();
        readonly List<Func<float, bool>> overlayTickers = new List<Func<float, bool>>();
        public Camera uiCamera;

        public static UiRoot Create(Transform parent, RenderTexture target = null)
        {
            var go = new GameObject("UI");
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<UiRoot>();
            r.Init(target);
            return r;
        }

        void Init(RenderTexture target)
        {
            ps = Resources.Load<PanelSettings>("UI/NeonPanelSettings");
            if (ps == null) ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps = Instantiate(ps);
            ps.scaleMode = PanelScaleMode.ConstantPixelSize;
            ps.scale = 1f;
            ps.targetTexture = target;
            ps.clearColor = target != null; ps.colorClearValue = new Color(0, 0, 0, 0);
            ps.sortingOrder = 10;
            var doc = gameObject.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            Root = doc.rootVisualElement;
            Root.style.flexGrow = 1;
            Root.pickingMode = PickingMode.Ignore;
            Root.style.unityTextGenerator = TextGeneratorType.Advanced;
            Hud = Layer("hud", PickingMode.Ignore); Fx = Layer("fx", PickingMode.Ignore);
            Screen = Layer("screen", PickingMode.Ignore); Overlay = Layer("overlay", PickingMode.Ignore);
        }

        VisualElement Layer(string name, PickingMode pm)
        {
            var v = new VisualElement { name = name, pickingMode = pm };
            Ui.Fill(v);
            Root.Add(v);
            return v;
        }

        /// <summary>Ekran / yoğunluk değişince çağrılır: 1 dp = 1 USS birimi.</summary>
        public void ApplyScale()
        {
            ps.scale = UiEnv.D;
        }

        public void SetTarget(RenderTexture rt) { ps.targetTexture = rt; ps.clearColor = rt != null; ps.colorClearValue = new Color(0, 0, 0, 0); }

        public void AddTicker(Func<float, bool> f) { tickers.Add(f); }
        public void AddOverlayTicker(Func<float, bool> f) { overlayTickers.Add(f); }

        public void ClearOverlay() { Overlay.Clear(); overlayTickers.Clear(); }
        public void ClearScreen() { Screen.Clear(); }

        /// <summary>Bir fonksiyon false döndürünce kendini kaldırır.</summary>
        public void Tick(float dt)
        {
            for (int i = tickers.Count - 1; i >= 0; i--) if (!tickers[i](dt)) tickers.RemoveAt(i);
            for (int i = overlayTickers.Count - 1; i >= 0; i--) if (!overlayTickers[i](dt)) overlayTickers.RemoveAt(i);
        }
    }
}
