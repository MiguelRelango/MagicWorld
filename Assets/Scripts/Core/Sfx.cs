using System.Collections.Generic;
using UnityEngine;

namespace Hearthbound
{
    /// <summary>Efectos de sonido sintetizados por código (sin archivos de audio).</summary>
    public static class Sfx
    {
        const int Rate = 22050;
        static AudioSource src;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        public static float Volume = 0.6f;

        public static void Play(string id, float pitch = 1f, float vol = 1f)
        {
            if (src == null)
            {
                GameObject go = new GameObject("Sfx");
                Object.DontDestroyOnLoad(go);
                src = go.AddComponent<AudioSource>();
                src.spatialBlend = 0f;
            }
            AudioClip c;
            if (!clips.TryGetValue(id, out c))
            {
                c = Build(id);
                clips[id] = c;
            }
            src.pitch = pitch;
            src.PlayOneShot(c, Volume * vol);
        }

        static AudioClip Build(string id)
        {
            switch (id)
            {
                case "swing": return Tone(0.12f, 500f, 200f, 0.8f, 6f);
                case "hit": return Tone(0.16f, 180f, 70f, 0.5f, 7f);
                case "bomb": return Tone(0.8f, 90f, 30f, 0.9f, 3.5f);
                case "pickup": return Tone(0.22f, 600f, 1000f, 0.0f, 4f);
                case "bow": return Tone(0.12f, 320f, 140f, 0.1f, 8f);
                case "shield": return Tone(0.14f, 260f, 200f, 0.4f, 8f);
                case "parry": return Tone(0.3f, 1200f, 1100f, 0.05f, 5f);
                case "hurt": return Tone(0.25f, 220f, 90f, 0.3f, 5f);
                case "ui": return Tone(0.06f, 700f, 800f, 0f, 10f);
                case "door": return Tone(0.5f, 110f, 70f, 0.6f, 3f);
                case "heal": return Tone(0.4f, 500f, 900f, 0f, 3f);
                case "jump": return Tone(0.16f, 260f, 520f, 0.05f, 6f);
                case "flash": return Tone(0.7f, 380f, 1500f, 0.25f, 3f);
                case "fire": return Tone(0.25f, 260f, 120f, 0.7f, 5f);
                case "wand": return Tone(0.3f, 500f, 900f, 0.2f, 5f);
                case "burn": return Tone(0.5f, 180f, 90f, 0.9f, 3.5f);
            }
            return Tone(0.1f, 440f, 440f, 0f, 6f);
        }

        static AudioClip Tone(float len, float f0, float f1, float noise, float decay)
        {
            int n = Mathf.Max(1, (int)(len * Rate));
            float[] d = new float[n];
            System.Random rng = new System.Random(1234);
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float f = Mathf.Lerp(f0, f1, t);
                phase += 2f * Mathf.PI * f / Rate;
                float tone = Mathf.Sin(phase);
                float nz = (float)(rng.NextDouble() * 2.0 - 1.0);
                float env = Mathf.Exp(-decay * t);
                d[i] = Mathf.Lerp(tone, nz, noise) * env * 0.6f;
            }
            AudioClip clip = AudioClip.Create("sfx", n, 1, Rate, false);
            clip.SetData(d, 0);
            return clip;
        }
    }
}
