using System.IO;
using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Mapa descubierto de la isla. El terreno es una función pura de (x, z), así que el mapa se pinta
    /// por celdas a medida que el jugador explora (radio de revelado ~55 m) y se guarda en disco por semilla.
    /// Lo que no se ha visitado queda oculto, incluso lejos del área cargada en 3D.
    /// </summary>
    public static class IslandMap
    {
        public const float Half = 330f;          // el mapa cubre [-Half, Half] en x y z
        public const int N = 440;                // celdas por lado (1,5 m por celda)
        public const float Cell = 2f * Half / N;
        const float RevealRadius = 55f;

        public static bool Active { get; private set; }
        public static Texture2D Texture { get { return tex; } }
        public static int SeenCount { get; private set; }

        static Texture2D tex;
        static Color32[] pix;
        static byte[] seen;
        static int seed;
        static bool dirty, unsaved;
        static float nextApply, nextSave;
        static Vector3 lastReveal = new Vector3(float.MaxValue, 0f, 0f);

        // ---------- Ciclo de vida ----------

        public static void Begin(int s)
        {
            End();
            seed = s;
            Active = true;
            pix = new Color32[N * N];
            seen = new byte[N * N];
            SeenCount = 0;
            tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Load();
            for (int j = 0; j < N; j++)
                for (int i = 0; i < N; i++)
                    if (seen[j * N + i] != 0) pix[j * N + i] = ColorAt(CellX(i), CellZ(j));
            tex.SetPixels32(pix);
            tex.Apply(false);
            dirty = false;
            unsaved = false;
            lastReveal = new Vector3(float.MaxValue, 0f, 0f);
            nextSave = Time.unscaledTime + 30f;
        }

        public static void End()
        {
            if (Active) Save();
            Active = false;
            if (tex != null) Object.Destroy(tex);
            tex = null;
            pix = null;
            seen = null;
        }

        static float CellX(int i) { return -Half + (i + 0.5f) * Cell; }
        static float CellZ(int j) { return -Half + (j + 0.5f) * Cell; }

        // ---------- Descubrimiento ----------

        /// <summary>Se llama cada fotograma con la posición del jugador.</summary>
        public static void Tick(Vector3 p)
        {
            if (!Active) return;
            Vector3 d = p - lastReveal;
            d.y = 0f;
            if (d.sqrMagnitude > 4f)
            {
                lastReveal = p;
                Reveal(p.x, p.z, RevealRadius);
            }
            float now = Time.unscaledTime;
            if (dirty && now >= nextApply)
            {
                tex.SetPixels32(pix);
                tex.Apply(false);
                dirty = false;
                nextApply = now + 0.3f;
            }
            if (unsaved && now >= nextSave)
            {
                Save();
                nextSave = now + 30f;
            }
        }

        static void Reveal(float x, float z, float r)
        {
            int i0 = Mathf.Max(0, Mathf.FloorToInt((x - r + Half) / Cell));
            int i1 = Mathf.Min(N - 1, Mathf.FloorToInt((x + r + Half) / Cell));
            int j0 = Mathf.Max(0, Mathf.FloorToInt((z - r + Half) / Cell));
            int j1 = Mathf.Min(N - 1, Mathf.FloorToInt((z + r + Half) / Cell));
            float r2 = r * r;
            for (int j = j0; j <= j1; j++)
            {
                float cz = CellZ(j);
                for (int i = i0; i <= i1; i++)
                {
                    int idx = j * N + i;
                    if (seen[idx] != 0) continue;
                    float cx = CellX(i);
                    float dx = cx - x, dz = cz - z;
                    if (dx * dx + dz * dz > r2) continue;
                    seen[idx] = 1;
                    SeenCount++;
                    pix[idx] = ColorAt(cx, cz);
                    dirty = true;
                    unsaved = true;
                }
            }
        }

        public static bool IsDiscovered(Vector3 w)
        {
            if (!Active) return true;
            int i = Mathf.FloorToInt((w.x + Half) / Cell), j = Mathf.FloorToInt((w.z + Half) / Cell);
            if (i < 0 || j < 0 || i >= N || j >= N) return false;
            return seen[j * N + i] != 0;
        }

        /// <summary>Porcentaje aproximado de la isla descubierta (respecto a un disco del radio de la isla).</summary>
        public static float Percent
        {
            get { return Mathf.Min(100f, SeenCount * Cell * Cell / (Mathf.PI * IslandTerrain.Radius * IslandTerrain.Radius) * 100f); }
        }

        /// <summary>Modo dev: descubre todo el mapa.</summary>
        public static void RevealAll()
        {
            if (!Active) return;
            Reveal(0f, 0f, Half * 1.5f);
            tex.SetPixels32(pix);
            tex.Apply(false);
            dirty = false;
        }

        /// <summary>Modo dev: olvida todo lo descubierto.</summary>
        public static void Forget()
        {
            if (!Active) return;
            System.Array.Clear(seen, 0, seen.Length);
            System.Array.Clear(pix, 0, pix.Length);
            SeenCount = 0;
            tex.SetPixels32(pix);
            tex.Apply(false);
            lastReveal = new Vector3(float.MaxValue, 0f, 0f);
            unsaved = true;
            Save();
        }

        // ---------- Pintado de una celda ----------

        static Color32 ColorAt(float x, float z)
        {
            float h = IslandTerrain.Height(x, z);
            Color c;
            if (h < IslandTerrain.DeepWater - 2.5f) c = new Color(0.12f, 0.30f, 0.52f);          // mar profundo
            else if (h < IslandTerrain.DeepWater) c = new Color(0.20f, 0.45f, 0.70f);            // mar
            else if (h < 0f) c = new Color(0.36f, 0.68f, 0.88f);                                 // aguas someras
            else if (h < 1.1f) c = new Color(0.94f, 0.85f, 0.60f);                               // playa
            else if (h > 21f) c = new Color(0.95f, 0.96f, 1.00f);                                // nieve
            else if (h > 11f) c = new Color(0.56f, 0.54f, 0.53f);                                // roca
            else
            {
                c = new Color(0.50f, 0.76f, 0.35f);                                              // hierba
                if (IslandTerrain.Forest(x, z) > 0.45f) c = new Color(0.33f, 0.58f, 0.27f);      // bosque
            }

            // Relieve: luz desde el noroeste.
            if (h >= 0f)
            {
                float hNW = IslandTerrain.Height(x - Cell, z + Cell);
                float hSE = IslandTerrain.Height(x + Cell, z - Cell);
                float shade = Mathf.Clamp((hSE - hNW) * 0.09f, -0.28f, 0.28f);
                c = new Color(Mathf.Clamp01(c.r + shade), Mathf.Clamp01(c.g + shade), Mathf.Clamp01(c.b + shade), 1f);
            }
            return c;
        }

        // ---------- Guardado ----------

        static string PathFor()
        {
            return Path.Combine(Application.persistentDataPath, "hearthbound_map_" + seed + ".bin");
        }

        public static void Save()
        {
            if (!Active || seen == null) return;
            try
            {
                byte[] bits = new byte[(N * N + 7) / 8];
                for (int k = 0; k < seen.Length; k++)
                    if (seen[k] != 0) bits[k >> 3] |= (byte)(1 << (k & 7));
                File.WriteAllBytes(PathFor(), bits);
                unsaved = false;
            }
            catch (System.Exception) { /* sin permisos de escritura: el mapa vale solo para esta sesión */ }
        }

        static void Load()
        {
            try
            {
                string path = PathFor();
                if (!File.Exists(path)) return;
                byte[] bits = File.ReadAllBytes(path);
                if (bits.Length != (N * N + 7) / 8) return;
                for (int k = 0; k < seen.Length; k++)
                {
                    if ((bits[k >> 3] & (1 << (k & 7))) != 0) { seen[k] = 1; SeenCount++; }
                }
            }
            catch (System.Exception) { }
        }
    }
}
