using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Terreno procedural de la isla. Es una función pura de (x, z) y la semilla, así que
    /// cualquier chunk puede generarse en cualquier momento y siempre da el mismo resultado.
    /// El nivel del mar está en y = 0.
    /// </summary>
    public static class IslandTerrain
    {
        public const float Radius = 280f;
        public const float SeaLevel = 0f;
        public const float DeepWater = -0.9f;   // por debajo de esta altura no se puede caminar

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

        /// <summary>0 en mar abierto, 1 en el corazón de la isla. La costa es irregular.</summary>
        public static float Land(float x, float z)
        {
            float d = Mathf.Sqrt(x * x + z * z);
            float ang = Mathf.Atan2(z, x);
            float cx = Mathf.Cos(ang), cz = Mathf.Sin(ang);
            float n1 = Mathf.PerlinNoise(ox1 + cx * 1.7f + 10f, oz1 + cz * 1.7f + 10f);
            float n2 = Mathf.PerlinNoise(ox2 + cx * 4.5f + 10f, oz2 + cz * 4.5f + 10f);
            float rEff = Radius * (0.70f + 0.32f * n1 + 0.10f * n2);
            float t = Mathf.Clamp01((rEff - d) / (rEff * 0.55f));
            return t * t * (3f - 2f * t);
        }

        public static float Height(float x, float z)
        {
            float s = Land(x, z);
            float h = Mathf.Lerp(-5f, 7f, s);

            float hills = (Fbm(x, z, ox3, oz3, 4, 0.012f) - 0.5f) * 2f;
            h += hills * 6.5f * Smooth(0.40f, 0.85f, s);

            float d2 = x * x + z * z;
            h += 20f * Mathf.Exp(-d2 / (50f * 50f)) * s;

            float detail = Fbm(x, z, ox4, oz4, 2, 0.07f) - 0.5f;
            h += detail * 1.4f * Smooth(0.45f, 0.65f, s);
            return h;
        }

        public static float Slope(float x, float z)
        {
            float dx = Height(x + 1f, z) - Height(x - 1f, z);
            float dz = Height(x, z + 1f) - Height(x, z - 1f);
            return Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) * 0.5f;
        }

        /// <summary>Densidad de bosque 0..1 (manchas grandes).</summary>
        public static float Forest(float x, float z)
        {
            return Fbm(x, z, ox4 + 900f, oz4 + 900f, 3, 0.02f);
        }

        // ---------- Punto de inicio ----------

        static Vector3 FindCamp()
        {
            // Busca desde la costa hacia el interior en varias direcciones, empezando por el sur.
            for (int k = 0; k < 16; k++)
            {
                float ang = (-Mathf.PI * 0.5f) + k * (Mathf.PI * 2f / 16f);
                float cx = Mathf.Cos(ang), cz = Mathf.Sin(ang);
                for (float d = Radius * 1.1f; d > 30f; d -= 2f)
                {
                    float x = cx * d, z = cz * d;
                    if (Height(x, z) > 2.2f)
                    {
                        float dd = d - 22f;
                        float px = cx * dd, pz = cz * dd;
                        float ph = Height(px, pz);
                        if (ph > 2.5f && ph < 9f && Slope(px, pz) < 0.35f)
                            return new Vector3(px, ph, pz);
                        break;
                    }
                }
            }
            return new Vector3(0f, Height(0f, 0f), 0f);
        }
    }
}
