// Ses servisi: efektler küçük AudioSource havuzlarıyla (üst üste binebilir), müzik döngüsel tek kaynak.
// Ateşle-ve-unut: hiçbir çağrı bir öncekinin sonucuna bağlı değildir.
using System.Collections.Generic;
using UnityEngine;

namespace NeonPinball
{
    public enum Sfx { Flipper, Bumper, Target, Wall, Ramp, Spinner, Score, Combo, Special, ExtraBall, Drain, LevelComplete, GameOver, Click, Launch }

    public sealed class AudioService : MonoBehaviour
    {
        public static AudioService I { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() { I = null; }

        struct PoolCfg { public int size; public float gap; public float vol; }
        sealed class Pool { public AudioClip clip; public AudioSource[] src; public int next; public float last; public PoolCfg cfg; }

        readonly Dictionary<Sfx, Pool> pools = new Dictionary<Sfx, Pool>();
        AudioSource music;
        bool sfxOn = true, musicOn = true, musicWanted, suspended;
        const float MusicVolume = 0.32f;

        static PoolCfg Cfg(Sfx s)
        {
            switch (s)
            {
                case Sfx.Bumper: return new PoolCfg { size = 3, gap = 0.04f, vol = 0.85f };
                case Sfx.Flipper: return new PoolCfg { size = 3, gap = 0.03f, vol = 0.85f };
                case Sfx.Wall: return new PoolCfg { size = 2, gap = 0.07f, vol = 0.5f };
                case Sfx.Spinner: return new PoolCfg { size = 2, gap = 0.06f, vol = 0.85f };
                case Sfx.Target: return new PoolCfg { size = 2, gap = 0.04f, vol = 0.85f };
                case Sfx.Click: return new PoolCfg { size = 3, gap = 0, vol = 0.85f };
                case Sfx.Score: return new PoolCfg { size = 2, gap = 0, vol = 0.85f };
                case Sfx.Launch: return new PoolCfg { size = 2, gap = 0, vol = 0.85f };
                default: return new PoolCfg { size = 2, gap = 0, vol = 0.85f };
            }
        }

        public static AudioService Create(Transform parent)
        {
            var go = new GameObject("Audio");
            go.transform.SetParent(parent, false);
            var a = go.AddComponent<AudioService>();
            a.Init();
            return a;
        }

        void Init()
        {
            I = this;
            // Düşük gecikme: küçük DSP tamponu (dokunma → ses 50 ms altında kalsın)
            var conf = AudioSettings.GetConfiguration();
            if (conf.dspBufferSize > 512 || conf.dspBufferSize == 0) { conf.dspBufferSize = 256; AudioSettings.Reset(conf); }
            foreach (Sfx s in System.Enum.GetValues(typeof(Sfx)))
            {
                var clip = Resources.Load<AudioClip>("Audio/" + s.ToString().ToLowerInvariant());
                if (clip == null) continue;
                var cfg = Cfg(s);
                var p = new Pool { clip = clip, cfg = cfg, src = new AudioSource[cfg.size], last = -10 };
                for (int i = 0; i < cfg.size; i++)
                {
                    var src = gameObject.AddComponent<AudioSource>();
                    src.clip = clip; src.playOnAwake = false; src.spatialBlend = 0; src.loop = false;
                    p.src[i] = src;
                }
                pools[s] = p;
            }
            var mc = Resources.Load<AudioClip>("Audio/music");
            if (mc != null)
            {
                music = gameObject.AddComponent<AudioSource>();
                music.clip = mc; music.loop = true; music.volume = MusicVolume; music.playOnAwake = false; music.spatialBlend = 0;
            }
        }

        public static void Play(Sfx s, float volume = 1f) { if (I != null) I.PlayInternal(s, volume); }

        void PlayInternal(Sfx s, float volume)
        {
            if (!sfxOn || suspended) return;
            if (!pools.TryGetValue(s, out var p)) return;
            float now = Time.unscaledTime;
            if (now - p.last < p.cfg.gap) return;
            p.last = now;
            var src = p.src[p.next];
            p.next = (p.next + 1) % p.src.Length;
            src.volume = Mathf.Clamp01(volume) * p.cfg.vol;
            src.Stop();
            src.Play();
        }

        public static void SetSfx(bool on) { if (I != null) I.sfxOn = on; }
        public static void SetMusic(bool on) { if (I != null) { I.musicOn = on; I.ApplyMusic(); } }
        public static void StartMusic() { if (I != null) { I.musicWanted = true; I.ApplyMusic(); } }
        public static void Suspend(bool on) { if (I != null) { I.suspended = on; I.ApplyMusic(); } }

        void ApplyMusic()
        {
            if (music == null) return;
            if (musicWanted && musicOn && !suspended) { if (!music.isPlaying) music.Play(); }
            else if (music.isPlaying) music.Pause();
        }
    }
}
