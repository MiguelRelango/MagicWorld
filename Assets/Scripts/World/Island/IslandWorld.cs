using System.Collections.Generic;
using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Mundo abierto en una isla. Genera el terreno por trozos (chunks) de 32 m alrededor del jugador
    /// y descarta los lejanos, como Minecraft. Cada chunk es una malla con colisión y su propio contenido.
    /// </summary>
    public class IslandWorld : MonoBehaviour
    {
        public const int ChunkSize = 32;
        public const int LoadRadius = 3;   // chunks cargados alrededor del jugador
        public const int KeepRadius = 4;   // los que superan este radio se descartan

        public static IslandWorld Instance;
        public static Transform Root;
        public static readonly HashSet<string> Consumed = new HashSet<string>();

        static Material[] mats;
        static readonly Color SkyColor = new Color(0.74f, 0.87f, 0.96f);

        class Chunk
        {
            public Vector2Int Id;
            public GameObject Go;
        }

        readonly Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();
        readonly List<Vector2Int> pending = new List<Vector2Int>();
        Vector2Int lastCenter = new Vector2Int(int.MinValue, int.MinValue);
        Transform sea;
        Vector3 lastSafe;
        float safeTimer;
        float warnUntil;

        public static bool IsConsumed(string key) { return Consumed.Contains(key); }
        public static void Consume(string key) { Consumed.Add(key); }

        // ---------- Construcción ----------

        public static void Build(int seed)
        {
            Clear();
            IslandTerrain.Init(seed);
            Consumed.Clear();

            GameObject go = new GameObject("IslandWorld");
            Root = go.transform;
            Instance = go.AddComponent<IslandWorld>();
            Instance.Setup();
        }

        public static void Clear()
        {
            if (Root != null)
            {
                MeshFilter[] mfs = Root.GetComponentsInChildren<MeshFilter>(true);
                for (int i = 0; i < mfs.Length; i++)
                    if (mfs[i].sharedMesh != null && mfs[i].sharedMesh.name.StartsWith("Chunk")) Destroy(mfs[i].sharedMesh);
                Destroy(Root.gameObject);
            }
            Root = null;
            Instance = null;
            mats = null;
        }

        void Setup()
        {
            BuildMaterials();
            BuildLighting();

            sea = Prim.Box(Root, new Vector3(0f, -0.35f, 0f), new Vector3(700f, 0.2f, 700f), new Color(0.28f, 0.62f, 0.86f), false, "Sea").transform;

            sea.GetComponent<Renderer>().sharedMaterial = IslandSurfaces.Water();

            Vector3 camp = IslandTerrain.Camp;
            lastSafe = camp + Vector3.up * 0.2f;
            Vector2Int c = ChunkOf(camp);

            // Carga inicial síncrona alrededor del campamento para que el jugador no caiga al vacío.
            for (int dx = -2; dx <= 2; dx++)
                for (int dz = -2; dz <= 2; dz++)
                    Generate(new Vector2Int(c.x + dx, c.y + dz));

            IslandProps.BuildCamp(Root, camp);
        }

        static void BuildMaterials()
        {
            mats = new[] { IslandSurfaces.Terrain() };
        }

        static void BuildLighting()
        {
            GameObject sun = new GameObject("IslandSun");
            sun.transform.SetParent(Root, false);
            sun.transform.rotation = Quaternion.Euler(48f, -25f, 0f);
            Light l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(1f, 0.96f, 0.85f);
            l.intensity = 1.15f;
            l.shadows = LightShadows.Soft;
            RenderSettings.sun = l;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.74f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = SkyColor;
            RenderSettings.fogStartDistance = 40f;
            RenderSettings.fogEndDistance = 100f;
            Camera cam = Camera.main;
            if (cam != null) cam.backgroundColor = SkyColor;
        }

        // ---------- Streaming ----------

        static Vector2Int ChunkOf(Vector3 p)
        {
            return new Vector2Int(Mathf.FloorToInt(p.x / ChunkSize), Mathf.FloorToInt(p.z / ChunkSize));
        }

        void Update()
        {
            if (Game.State != GameState.Playing || Game.Player == null) return;
            Vector3 pos = Game.Player.transform.position;

            sea.position = new Vector3(pos.x, -0.35f + Mathf.Sin(Time.time * 0.8f) * 0.05f, pos.z);

            Vector2Int center = ChunkOf(pos);
            if (center != lastCenter)
            {
                lastCenter = center;
                QueueChunks(center);
                Unload(center);
            }

            int budget = pending.Count > 12 ? 2 : 1;
            while (budget > 0 && pending.Count > 0)
            {
                Vector2Int id = pending[0];
                pending.RemoveAt(0);
                if (!chunks.ContainsKey(id)) Generate(id);
                budget--;
            }

            KeepOnLand(pos);
        }

        /// <summary>Genera de inmediato los chunks de alrededor (para teletransportes seguros).</summary>
        public void EnsureAround(Vector3 pos, int radius)
        {
            Vector2Int c = ChunkOf(pos);
            for (int dx = -radius; dx <= radius; dx++)
                for (int dz = -radius; dz <= radius; dz++)
                {
                    Vector2Int id = new Vector2Int(c.x + dx, c.y + dz);
                    if (!chunks.ContainsKey(id)) Generate(id);
                }
            lastSafe = pos + Vector3.up * 0.3f;
        }

        void QueueChunks(Vector2Int center)
        {
            pending.Clear();
            for (int dx = -LoadRadius; dx <= LoadRadius; dx++)
            {
                for (int dz = -LoadRadius; dz <= LoadRadius; dz++)
                {
                    Vector2Int id = new Vector2Int(center.x + dx, center.y + dz);
                    if (!chunks.ContainsKey(id)) pending.Add(id);
                }
            }
            pending.Sort((a, b) =>
            {
                int da = (a.x - center.x) * (a.x - center.x) + (a.y - center.y) * (a.y - center.y);
                int db = (b.x - center.x) * (b.x - center.x) + (b.y - center.y) * (b.y - center.y);
                return da.CompareTo(db);
            });
        }

        void Unload(Vector2Int center)
        {
            List<Vector2Int> remove = null;
            foreach (KeyValuePair<Vector2Int, Chunk> kv in chunks)
            {
                int d = Mathf.Max(Mathf.Abs(kv.Key.x - center.x), Mathf.Abs(kv.Key.y - center.y));
                if (d > KeepRadius)
                {
                    if (remove == null) remove = new List<Vector2Int>();
                    remove.Add(kv.Key);
                }
            }
            if (remove == null) return;
            for (int i = 0; i < remove.Count; i++)
            {
                Chunk ch = chunks[remove[i]];
                if (ch.Go != null)
                {
                    MeshFilter mf = ch.Go.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null) Destroy(mf.sharedMesh);
                    Destroy(ch.Go);
                }
                chunks.Remove(remove[i]);
            }
        }

        /// <summary>El mar profundo no se cruza a pie: si el jugador se mete demasiado, vuelve a tierra.</summary>
        void KeepOnLand(Vector3 pos)
        {
            float h = IslandTerrain.Height(pos.x, pos.z);
            safeTimer -= Time.deltaTime;
            if (h > 0.4f && safeTimer <= 0f)
            {
                lastSafe = new Vector3(pos.x, pos.y + 0.1f, pos.z);
                safeTimer = 0.5f;
            }
            if (h < IslandTerrain.DeepWater || pos.y < -12f)
            {
                Game.Player.Teleport(lastSafe);
                if (Time.time > warnUntil)
                {
                    Game.Say("El mar es demasiado profundo para cruzarlo a pie", 3.5f);
                    warnUntil = Time.time + 4f;
                }
            }
        }

        // ---------- Generación de un chunk ----------

        void Generate(Vector2Int id)
        {
            Chunk ch = new Chunk { Id = id };
            chunks[id] = ch;

            int N = ChunkSize;
            int ox = id.x * N, oz = id.y * N;

            // Altura con un anillo extra alrededor para calcular normales sin costuras entre chunks.
            int G = N + 3;
            float[,] hg = new float[G, G];
            float minH = float.MaxValue, maxH = float.MinValue;
            for (int i = 0; i < G; i++)
            {
                for (int j = 0; j < G; j++)
                {
                    float h = IslandTerrain.Height(ox + i - 1, oz + j - 1);
                    hg[i, j] = h;
                    if (h < minH) minH = h;
                    if (h > maxH) maxH = h;
                }
            }
            // Chunk completamente bajo el agua: el mar opaco lo tapa, no hace falta malla.
            if (maxH < -1.5f) return;

            int V = N + 1;
            Vector3[] verts = new Vector3[V * V];
            Vector3[] norms = new Vector3[V * V];
            Vector2[] uv = new Vector2[V * V];
            for (int i = 0; i < V; i++)
            {
                for (int j = 0; j < V; j++)
                {
                    int k = i * V + j;
                    verts[k] = new Vector3(ox + i, hg[i + 1, j + 1], oz + j);
                    Vector3 n = new Vector3(hg[i, j + 1] - hg[i + 2, j + 1], 2f, hg[i + 1, j] - hg[i + 1, j + 2]);
                    norms[k] = n.normalized;
                    uv[k] = new Vector2((ox + i) / 8f, (oz + j) / 8f);
                }
            }

            List<int> tris = new List<int>(N * N * 6);

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    int ia = i * V + j, ib = i * V + j + 1, ic = (i + 1) * V + j + 1, id2 = (i + 1) * V + j;
                    List<int> t = tris;
                    t.Add(ia); t.Add(ib); t.Add(ic);
                    t.Add(ia); t.Add(ic); t.Add(id2);
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "Chunk " + id.x + "," + id.y;
            mesh.vertices = verts;
            mesh.normals = norms;
            mesh.uv = uv;
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            GameObject go = new GameObject("Chunk " + id.x + "," + id.y);
            go.transform.SetParent(Root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = mats;
            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            ch.Go = go;

            IslandProps.Populate(go.transform, id.x, id.y);
            ForestLook.GrassPatch(go.transform, IslandTerrain.Seed ^ (ox * 73856093) ^ (oz * 19349663),
                ox, oz, N, N, 1200, IslandTerrain.Height,
                (x, z) => IslandTerrain.Height(x, z) > 1.5f && IslandTerrain.Height(x, z) < 10.5f
                    && IslandTerrain.Slope(x, z) < 0.45f
                    && (new Vector2(x - IslandTerrain.Camp.x, z - IslandTerrain.Camp.z)).sqrMagnitude > 100f);
        }

    }
}
