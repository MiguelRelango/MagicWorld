using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hearthbound
{
    /// <summary>Árbol talado que está volviendo a crecer (se recuerda aunque el trozo de mundo se descargue).</summary>
    public class TreeRecord
    {
        public string Id;
        public Vector3 Pos;
        public float Scale;
        public float Start;       // momento (TreeRegistry.Now) en que empezó a crecer
        public float Duration;    // segundos hasta ser árbol adulto
        public bool Planted;
    }

    /// <summary>Registro global de árboles en crecimiento. Los que crecen solos tardan 30 min; los replantados, 10 min.</summary>
    public static class TreeRegistry
    {
        public const float NaturalGrowSeconds = 1800f;   // 30 minutos
        public const float PlantedGrowSeconds = 600f;    // 10 minutos

        public static readonly Dictionary<string, TreeRecord> Records = new Dictionary<string, TreeRecord>();
        static float offset;       // el modo dev puede adelantar el reloj de los árboles
        static int plantedCounter;
        static Transform plantRoot;

        public static float Now { get { return Time.time + offset; } }

        public static void Skip(float seconds) { offset += seconds; }

        public static TreeRecord Get(string id)
        {
            TreeRecord r;
            return Records.TryGetValue(id, out r) ? r : null;
        }

        public static void Clear()
        {
            Records.Clear();
            offset = 0f;
            plantedCounter = 0;
            if (plantRoot != null) Object.Destroy(plantRoot.gameObject);
            plantRoot = null;
        }

        /// <summary>Planta una semilla: brote que tarda 10 minutos en ser un árbol.</summary>
        public static TreeNode Plant(Vector3 pos)
        {
            if (plantRoot == null) plantRoot = new GameObject("PlantedTrees").transform;
            TreeRecord r = new TreeRecord
            {
                Id = "p" + (++plantedCounter) + "_" + Mathf.RoundToInt(Time.time * 10f),
                Pos = pos,
                Scale = Random.Range(0.9f, 1.2f),
                Start = Now,
                Duration = PlantedGrowSeconds,
                Planted = true
            };
            Records[r.Id] = r;
            return TreeNode.Create(plantRoot, pos, r.Scale, r);
        }
    }

    /// <summary>
    /// Árbol del mundo: se tala con el hacha (4 golpes), suelta madera y semillas, y vuelve a crecer
    /// desde un brote junto al tocón. Los árboles replantados con semillas viven en una raíz persistente.
    /// </summary>
    public class TreeNode : MonoBehaviour
    {
        public static readonly List<TreeNode> All = new List<TreeNode>();

        public string Id;
        public float Scale = 1f;
        public bool Planted;

        const int MaxHits = 4;
        Transform vis;
        GameObject stump;
        TreeRecord rec;
        bool growing, falling;
        int hits;
        float shake;
        float nextCheck;

        public bool IsGrown { get { return !growing && !falling; } }

        /// <summary>0..1 de progreso de crecimiento (1 = adulto).</summary>
        public float Progress
        {
            get
            {
                if (rec == null) return 1f;
                return Mathf.Clamp01((TreeRegistry.Now - rec.Start) / Mathf.Max(1f, rec.Duration));
            }
        }

        public float SecondsLeft
        {
            get { return rec == null ? 0f : Mathf.Max(0f, rec.Duration - (TreeRegistry.Now - rec.Start)); }
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        static string IdFor(Vector3 p)
        {
            return "t" + Mathf.RoundToInt(p.x * 10f) + "," + Mathf.RoundToInt(p.z * 10f);
        }

        public static TreeNode Create(Transform parent, Vector3 pos, float scale, TreeRecord forced = null)
        {
            GameObject t = new GameObject("Tree");
            t.transform.SetParent(parent, false);
            t.transform.position = pos;
            TreeNode n = t.AddComponent<TreeNode>();
            n.Scale = scale;
            n.Id = forced != null ? forced.Id : IdFor(pos);

            GameObject v = new GameObject("Visual");
            v.transform.SetParent(t.transform, false);
            n.vis = v.transform;
            Prim.Cyl(n.vis, new Vector3(0f, 1.1f * scale, 0f), new Vector3(0.55f, 1.1f, 0.55f) * scale, new Color(0.50f, 0.34f, 0.22f), true, "Trunk")
                .GetComponent<Renderer>().sharedMaterial = ForestLook.Surface("Bark");
            ForestLook.Canopy(n.vis, scale);

            TreeRecord r = forced != null ? forced : TreeRegistry.Get(n.Id);
            if (r != null)
            {
                if (TreeRegistry.Now - r.Start >= r.Duration) TreeRegistry.Records.Remove(r.Id);   // ya creció mientras no mirabas
                else { n.rec = r; n.Planted = r.Planted; n.BeginGrowing(); }
            }
            return n;
        }

        // ---------- Crecimiento ----------

        void BeginGrowing()
        {
            growing = true;
            hits = 0;
            if (stump == null)
            {
                stump = Prim.Cyl(transform, new Vector3(0f, 0.18f * Scale, 0f), new Vector3(0.6f, 0.18f, 0.6f) * Scale, new Color(0.45f, 0.30f, 0.19f), false, "Stump");
            }
            ApplyGrowth();
        }

        void ApplyGrowth()
        {
            float p = Progress;
            float s = Mathf.Lerp(0.14f, 1f, p * p * (3f - 2f * p));
            vis.localScale = Vector3.one * s;
            vis.localRotation = Quaternion.identity;
            // Un brote no estorba el paso; el árbol casi adulto sí.
            Collider c = vis.GetComponentInChildren<Collider>();
            if (c != null) c.enabled = p > 0.6f;
        }

        void Update()
        {
            if (shake > 0f)
            {
                shake -= Time.deltaTime;
                float a = Mathf.Sin(Time.time * 55f) * 3.5f * Mathf.Clamp01(shake / 0.25f);
                if (!falling) vis.localRotation = Quaternion.Euler(a * 0.5f, 0f, a);
                if (shake <= 0f && !falling) vis.localRotation = Quaternion.identity;
            }

            if (!growing || falling) return;
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + 0.5f;
            if (Progress >= 1f) Mature(); else ApplyGrowth();
        }

        void Mature()
        {
            growing = false;
            if (rec != null) TreeRegistry.Records.Remove(rec.Id);
            rec = null;
            vis.localScale = Vector3.one;
            Collider c = vis.GetComponentInChildren<Collider>();
            if (c != null) c.enabled = true;
            if (stump != null) { Destroy(stump); stump = null; }
            Sfx.Play("heal", 1.6f, 0.25f);
        }

        // ---------- Tala ----------

        /// <summary>Un hachazo. Devuelve false si no se pudo talar (brote).</summary>
        public bool Chop(Vector3 from)
        {
            if (falling) return false;
            if (growing)
            {
                Game.Say("Aún es un brote: crecerá en " + FormatTime(SecondsLeft), 2.5f);
                return false;
            }
            hits++;
            shake = 0.3f;
            Sfx.Play("chop", 0.9f + hits * 0.08f, 0.9f);
            Vector3 up = transform.position + Vector3.up * 1.4f;
            for (int i = 0; i < 4; i++) Chip.Spawn(up, (transform.position - from).normalized);
            if (hits >= MaxHits) Fall(from);
            else Game.Say("Árbol: " + (MaxHits - hits) + " hachazos más", 1.2f);
            return true;
        }

        /// <summary>Cuarto hachazo: el botín y el rebrote se registran ya (aunque el trozo de mundo se descargue durante la caída).</summary>
        void Fall(Vector3 from)
        {
            falling = true;
            Vector3 away = Nav.Flat(transform.position - from);
            if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
            away.Normalize();

            Vector3 drop = transform.position + away * 1.4f + Vector3.up * 0.6f;
            RaycastHit gh;
            if (Physics.Raycast(drop + Vector3.up * 3f, Vector3.down, out gh, 8f, ~0, QueryTriggerInteraction.Ignore)) drop.y = gh.point.y + 0.5f;
            int wood = Mathf.Clamp(Mathf.RoundToInt(2f + Scale * 2f), 3, 6);
            for (int i = 0; i < wood; i++)
                Pickup.Spawn(drop + new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)), PickupKind.Wood, 1);
            int seeds = Random.value < 0.6f ? (Random.value < 0.3f ? 2 : 1) : 0;
            for (int i = 0; i < seeds; i++)
                Pickup.Spawn(drop + new Vector3(Random.Range(-1.2f, 1.2f), 0f, Random.Range(-1.2f, 1.2f)), PickupKind.Seed, 1);
            Chip.Burst(drop, 10);

            TreeRegistry.Records[Id] = new TreeRecord
            {
                Id = Id,
                Pos = transform.position,
                Scale = Scale,
                Start = TreeRegistry.Now,
                Duration = Planted ? TreeRegistry.PlantedGrowSeconds : TreeRegistry.NaturalGrowSeconds,
                Planted = Planted
            };
            StartCoroutine(FallAnim(away));
        }

        IEnumerator FallAnim(Vector3 away)
        {
            Vector3 axis = Vector3.Cross(Vector3.up, away);
            Sfx.Play("door", 1.8f, 0.6f);

            float t = 0f;
            while (t < 0.9f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.9f);
                vis.localRotation = Quaternion.AngleAxis(88f * k * k, axis);
                yield return null;
            }

            rec = TreeRegistry.Get(Id);
            falling = false;
            BeginGrowing();
        }

        public static string FormatTime(float s)
        {
            int total = Mathf.CeilToInt(s);
            return (total / 60) + ":" + (total % 60).ToString("00");
        }

        /// <summary>Brote en crecimiento más cercano (para el aviso del HUD).</summary>
        public static TreeNode NearestGrowing(Vector3 pos, float range)
        {
            TreeNode best = null;
            float bd = range;
            for (int i = 0; i < All.Count; i++)
            {
                TreeNode n = All[i];
                if (n == null || !n.growing || n.falling) continue;
                float d = Vector3.Distance(Nav.Flat(n.transform.position), Nav.Flat(pos));
                if (d < bd) { bd = d; best = n; }
            }
            return best;
        }

        /// <summary>Árbol adulto o brote más cercano en el arco frontal del jugador.</summary>
        public static TreeNode Target(Vector3 pos, Vector3 forward, float range, float coneDeg)
        {
            TreeNode best = null;
            float bs = float.MaxValue;
            for (int i = 0; i < All.Count; i++)
            {
                TreeNode n = All[i];
                if (n == null || n.falling) continue;
                Vector3 to = Nav.Flat(n.transform.position - pos);
                float d = to.magnitude;
                if (d > range + 0.4f * n.Scale) continue;
                if (d > 0.3f && Vector3.Angle(forward, to) > coneDeg) continue;
                float score = d + (n.growing ? 0.5f : 0f);
                if (score < bs) { bs = score; best = n; }
            }
            return best;
        }
    }

    /// <summary>Astilla de madera que sale despedida al talar y desaparece.</summary>
    public class Chip : MonoBehaviour
    {
        Vector3 vel;
        float life = 0.8f;
        Vector3 baseScale = Vector3.one;

        public static void Spawn(Vector3 pos, Vector3 dir)
        {
            Vector3 v = dir * Random.Range(1.5f, 3.5f) + Vector3.up * Random.Range(1.5f, 3.5f) + Random.insideUnitSphere * 1.2f;
            Make(pos, v);
        }

        public static void Burst(Vector3 pos, int n)
        {
            for (int i = 0; i < n; i++) Make(pos, Random.onUnitSphere * 3f + Vector3.up * 2f);
        }

        static void Make(Vector3 pos, Vector3 v)
        {
            Color c = Random.value < 0.5f ? new Color(0.62f, 0.44f, 0.26f) : new Color(0.40f, 0.62f, 0.30f);
            GameObject go = Prim.Box(null, pos, new Vector3(0.1f, 0.05f, 0.16f), c, false, "Chip");
            go.transform.rotation = Random.rotation;
            Chip ch = go.AddComponent<Chip>();
            ch.vel = v;
            ch.baseScale = go.transform.localScale;
            ch.life = Random.Range(0.5f, 0.9f);
        }

        void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f) { Destroy(gameObject); return; }
            vel += Vector3.down * 12f * Time.deltaTime;
            transform.position += vel * Time.deltaTime;
            transform.Rotate(300f * Time.deltaTime, 200f * Time.deltaTime, 0f);
            transform.localScale = baseScale * Mathf.Clamp01(life / 0.3f);
        }
    }
}
