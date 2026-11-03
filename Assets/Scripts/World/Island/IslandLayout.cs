using System.Collections.Generic;
using UnityEngine;

namespace Hearthbound
{
    public enum IslandRegion { Plains, Swamp, Lava, Desert, City, Harbor }

    /// <summary>Un lugar importante de la isla: una elipse del mapa que aplana el terreno y marca una región.</summary>
    public class Site
    {
        public string Name;
        public Vector2 C;            // centro (x, z)
        public float Rx, Rz;         // semiejes en metros
        public float Blend;          // anchura del borde de transición respecto al semieje
        public float H;              // altura objetivo del terreno
        public float Strength;       // 0 = no toca la altura, 1 = completamente plano
        public float Block = 0.9f;   // fracción de la elipse que queda libre de árboles y enemigos
        public IslandRegion Region;

        public Vector3 Pos { get { return new Vector3(C.x, H, C.y); } }
    }

    public class PeakDef
    {
        public float X, Z, H, R;
    }

    /// <summary>
    /// Plano de la isla según el mapa de referencia: dónde están la ciudad amurallada, el puerto del desierto,
    /// el puerto pesquero, el pueblo del centro, el árbol gigante, el pantano con niebla y las montañas de lava.
    /// Todo son datos fijos: la semilla solo cambia el detalle del terreno y el contenido suelto.
    /// </summary>
    public static class IslandLayout
    {
        public static readonly Site City = Make("Ciudad", 912f, 275f, 62f, 48f, 0.25f, 5.0f, 1f, IslandRegion.City);
        public static readonly Site Desert = Make("Puerto del desierto", 1115f, 522f, 52f, 50f, 0.20f, 2.6f, 1f, IslandRegion.Desert);
        public static readonly Site Fishing = Make("Puerto pesquero", 822f, 650f, 20f, 34f, 0.35f, 1.2f, 1f, IslandRegion.Harbor);
        public static readonly Site Town = Make("Pueblo", 655f, 500f, 36f, 36f, 0.45f, 4.4f, 1f, IslandRegion.Plains);
        public static readonly Site GiantTree = Make("Árbol gigante", 130f, 75f, 40f, 34f, 0.5f, 5.4f, 1f, IslandRegion.Plains);
        public static readonly Site Keep = Make("Fortaleza", 450f, 225f, 36f, 22f, 0.35f, 5.2f, 1f, IslandRegion.Plains);
        public static readonly Site Tower = Make("Torre del sur", 420f, 745f, 14f, 14f, 0.6f, 3.4f, 1f, IslandRegion.Plains);
        public static readonly Site Swamp = Make("Bosque con niebla", 245f, 590f, 58f, 80f, 0.35f, 1.5f, 0.7f, IslandRegion.Swamp);
        public static readonly Site Volcano = Make("Montañas de lava", 665f, 215f, 98f, 58f, 0.55f, 0f, 0f, IslandRegion.Lava);

        public static readonly List<Site> Sites = new List<Site> { City, Desert, Fishing, Town, GiantTree, Keep, Tower, Swamp, Volcano };

        public static readonly PeakDef[] Peaks =
        {
            Peak(735f, 130f, 17.5f, 34f), Peak(545f, 160f, 11f, 24f), Peak(600f, 175f, 9f, 20f),
            Peak(690f, 175f, 12f, 26f), Peak(790f, 170f, 9f, 20f), Peak(505f, 128f, 7.5f, 18f),
            Peak(1075f, 360f, 6f, 16f)
        };

        /// <summary>Charcos de lava (x, z, radio).</summary>
        public static readonly Vector3[] Lava =
        {
            Pool(750f, 297f, 9f), Pool(688f, 265f, 5.5f), Pool(600f, 217f, 5f), Pool(745f, 240f, 3.2f)
        };

        /// <summary>Entradas de las mazmorras en las montañas (x, z).</summary>
        public static readonly Vector2[] DungeonSpots =
        {
            IslandShape.FromPx(735f, 160f), IslandShape.FromPx(690f, 213f), IslandShape.FromPx(545f, 185f)
        };

        /// <summary>Cabañas sueltas (posiciones del mapa).</summary>
        public static readonly Vector2[] Cabins =
        {
            IslandShape.FromPx(545f, 420f), IslandShape.FromPx(625f, 420f), IslandShape.FromPx(765f, 425f), IslandShape.FromPx(690f, 440f),
            IslandShape.FromPx(545f, 465f), IslandShape.FromPx(590f, 470f), IslandShape.FromPx(770f, 495f), IslandShape.FromPx(555f, 530f),
            IslandShape.FromPx(610f, 565f), IslandShape.FromPx(665f, 570f), IslandShape.FromPx(760f, 560f), IslandShape.FromPx(415f, 530f),
            IslandShape.FromPx(715f, 680f), IslandShape.FromPx(385f, 330f), IslandShape.FromPx(500f, 265f), IslandShape.FromPx(665f, 345f),
            IslandShape.FromPx(910f, 420f), IslandShape.FromPx(1000f, 385f), IslandShape.FromPx(250f, 150f), IslandShape.FromPx(150f, 155f),
            IslandShape.FromPx(60f, 150f), IslandShape.FromPx(200f, 330f)
        };

