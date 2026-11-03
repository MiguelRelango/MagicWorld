using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Silueta de la isla sacada del mapa de referencia: costa y ríos como campos de distancia en metros.
    /// Las coordenadas del mapa son 0,5 m por píxel con el origen en el centro de la imagen
    /// (x = (px - 656) * 0,5; z = (404 - py) * 0,5).
    /// </summary>
    public static class IslandShape
    {
        public const float Cell = 3f;
        public const float X0 = -328f;   // borde izquierdo de la rejilla
        public const float Z0 = 202f;    // borde superior (norte) de la rejilla
        const float Far = -40f;

        static byte[] coast, rivers;

        /// <summary>Convierte un punto del mapa de referencia (píxeles) a coordenadas del mundo.</summary>
        public static Vector2 FromPx(float px, float py)
        {
            return new Vector2((px - 656f) * 0.5f, (404f - py) * 0.5f);
        }

        static byte[] Decode(string b64, int n)
        {
            byte[] result = new byte[n];
            try
            {
                byte[] z = Convert.FromBase64String(b64);
                using (MemoryStream ms = new MemoryStream(z))
                using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                {
                    int got = 0;
                    while (got < n)
                    {
                        int r = ds.Read(result, got, n - got);
                        if (r <= 0) break;
                        got += r;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("IslandShape: no se pudo leer la silueta de la isla: " + e.Message);
            }
            return result;
        }

        static void Load()
        {
            if (coast != null) return;
            int n = IslandShapeData.Width * IslandShapeData.Height;
            coast = Decode(IslandShapeData.Coast, n);
            rivers = Decode(IslandShapeData.Rivers, n);
        }

        static float Cellv(byte[] g, int i, int j)
        {
            int W = IslandShapeData.Width, H = IslandShapeData.Height;
            i = Mathf.Clamp(i, 0, W - 1);
            j = Mathf.Clamp(j, 0, H - 1);
            return (g[j * W + i] - 128f) / 127f * 40f;
        }

        static float Sample(byte[] g, float x, float z)
        {
            int W = IslandShapeData.Width, H = IslandShapeData.Height;
            float fx = (x - X0) / Cell - 0.5f;
            float fz = (Z0 - z) / Cell - 0.5f;
            // Fuera de la rejilla: el valor del borde, restando la distancia al borde (el mar crece hacia fuera).
            float outside = 0f;
            if (fx < 0f) outside = Mathf.Max(outside, -fx * Cell);
            if (fx > W - 1) outside = Mathf.Max(outside, (fx - (W - 1)) * Cell);
            if (fz < 0f) outside = Mathf.Max(outside, -fz * Cell);
            if (fz > H - 1) outside = Mathf.Max(outside, (fz - (H - 1)) * Cell);
            int i = Mathf.FloorToInt(fx), j = Mathf.FloorToInt(fz);
            float tx = fx - i, tz = fz - j;
            float a = Cellv(g, i, j), b = Cellv(g, i + 1, j), c = Cellv(g, i, j + 1), d = Cellv(g, i + 1, j + 1);
            float v = Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), tz);
            return v - outside;
        }

        /// <summary>Distancia con signo a la costa en metros: positiva en tierra, negativa en el mar.</summary>
        public static float Coast(float x, float z)
        {
            Load();
            return Sample(coast, x, z);
        }

        /// <summary>Distancia con signo al agua de los ríos y lagos interiores (positiva dentro del agua).</summary>
        public static float River(float x, float z)
        {
            Load();
            float v = Sample(rivers, x, z);
            return v < Far ? Far : v;
        }
    }
}
