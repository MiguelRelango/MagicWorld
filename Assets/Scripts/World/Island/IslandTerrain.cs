using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Terreno procedural de la isla. Es una función pura de (x, z) y la semilla, así que
    /// cualquier chunk puede generarse en cualquier momento y siempre da el mismo resultado.
    /// La costa y los ríos salen del mapa de referencia (IslandShape) y los lugares importantes
    /// del plano de la isla (IslandLayout). El nivel del mar está en y = 0.
    /// </summary>
    public static class IslandTerrain
    {
        public const float Radius = 330f;       // la isla cabe en [-Radius, Radius]
        public const float LandArea = 155000f;  // superficie aproximada de la isla en m²
        public const float SeaLevel = 0f;
        public const float DeepWater = -0.9f;   // por debajo de esta altura no se puede caminar
        public const float RiverBed = -0.62f;   // fondo de ríos y estanques: se vadean con el agua a media pierna

        public static int Seed;
        public static Vector3 Camp;

        static float ox1, oz1, ox2, oz2, ox3, oz3, ox4, oz4;

        public static void Init(int seed)
        {
            Seed = seed;
            System.Random r = new System.Random(seed);
            ox1 = Off(r); oz1 = Off(r);
            ox2 = Off(r); oz2 = Off(r);
            ox3 = Off(r); oz3 = Off(r);
            ox4 = Off(r); oz4 = Off(r);
            Camp = FindCamp();
        }

        static float Off(System.Random r)
        {
            return (float)(r.NextDouble() * 5000.0 + 2000.0);
        }

        static float Fbm(float x, float z, float ox, float oz, int octaves, float freq)
        {
            float sum = 0f, amp = 1f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += amp * Mathf.PerlinNoise((x + ox) * freq, (z + oz) * freq);
                norm += amp;
                amp *= 0.5f;
                freq *= 2f;
            }
            return sum / norm;
        }

        static float Smooth(float a, float b, float v)
        {
            float t = Mathf.Clamp01((v - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        /// <summary>Distancia con signo a la costa (m), con la orilla algo irregular.</summary>
        public static float Coast(float x, float z)
        {
            float wob = (Fbm(x, z, ox1, oz1, 2, 0.045f) - 0.5f) * 5f;
            return IslandShape.Coast(x, z) + wob;
        }

        /// <summary>0 en mar abierto, 1 tierra adentro.</summary>
        public static float Land(float x, float z)
        {
            return Smooth(-6f, 12f, Coast(x, z));
        }

        public static float Height(float x, float z)
        {
            float sdf = Coast(x, z);
            float inland = Smooth(6f, 30f, sdf);

            // Meseta suave con colinas, y un perfil de playa que baja hasta el fondo del mar.
            float hills = (Fbm(x, z, ox3, oz3, 4, 0.012f) - 0.5f) * 2f;
            float plateau = 4.4f + hills * 1.5f * inland;
            float h = Mathf.Lerp(-5f, plateau, Smooth(-14f, 14f, sdf));

            // Montañas de lava (con aristas) y charcos hundidos.
            float m = IslandLayout.Mountains(x, z);
            if (m > 0.01f)
            {
                float crag = 0.82f + 0.36f * Fbm(x, z, ox2, oz2, 3, 0.05f);
                h += m * crag * Smooth(0f, 8f, sdf);
            }
            h = Mathf.Min(h, 19.2f);   // por debajo de la nieve
            h -= IslandLayout.LavaBowl(x, z);

            // Ríos, lagos del pantano y estanque central: se carvan hasta un fondo vadeable.
            float rv = IslandShape.River(x, z);
            if (rv > -6f)
            {
                float w = Smooth(-5f, 1.5f, rv);
                h = Mathf.Lerp(h, Mathf.Min(h, RiverBed), w);
            }
            float pond = IslandLayout.PondWeight(x, z);
            if (pond > 0f) h = Mathf.Lerp(h, Mathf.Min(h, RiverBed), pond);

            // Ciudades, pueblo, puertos, pantano...
            h = IslandLayout.Flatten(x, z, h);

            float detail = Fbm(x, z, ox4, oz4, 2, 0.07f) - 0.5f;
            float calm = 1f - Mathf.Max(IslandLayout.Weight(IslandLayout.City, x, z), Mathf.Max(IslandLayout.Weight(IslandLayout.Desert, x, z), IslandLayout.Weight(IslandLayout.Town, x, z)));
            h += detail * 1.4f * Smooth(0.45f, 0.65f, Land(x, z)) * calm;
            return h;
        }

        public static float Slope(float x, float z)
        {
            float dx = Height(x + 1f, z) - Height(x - 1f, z);
            float dz = Height(x, z + 1f) - Height(x, z - 1f);
            return Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) * 0.5f;
        }

        /// <summary>Densidad de bosque 0..1 (manchas grandes más los bosques densos del mapa).</summary>
        public static float Forest(float x, float z)
        {
            float noise = Fbm(x, z, ox4 + 900f, oz4 + 900f, 3, 0.02f);
            return Mathf.Clamp01(noise * 0.62f + IslandLayout.ForestBlob(x, z) * 0.9f);
        }

        // ---------- Punto de inicio ----------

        static bool GoodCamp(float x, float z, out Vector3 p)
        {
            p = Vector3.zero;
            float h = Height(x, z);
            if (h < 2.2f || h > 8f) return false;
            if (Slope(x, z) > 0.30f) return false;
            if (Coast(x, z) < 10f) return false;
            if (IslandLayout.Blocks(x, z, 8f)) return false;
            if (IslandLayout.RegionAt(x, z) != IslandRegion.Plains) return false;
            p = new Vector3(x, h, z);
            return true;
        }

        static Vector3 FindCamp()
        {
            // Playa del sur, junto a la torre: se sale hacia el interior en espiral.
            Vector2 start = IslandShape.FromPx(560f, 715f);
            Vector3 p;
            for (float r = 0f; r <= 80f; r += 5f)
            {
                int n = r < 1f ? 1 : 14;
                for (int k = 0; k < n; k++)
                {
                    float a = k * Mathf.PI * 2f / n;
                    if (GoodCamp(start.x + Mathf.Cos(a) * r, start.y + Mathf.Sin(a) * r, out p)) return p;
                }
            }
            // Plan B: cualquier punto llano cerca del pueblo.
            Vector2 alt = IslandLayout.Town.C;
            for (float r = 40f; r <= 160f; r += 8f)
                for (int k = 0; k < 16; k++)
                {
                    float a = k * Mathf.PI * 2f / 16f;
                    if (GoodCamp(alt.x + Mathf.Cos(a) * r, alt.y + Mathf.Sin(a) * r, out p)) return p;
                }
            return new Vector3(alt.x, Height(alt.x, alt.y), alt.y);
        }
    }
}