        // Bosques densos (x, z, radio x, radio z, peso).
        static readonly float[][] ForestBlobs =
        {
            B(210f, 290f, 65f, 60f, 0.55f), B(130f, 215f, 60f, 18f, 0.50f), B(400f, 140f, 40f, 15f, 0.40f),
            B(450f, 400f, 70f, 9f, 0.50f), B(560f, 650f, 90f, 28f, 0.55f), B(430f, 520f, 30f, 35f, 0.50f),
            B(940f, 480f, 45f, 40f, 0.50f), B(840f, 410f, 70f, 8f, 0.45f)
        };

        public static int Seed;

        public static void Init(int seed)
        {
            Seed = seed;
        }

        static Site Make(string name, float px, float py, float rx, float rz, float blend, float h, float strength, IslandRegion region)
        {
            Vector2 c = IslandShape.FromPx(px, py);
            return new Site { Name = name, C = c, Rx = rx, Rz = rz, Blend = blend, H = h, Strength = strength, Region = region };
        }

        static PeakDef Peak(float px, float py, float h, float r)
        {
            Vector2 c = IslandShape.FromPx(px, py);
            return new PeakDef { X = c.x, Z = c.y, H = h, R = r };
        }

        static Vector3 Pool(float px, float py, float r)
        {
            Vector2 c = IslandShape.FromPx(px, py);
            return new Vector3(c.x, r, c.y);   // y guarda el radio
        }

        static float[] B(float px, float py, float rx, float rz, float w)
        {
            Vector2 c = IslandShape.FromPx(px, py);
            return new[] { c.x, c.y, rx, rz, w };
        }

