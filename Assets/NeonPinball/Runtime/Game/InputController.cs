// Çok dokunuşlu giriş (TS GameScreen): ekranın sol yarısı sol flipper, sağ yarısı sağ flipper; sağ alt köşedeki launch lane = piston.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NeonPinball
{
    public sealed class InputController
    {
        enum Role { Left, Right, Plunger, Ignore }
        readonly Dictionary<int, Role> touches = new Dictionary<int, Role>();
        public System.Action<bool> OnFlipperPress;
        /// <summary>Dokunuş bir arayüz öğesinin (duraklat düğmesi vb.) üzerindeyse true → masa girişi sayılmaz.</summary>
        public System.Func<Vector2, bool> Blocked;   // sol/sağ basıldı (ses + titreşim için)

        public void Clear(GameEngine engine)
        {
            touches.Clear();
            if (engine != null) { engine.PlungerRelease(); engine.SetFlipper(true, false); engine.SetFlipper(false, false); }
        }

        void Down(int id, Vector2 pos, TableLayout L, GameEngine engine)
        {
            if (Blocked != null && Blocked(pos)) { touches[id] = Role.Ignore; return; }
            double px = pos.x, py = Scr.H - pos.y;
            double wx = L.ToWorldX(px), wy = L.ToWorldY(py);
            // Top yayda beklerken (Ready) sağ alt bölge geniş bir fırlatma alanıdır; oyun sırasında bu bölge normal sağ flipperdir.
            bool ready = engine.status == GameStatus.Ready;
            bool inLane = ready && wx >= 0.72 && wy >= 0.9;
            Role role = inLane ? Role.Plunger : (px < L.width / 2 ? Role.Left : Role.Right);
            touches[id] = role;
            if (role == Role.Plunger) engine.PlungerPress();
        }

        List<int> gone = new List<int>();
        HashSet<int> seen = new HashSet<int>();

        public void Poll(GameEngine engine, TableLayout L, bool run)
        {
            if (!run) { if (touches.Count > 0) Clear(engine); return; }
            seen.Clear();
            var ts = Touchscreen.current;
            if (ts != null)
            {
                foreach (var t in ts.touches)
                {
                    if (!t.press.isPressed) continue;
                    int id = t.touchId.ReadValue();
                    seen.Add(id);
                    if (!touches.ContainsKey(id)) Down(id, t.position.ReadValue(), L, engine);
                }
            }
            var m = Mouse.current;
            if (m != null && m.leftButton.isPressed)
            {
                seen.Add(-1);
                if (!touches.ContainsKey(-1)) Down(-1, m.position.ReadValue(), L, engine);
            }
            // Klavye (editörde test): Z/X flipper, Space piston
            var kb = Keyboard.current;
            bool kl = false, kr = false;
            if (kb != null)
            {
                kl = kb.zKey.isPressed || kb.leftArrowKey.isPressed; kr = kb.xKey.isPressed || kb.rightArrowKey.isPressed;
                if (kb.spaceKey.wasPressedThisFrame) engine.PlungerPress();
                if (kb.spaceKey.wasReleasedThisFrame) engine.PlungerRelease();
            }
            gone.Clear();
            foreach (var kv in touches) if (!seen.Contains(kv.Key)) gone.Add(kv.Key);
            bool plungerUp = false;
            foreach (int id in gone) { if (touches[id] == Role.Plunger) plungerUp = true; touches.Remove(id); }
            if (plungerUp)
            {
                bool still = false;
                foreach (var r in touches.Values) if (r == Role.Plunger) still = true;
                if (!still) engine.PlungerRelease();
            }
            bool l = kl, r2 = kr;
            foreach (var r in touches.Values) { if (r == Role.Left) l = true; else if (r == Role.Right) r2 = true; }
            bool pl = engine.world.flippers[0].pressed, pr = engine.world.flippers[1].pressed;
            if ((l && !pl) || (r2 && !pr)) OnFlipperPress?.Invoke(l && !pl);
            engine.SetFlipper(true, l);
            engine.SetFlipper(false, r2);
        }
    }
}
