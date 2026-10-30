using System.Collections.Generic;
using UnityEngine;

namespace Hearthbound
{
    /// <summary>Cualquier cosa que el fuego pueda prender.</summary>
    public interface IFlammable
    {
        bool Burning { get; }
        void Ignite();
    }

    /// <summary>
    /// Mata de enredaderas: bloquea el paso (collider sólido) hasta que se quema.
    /// Al prenderse arde ~2 s, contagia a las enredaderas cercanas y desaparece dejando cenizas.
    /// </summary>
    public class Vine : MonoBehaviour, IFlammable
    {
        public static readonly List<Vine> All = new List<Vine>();

        const float BurnTime = 2.2f;
        const float SpreadDelay = 0.5f;
        const float SpreadRange = 3.0f;

        static float lastBurnSfx = -1f;
        /// <summary>Se llama cuando la mata se consume del todo (para recordarlo en el mundo persistente).</summary>
        public System.Action OnBurnt;

        public bool Burning { get { return burning; } }
        public Vector3 Center { get { return transform.position + Vector3.up * 0.8f; } }

        bool burning, spread;
        float t;
        Renderer[] renders;
        Transform[] flames;
        float[] phase;
        Light glow;
        static readonly Color Green = new Color(0.22f, 0.50f, 0.20f);
        static readonly Color Leaf = new Color(0.30f, 0.65f, 0.26f);
        static readonly Color Char = new Color(0.14f, 0.11f, 0.10f);

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public static Vine Create(Transform parent, Vector3 pos, float yaw, float scale = 1f)
        {
            GameObject go = new GameObject("Vine");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            System.Random rng = new System.Random(Mathf.RoundToInt(pos.x * 31f) ^ Mathf.RoundToInt(pos.z * 17f));
            // Tallos que se cruzan y hojas.
            for (int i = 0; i < 7; i++)
            {
                float x = ((float)rng.NextDouble() - 0.5f) * 1.3f * scale;
                float h = (0.9f + (float)rng.NextDouble() * 0.9f) * scale;
                GameObject stem = Prim.Cyl(go.transform, new Vector3(x, h * 0.5f, ((float)rng.NextDouble() - 0.5f) * 0.4f),
                    new Vector3(0.09f * scale, h * 0.5f, 0.09f * scale), Green, false, "Stem");
                stem.transform.localRotation = Quaternion.Euler(((float)rng.NextDouble() - 0.5f) * 35f, 0f, ((float)rng.NextDouble() - 0.5f) * 40f);
            }
            for (int i = 0; i < 12; i++)
            {
                Vector3 lp = new Vector3(((float)rng.NextDouble() - 0.5f) * 1.5f, 0.2f + (float)rng.NextDouble() * 1.4f, ((float)rng.NextDouble() - 0.5f) * 0.6f) * scale;
                float ls = (0.22f + (float)rng.NextDouble() * 0.16f) * scale;
                Prim.Ball(go.transform, lp, new Vector3(ls * 1.3f, ls * 0.6f, ls), Leaf, false, "Leaf");
            }

            BoxCollider col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.8f * scale, 0f);
            col.size = new Vector3(1.7f * scale, 1.6f * scale, 0.9f * scale);

            Vine v = go.AddComponent<Vine>();
            v.renders = go.GetComponentsInChildren<Renderer>();
            return v;
        }

        public void Ignite()
        {
            if (burning) return;
            burning = true;
            t = 0f;
            if (Time.time - lastBurnSfx > 0.2f) { lastBurnSfx = Time.time; Sfx.Play("burn", 0.9f + Random.value * 0.2f, 0.7f); }

            int n = 5;
            flames = new Transform[n];
            phase = new float[n];
            for (int i = 0; i < n; i++)
            {
                Color c = i % 2 == 0 ? new Color(1f, 0.45f, 0.1f) : new Color(1f, 0.80f, 0.25f);
                GameObject f = Prim.Make(PrimitiveType.Sphere, transform,
                    new Vector3(((float)i / (n - 1) - 0.5f) * 1.4f, 0.5f + (i % 3) * 0.3f, ((i * 37) % 5 - 2) * 0.1f),
                    Vector3.one * 0.5f, c, false, "Flame", true);
                flames[i] = f.transform;
                phase[i] = Random.value * 6.28f;
            }

            GameObject lg = new GameObject("FireLight");
            lg.transform.SetParent(transform, false);
            lg.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            glow = lg.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.6f, 0.25f);
            glow.range = 7f;
            glow.intensity = 2.2f;
            glow.shadows = LightShadows.None;
        }

        void Update()
        {
            if (!burning) return;
            if (Game.State == GameState.Paused) return;
            t += Time.deltaTime;

            // Se ennegrece poco a poco.
            if (t > 0.35f && renders != null)
            {
                Material m = Prim.Mat(Char);
                for (int i = 0; i < renders.Length; i++)
                {
                    if (renders[i] == null) continue;
                    string n = renders[i].name;
                    if (n == "Stem" || n == "Leaf") renders[i].sharedMaterial = m;
                }
                renders = null;
            }

            // Llamas que parpadean y se apagan al final.
            float life = Mathf.Clamp01(1f - Mathf.Max(0f, t - (BurnTime - 0.6f)) / 0.6f);
            for (int i = 0; i < flames.Length; i++)
            {
                if (flames[i] == null) continue;
                float f = 0.55f + 0.25f * Mathf.Sin(Time.time * 14f + phase[i]) + 0.1f * Mathf.Sin(Time.time * 23f + phase[i] * 2f);
                float grow = Mathf.Clamp01(t / 0.3f);
                flames[i].localScale = new Vector3(0.45f, 0.9f, 0.45f) * f * grow * life * 1.2f;
            }
            if (glow != null) glow.intensity = (1.8f + Mathf.Sin(Time.time * 17f) * 0.5f) * life;

            if (!spread && t >= SpreadDelay)
            {
                spread = true;
                Vector3 c = Center;
                for (int i = All.Count - 1; i >= 0; i--)
                {
                    Vine o = All[i];
                    if (o == this || o.burning) continue;
                    if (Vector3.Distance(o.Center, c) <= SpreadRange) o.Ignite();
                }
            }

            if (t >= BurnTime) Burnt();
        }

        void Burnt()
        {
            Vector3 p = transform.position;
            // Cenizas
            GameObject ash = Prim.Cyl(transform.parent, p + Vector3.up * 0.03f, new Vector3(1.5f, 0.02f, 0.9f), new Color(0.18f, 0.16f, 0.15f), false, "Ash");
            Destroy(ash, 25f);
            Fireball.Embers(p + Vector3.up * 0.7f, 7, 2.5f);
            if (Random.value < 0.35f) Pickup.Spawn(p + Vector3.up * 0.4f, PickupKind.Gem, 1);
            if (OnBurnt != null) OnBurnt();
            Destroy(gameObject);
        }
    }
}