        static float Smooth(float a, float b, float v)
        {
            float t = Mathf.Clamp01((v - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        // ---------- Consultas ----------

        /// <summary>Distancia normalizada a la elipse del lugar: 1 en su borde.</summary>
        public static float Norm(Site s, float x, float z)
        {
            float dx = (x - s.C.x) / s.Rx, dz = (z - s.C.y) / s.Rz;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>1 dentro de la elipse, bajando a 0 a lo largo del borde de transición.</summary>
        public static float Weight(Site s, float x, float z)
        {
            return 1f - Smooth(1f, 1f + s.Blend, Norm(s, x, z));
        }

        /// <summary>Aplana (o hunde) la altura según los lugares que cubren el punto.</summary>
        public static float Flatten(float x, float z, float h)
        {
            for (int i = 0; i < Sites.Count; i++)
            {
                Site s = Sites[i];
                if (s.Strength <= 0f) continue;
                float w = Weight(s, x, z) * s.Strength;
                if (w > 0f) h = Mathf.Lerp(h, s.H, w);
            }
            return h;
        }

        /// <summary>Altura que aportan las montañas.</summary>
        public static float Mountains(float x, float z)
        {
            float m = 0f;
            for (int i = 0; i < Peaks.Length; i++)
            {
                PeakDef p = Peaks[i];
                float dx = x - p.X, dz = z - p.Z;
                float u = (dx * dx + dz * dz) / (p.R * p.R);
                if (u > 6f) continue;
                m = Mathf.Max(m, p.H * Mathf.Exp(-u * 1.5f));
            }
            return m;
        }

        /// <summary>Cuánto se hunde el terreno en los charcos de lava.</summary>
        public static float LavaBowl(float x, float z)
        {
            float depth = 0f;
            for (int i = 0; i < Lava.Length; i++)
            {
                float dx = x - Lava[i].x, dz = z - Lava[i].z;
                float r = Lava[i].y;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d > r * 2f) continue;
                depth = Mathf.Max(depth, 1.3f * (1f - Smooth(r * 0.7f, r * 1.9f, d)));
            }
            return depth;
        }

        /// <summary>Estanque del centro de la isla (aguas poco profundas).</summary>
        public static float PondWeight(float x, float z)
        {
            Vector2 c = IslandShape.FromPx(588f, 350f);
            float dx = (x - c.x) / 15f, dz = (z - c.y) / 22f;
            float d = Mathf.Sqrt(dx * dx + dz * dz);
            return 1f - Smooth(0.8f, 1.25f, d);
        }

        /// <summary>Densidad extra de bosque 0..1 en los bosques densos del mapa.</summary>
        public static float ForestBlob(float x, float z)
        {
            float f = 0f;
            for (int i = 0; i < ForestBlobs.Length; i++)
            {
                float[] b = ForestBlobs[i];
                float dx = (x - b[0]) / b[2], dz = (z - b[1]) / b[3];
                float u = dx * dx + dz * dz;
                if (u > 3f) continue;
                f = Mathf.Max(f, b[4] * Mathf.Exp(-u * 1.3f));
            }
            return f;
        }

        /// <summary>¿Se puede construir una cabaña aquí? (en tierra seca, llana y fuera de pueblos y agua).</summary>
        public static bool CabinOk(float x, float z)
        {
            float h = IslandTerrain.Height(x, z);
            if (h < 1.6f || h > 9f) return false;
            if (IslandTerrain.Slope(x, z) > 0.4f) return false;
            if (IslandShape.River(x, z) > -7f) return false;
            if (PondWeight(x, z) > 0.01f) return false;
            if (Village.Exists && Village.Blocks(x, z, 2f)) return false;
            for (int i = 0; i < Sites.Count; i++)
            {
                Site s = Sites[i];
                if (s.Region == IslandRegion.Plains && s != Town) { if (Norm(s, x, z) < 1.05f) return false; }
                else if (s.Region != IslandRegion.Swamp && Norm(s, x, z) < 1.05f) return false;
            }
            for (int i = 0; i < Lava.Length; i++)
            {
                float dx = x - Lava[i].x, dz = z - Lava[i].z;
                float r = Lava[i].y + 4f;
                if (dx * dx + dz * dz < r * r) return false;
            }
            return true;
        }

        /// <summary>Región del terreno en un punto.</summary>
        public static IslandRegion RegionAt(float x, float z)
        {
            for (int i = 0; i < Sites.Count; i++)
            {
                Site s = Sites[i];
                if (s.Region == IslandRegion.Plains) continue;
                if (Norm(s, x, z) < 1f) return s.Region;
            }
            return IslandRegion.Plains;
        }

        /// <summary>Peso del pantano 0..1 (para la niebla del jugador).</summary>
        public static float SwampWeight(float x, float z)
        {
            return Weight(Swamp, x, z);
        }

        /// <summary>Lugar con el que se solapa un punto (null si no hay), con margen en metros.</summary>
        public static bool Blocks(float x, float z, float margin)
        {
            for (int i = 0; i < Sites.Count; i++)
            {
                Site s = Sites[i];
                if (s.Region == IslandRegion.Swamp || s.Region == IslandRegion.Lava) continue;
                float dx = x - s.C.x, dz = z - s.C.y;
                float rx = s.Rx * s.Block + margin, rz = s.Rz * s.Block + margin;
                if (s == GiantTree) { rx = 23f + margin; rz = 23f + margin; }
                if (s == Town) { rx = 30f + margin; rz = 30f + margin; }
                if (s == Keep) { rx = 30f + margin; rz = 17f + margin; }
                if ((dx * dx) / (rx * rx) + (dz * dz) / (rz * rz) < 1f) return true;
            }
            // Charcos de lava
            for (int i = 0; i < Lava.Length; i++)
            {
                float dx = x - Lava[i].x, dz = z - Lava[i].z;
                float r = Lava[i].y + 2f + margin;
                if (dx * dx + dz * dz < r * r) return true;
            }
            // Cabañas sueltas
            for (int i = 0; i < Cabins.Length; i++)
            {
                float dx = x - Cabins[i].x, dz = z - Cabins[i].y;
                float r = 7f + margin;
                if (dx * dx + dz * dz < r * r) return true;
            }
            // Muralla norte y charco del estanque
            if (z > 100f - margin && z < 114f + margin && x > -198f - margin && x < -66f + margin) return true;
            if (PondWeight(x, z) > 0.2f) return true;
            return false;
        }

        /// <summary>Colores de vértice del terreno. Blanco = sin efecto: R arena, G ceniza, B adoquín, A pantano (invertidos).</summary>
        public static Color32 Tint(float x, float z)
        {
            float sand = Weight(Desert, x, z);
            float ash = Weight(Volcano, x, z);
            float paved = Mathf.Max(Weight(City, x, z), Weight(Fishing, x, z) * 0.8f);
            float swamp = Weight(Swamp, x, z);
            return new Color32((byte)(255f * (1f - sand)), (byte)(255f * (1f - ash)), (byte)(255f * (1f - paved)), (byte)(255f * (1f - swamp)));
        }

        public static void PopulateChunk(Transform chunk, int cx, int cz)
        {
            Landmarks.Populate(chunk, cx, cz);
        }
    }
}
