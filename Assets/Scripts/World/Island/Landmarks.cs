using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Lugares grandes de la isla según el mapa de referencia: la ciudad amurallada, el puerto del desierto,
    /// el puerto pesquero, el árbol gigante, la fortaleza y la muralla norte, la torre del sur, las cabañas,
    /// los charcos de lava y la niebla del pantano.
    /// Cada chunk que carga construye solo las piezas cuyo centro cae dentro de él, así que las estructuras
    /// enormes se reparten entre chunks y nada se genera lejos del jugador.
    /// </summary>
    public static class Landmarks
    {
        static Transform T;
        static float X0, X1, Z0, Z1;

        static readonly Color Stone = new Color(0.62f, 0.60f, 0.58f);
        static readonly Color StoneDark = new Color(0.46f, 0.45f, 0.45f);
        static readonly Color Plaster = new Color(0.84f, 0.81f, 0.74f);
        static readonly Color Timber = new Color(0.48f, 0.34f, 0.22f);
        static readonly Color Planks = new Color(0.62f, 0.46f, 0.30f);
        static readonly Color Sandstone = new Color(0.88f, 0.76f, 0.54f);
        static readonly Color Cream = new Color(0.93f, 0.84f, 0.64f);

        static readonly Color[] CityRoofs =
        {
            new Color(0.22f, 0.43f, 0.46f), new Color(0.30f, 0.38f, 0.50f), new Color(0.55f, 0.30f, 0.24f), new Color(0.26f, 0.48f, 0.40f)
        };
        static readonly Color[] DesertRoofs =
        {
            new Color(0.80f, 0.42f, 0.22f), new Color(0.72f, 0.30f, 0.20f), new Color(0.86f, 0.55f, 0.28f)
        };
        static readonly Color[] FishRoofs =
        {
            new Color(0.28f, 0.42f, 0.62f), new Color(0.55f, 0.32f, 0.26f), new Color(0.32f, 0.50f, 0.52f)
        };
        static readonly Color[] CabinRoofs =
        {
            new Color(0.62f, 0.30f, 0.24f), new Color(0.36f, 0.50f, 0.34f), new Color(0.38f, 0.44f, 0.64f), new Color(0.50f, 0.36f, 0.24f)
        };

        static Material mist;

        // ---------- Utilidades ----------

        static int Hash(int a, int b)
        {
            unchecked { return IslandTerrain.Seed * 73856093 ^ a * 19349663 ^ b * 83492791; }
        }

        static float Rand01(int a, int b)
        {
            return (Mathf.Abs(Hash(a, b)) % 10000) / 10000f;
        }

        static bool In(float x, float z)
        {
            return x >= X0 && x < X1 && z >= Z0 && z < Z1;
        }

        static bool Overlaps(Vector2 c, float rx, float rz)
        {
            return c.x + rx > X0 && c.x - rx < X1 && c.y + rz > Z0 && c.y - rz < Z1;
        }

        static float H(float x, float z)
        {
            return IslandTerrain.Height(x, z);
        }

        static GameObject Mk(PrimitiveType type, Vector3 c, Vector3 size, Color col, bool collider, float yaw, string name, bool glow = false)
        {
            if (!In(c.x, c.z)) return null;
            GameObject g = Prim.Make(type, T, c, size, col, collider, name, glow);
            if (yaw != 0f) g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return g;
        }

        static GameObject Box(Vector3 c, Vector3 size, Color col, bool collider = true, float yaw = 0f, string name = "Piece")
        {
            return Mk(PrimitiveType.Cube, c, size, col, collider, yaw, name);
        }

        static GameObject Cyl(Vector3 c, Vector3 size, Color col, bool collider = true, string name = "Piece")
        {
            return Mk(PrimitiveType.Cylinder, c, size, col, collider, 0f, name);
        }

        static GameObject Ball(Vector3 c, Vector3 size, Color col, bool collider = false, string name = "Piece")
        {
            return Mk(PrimitiveType.Sphere, c, size, col, collider, 0f, name);
        }

        static GameObject Glow(Vector3 c, Vector3 size, Color col, PrimitiveType type = PrimitiveType.Sphere, float yaw = 0f)
        {
            return Mk(type, c, size, col, false, yaw, "Glow", true);
        }

        static void Masonry(GameObject g, float tile = 3f)
        {
            if (g != null) WorldLook.Surface(g, "Masonry", tile);
        }

        /// <summary>Muro recto de piedra entre dos puntos, con un hueco opcional para una puerta en el centro.</summary>
        static void WallLine(Vector2 a, Vector2 b, float h, float thick, Color col, float gap, bool crenel)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 0.1f) return;
            Vector2 u = d / len;
            float yaw = Mathf.Atan2(u.x, u.y) * Mathf.Rad2Deg;
            int n = Mathf.Max(1, Mathf.CeilToInt(len / 6f));
            float seg = len / n;
            for (int i = 0; i < n; i++)
            {
                float t = (i + 0.5f) * seg;
                if (gap > 0f && Mathf.Abs(t - len * 0.5f) < gap * 0.5f + seg * 0.5f - 0.01f) continue;
                Vector2 c = a + u * t;
                if (!In(c.x, c.y)) continue;
                if (IslandShape.River(c.x, c.y) > -2f) continue;   // los ríos pasan bajo la muralla
                float y = H(c.x, c.y);
                GameObject w = Box(new Vector3(c.x, y + h * 0.5f - 0.8f, c.y), new Vector3(thick, h + 0.8f, seg + 0.25f), col, true, yaw, "Wall");
                Masonry(w);
                if (crenel)
                {
                    Box(new Vector3(c.x, y + h + 0.25f, c.y), new Vector3(thick + 0.5f, 0.5f, seg + 0.25f), StoneDark, false, yaw, "Parapet");
                    for (int m = 0; m < 3; m++)
                    {
                        Vector2 mp = c + u * ((m - 1) * seg * 0.33f);
                        Box(new Vector3(mp.x, y + h + 0.9f, mp.y), new Vector3(thick + 0.5f, 0.9f, 0.9f), StoneDark, false, yaw, "Merlon");
                    }
                }
            }
        }

        static void Tower(Vector2 p, float r, float height, Color wall, Color roof)
        {
            if (!In(p.x, p.y)) return;
            float y = H(p.x, p.y);
            GameObject t = Cyl(new Vector3(p.x, y + height * 0.5f - 0.8f, p.y), new Vector3(r * 2f, height * 0.5f + 0.4f, r * 2f), wall, true, "Tower");
            Masonry(t);
            Cyl(new Vector3(p.x, y + height - 0.3f, p.y), new Vector3(r * 2.3f, 0.3f, r * 2.3f), StoneDark, false, "TowerRim");
            Ball(new Vector3(p.x, y + height + 0.9f, p.y), new Vector3(r * 2.2f, r * 1.5f, r * 2.2f), roof, false, "TowerRoof");
        }

        static void Gate(Vector2 mid, Vector2 dir, float gap, float h)
        {
            // Dos pilares y un dintel a cada lado del hueco.
            Vector2 u = dir.normalized;
            float yaw = Mathf.Atan2(u.x, u.y) * Mathf.Rad2Deg;
            for (int s = -1; s <= 1; s += 2)
            {
                Vector2 p = mid + u * (s * (gap * 0.5f + 0.9f));
                if (!In(p.x, p.y)) continue;
                float y = H(p.x, p.y);
                Masonry(Box(new Vector3(p.x, y + (h + 3f) * 0.5f - 0.8f, p.y), new Vector3(3.2f, h + 3.8f, 3.2f), Stone, true, yaw, "GatePillar"));
            }
            if (In(mid.x, mid.y))
            {
                float y = H(mid.x, mid.y);
                Masonry(Box(new Vector3(mid.x, y + h + 1.8f, mid.y), new Vector3(2.6f, 1.6f, gap + 5.6f), Stone, false, yaw, "GateLintel"));
            }
        }

        static void Lamp(float x, float z, float h = 3.0f)
        {
            if (!In(x, z)) return;
            float y = H(x, z);
            Cyl(new Vector3(x, y + h * 0.5f, z), new Vector3(0.18f, h * 0.5f, 0.18f), new Color(0.28f, 0.22f, 0.18f), false, "LampPost");
            Glow(new Vector3(x, y + h + 0.2f, z), new Vector3(0.5f, 0.6f, 0.5f), new Color(1f, 0.82f, 0.45f));
        }

        /// <summary>Casa pequeña con la puerta en el lado -z local.</summary>
        static void House(float x, float z, float yaw, Color wall, Color roof, float w = 7f, float d = 6f, float hh = 3f)
        {
            if (!In(x, z)) return;
            GameObject g = new GameObject("House");
            g.transform.SetParent(T, false);
            g.transform.position = new Vector3(x, H(x, z), z);
            g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Transform t = g.transform;
            Prim.Box(t, new Vector3(0f, -0.9f, 0f), new Vector3(w + 0.4f, 2f, d + 0.4f), StoneDark, true, "Foundation");
            Prim.Box(t, new Vector3(0f, hh * 0.5f, 0f), new Vector3(w, hh, d), wall, true, "Walls");
            float rd = d * 0.64f;
            GameObject r1 = Prim.Box(t, new Vector3(0f, hh + 0.7f, -d * 0.265f), new Vector3(w + 0.8f, 0.3f, rd), roof, false, "RoofA");
            r1.transform.localRotation = Quaternion.Euler(-32f, 0f, 0f);
            GameObject r2 = Prim.Box(t, new Vector3(0f, hh + 0.7f, d * 0.265f), new Vector3(w + 0.8f, 0.3f, rd), roof, false, "RoofB");
            r2.transform.localRotation = Quaternion.Euler(32f, 0f, 0f);
            Prim.Box(t, new Vector3(0f, 0.9f, -d * 0.5f - 0.03f), new Vector3(1.3f, 1.8f, 0.1f), Timber, false, "Door");
            Prim.Make(PrimitiveType.Cube, t, new Vector3(w * 0.3f, hh * 0.57f, -d * 0.5f - 0.03f), new Vector3(1.0f, 0.9f, 0.1f), new Color(1f, 0.92f, 0.60f), false, "Window", true);
        }

        static void Crate(float x, float z, float s = 0.9f)
        {
            if (!In(x, z)) return;
            Box(new Vector3(x, H(x, z) + s * 0.5f, z), new Vector3(s, s, s), Planks, true, 0f, "Crate");
        }

        static void Barrel(float x, float z)
        {
            if (!In(x, z)) return;
            Cyl(new Vector3(x, H(x, z) + 0.5f, z), new Vector3(0.8f, 0.5f, 0.8f), Timber, true, "Barrel");
        }

        static void Citizen(Vector3 p, string name, Color outfit, Color hair, bool beard, float yaw, string[] lines)
        {
            if (!In(p.x, p.z)) return;
            p.y = H(p.x, p.z) + 0.08f;
            GameObject npc = Npc.Create(T, p, name, outfit, hair, beard, yaw);
            Interactable it = npc.AddComponent<Interactable>();
            it.Prompt = "Hablar con " + name.ToLower();
            it.Range = 3.0f;
            int n = 0;
            it.OnUse = () => { Game.Dialog(name, lines[n % lines.Length], 6f); n++; };
        }

        static void Boat(float x, float y, float z, float yaw, Color hull)
        {
            if (!In(x, z)) return;
            GameObject g = new GameObject("Boat");
            g.transform.SetParent(T, false);
            g.transform.position = new Vector3(x, y, z);
            g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Transform t = g.transform;
            Prim.Box(t, new Vector3(0f, 0.2f, 0f), new Vector3(2.6f, 1.1f, 6.6f), hull, true, "Hull");
            Prim.Box(t, new Vector3(0f, 0.8f, 0.2f), new Vector3(2.0f, 0.2f, 5.6f), Timber, false, "Deck");
            Prim.Box(t, new Vector3(0f, 0.4f, 3.6f), new Vector3(1.6f, 0.9f, 1.4f), hull, false, "Bow");
            Prim.Cyl(t, new Vector3(0f, 2.6f, -0.4f), new Vector3(0.16f, 2.2f, 0.16f), Timber, false, "Mast");
            Prim.Box(t, new Vector3(0f, 3.0f, -0.4f), new Vector3(0.06f, 2.8f, 2.4f), new Color(0.95f, 0.92f, 0.82f), false, "Sail");
        }

        static void Pier(Vector2 a, Vector2 b, float y, float width)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            Vector2 u = d / len;
            float yaw = Mathf.Atan2(u.x, u.y) * Mathf.Rad2Deg;
            int n = Mathf.CeilToInt(len / 5f);
            float seg = len / n;
            for (int i = 0; i < n; i++)
            {
                Vector2 c = a + u * ((i + 0.5f) * seg);
                if (!In(c.x, c.y)) continue;
                Box(new Vector3(c.x, y, c.y), new Vector3(width, 0.3f, seg + 0.1f), Planks, true, yaw, "Planks");
                Vector2 side = new Vector2(u.y, -u.x);
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector2 p = c + side * (s * (width * 0.5f - 0.2f));
                    Cyl(new Vector3(p.x, y - 1.2f, p.y), new Vector3(0.4f, 1.6f, 0.4f), Timber, false, "Post");
                }
            }
        }

        // ---------- Entrada ----------

        public static void Populate(Transform chunk, int cx, int cz)
        {
            T = chunk;
            int N = IslandWorld.ChunkSize;
            X0 = cx * N; X1 = X0 + N; Z0 = cz * N; Z1 = Z0 + N;

            if (Overlaps(IslandLayout.City.C, IslandLayout.City.Rx + 6f, IslandLayout.City.Rz + 6f)) City();
            if (Overlaps(IslandLayout.Desert.C, IslandLayout.Desert.Rx + 6f, IslandLayout.Desert.Rz + 6f)) DesertTown();
            if (Overlaps(IslandLayout.Fishing.C, IslandLayout.Fishing.Rx + 30f, IslandLayout.Fishing.Rz + 6f)) FishingPort();
            if (Overlaps(IslandLayout.GiantTree.C, 70f, 70f)) GiantTree();
            if (Overlaps(IslandLayout.Keep.C, 100f, 30f)) KeepAndWall();
            if (Overlaps(IslandLayout.Tower.C, 10f, 10f)) SouthTower();
            Cabins();
            LavaPools();
            Mist();
        }

        // ---------- Ciudad amurallada ----------

        static Vector2[] Octagon(Site s, float k)
        {
            Vector2[] v = new Vector2[8];
            for (int i = 0; i < 8; i++)
            {
                float a = (22.5f + 45f * i) * Mathf.Deg2Rad;
                v[i] = new Vector2(s.C.x + Mathf.Cos(a) * s.Rx * k, s.C.y + Mathf.Sin(a) * s.Rz * k);
            }
            return v;
        }

        static void WalledRing(Site s, float wallH, Color wall, Color roof, int[] gates)
        {
            Vector2[] v = Octagon(s, 0.95f);
            for (int i = 0; i < 8; i++)
            {
                Vector2 a = v[i], b = v[(i + 1) % 8];
                bool gate = System.Array.IndexOf(gates, i) >= 0;
                WallLine(a, b, wallH, 2.2f, wall, gate ? 9f : 0f, true);
                if (gate) Gate((a + b) * 0.5f, (b - a), 9f, wallH);
                Tower(a, 2.8f, wallH + 3.5f, wall, roof);
            }
        }

        static void City()
        {
            Site s = IslandLayout.City;
            WalledRing(s, 6.5f, Stone, CityRoofs[0], new[] { 3, 5 });

            // Casas en cuadrícula con dos calles principales y una plaza central.
            float step = 13f;
            for (int gx = -4; gx <= 4; gx++)
            {
                for (int gz = -3; gz <= 3; gz++)
                {
                    if (gx == 0 || gz == 0) continue;
                    if (Mathf.Abs(gx) <= 1 && Mathf.Abs(gz) <= 1) continue;
                    float jx = (Rand01(gx, gz) - 0.5f) * 2.4f, jz = (Rand01(gz, gx + 91) - 0.5f) * 2.0f;
                    float x = s.C.x + gx * step + jx, z = s.C.y + gz * step * 0.95f + jz;
                    if (IslandLayout.Norm(s, x, z) > 0.80f) continue;
                    if (gx >= 3 && gz >= 2) continue;   // sitio de la fortaleza
                    int v = Mathf.Abs(Hash(gx, gz)) % 4;
                    House(x, z, gz > 0 ? 0f : 180f, v == 1 ? Cream : Plaster, CityRoofs[v], 7.5f, 6.5f, 3.2f + (v == 2 ? 1.6f : 0f));
                }
            }

            // Plaza con fuente y farolas.
            Vector3 c = new Vector3(s.C.x, H(s.C.x, s.C.y), s.C.y);
            if (In(c.x, c.z))
            {
                Cyl(c + new Vector3(0f, 0.3f, 0f), new Vector3(7f, 0.45f, 7f), Stone, true, "FountainBase");
                Cyl(c + new Vector3(0f, 0.7f, 0f), new Vector3(5.8f, 0.2f, 5.8f), new Color(0.35f, 0.62f, 0.85f), false, "FountainWater");
                Cyl(c + new Vector3(0f, 1.6f, 0f), new Vector3(0.9f, 1.2f, 0.9f), Stone, true, "FountainColumn");
                Ball(c + new Vector3(0f, 3.1f, 0f), new Vector3(1.4f, 0.8f, 1.4f), new Color(0.40f, 0.68f, 0.88f), false, "FountainSpray");
            }
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f + 0.3f;
                Lamp(s.C.x + Mathf.Cos(a) * 11f, s.C.y + Mathf.Sin(a) * 11f);
            }
            for (int i = -3; i <= 3; i += 2)
            {
                Lamp(s.C.x + i * 13f, s.C.y - 2.6f);
                Lamp(s.C.x - 2.6f, s.C.y + i * 12f);
            }

            // Puestos de mercado alrededor de la fuente.
            Color[] awn = { new Color(0.78f, 0.25f, 0.22f), new Color(0.25f, 0.45f, 0.75f), new Color(0.90f, 0.72f, 0.25f), new Color(0.30f, 0.60f, 0.40f) };
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                Stall(s.C.x + Mathf.Cos(a) * 16f, s.C.y + Mathf.Sin(a) * 13f, awn[i], i * 90f);
            }

            // Palacio al noreste de la ciudad.
            Palace(new Vector2(s.C.x + 38f, s.C.y + 24f));

            Citizen(new Vector3(s.C.x + 6f, 0f, s.C.y - 8f), "Mercader", new Color(0.55f, 0.30f, 0.50f), new Color(0.20f, 0.14f, 0.10f), true, 200f,
                new[] { "Telas de la costa del desierto, especias del puerto... lo que busques, aquí lo hay.", "Dicen que en las montañas de lava hay cuevas llenas de goblins. Yo prefiero vender." });
            Citizen(new Vector3(s.C.x - 9f, 0f, s.C.y + 6f), "Guardia de la ciudad", new Color(0.30f, 0.38f, 0.55f), new Color(0.35f, 0.30f, 0.25f), false, 90f,
                new[] { "Alto. Ah, eres de fuera. Bienvenido a la ciudad amurallada.", "Si buscas misiones, el gremio está en el pueblo del centro de la isla." });
            Citizen(new Vector3(s.C.x + 2f, 0f, s.C.y + 12f), "Niña", new Color(0.85f, 0.55f, 0.60f), new Color(0.55f, 0.35f, 0.20f), false, 180f,
                new[] { "¡Mira cómo brilla la fuente! Mi abuelo dice que antes era de oro." });
        }

        static void Stall(float x, float z, Color awning, float yaw)
        {
            if (!In(x, z)) return;
            float y = H(x, z);
            Box(new Vector3(x, y + 0.5f, z), new Vector3(3.2f, 1.0f, 1.2f), Planks, true, yaw, "Counter");
            for (int i = -1; i <= 1; i += 2)
                Cyl(new Vector3(x + i * 1.5f * Mathf.Cos(yaw * Mathf.Deg2Rad), y + 1.3f, z - i * 1.5f * Mathf.Sin(yaw * Mathf.Deg2Rad)), new Vector3(0.14f, 1.3f, 0.14f), Timber, false, "StallPost");
            Box(new Vector3(x, y + 2.7f, z), new Vector3(3.8f, 0.15f, 2.2f), awning, false, yaw, "Awning");
        }

        static void Palace(Vector2 p)
        {
            float y = H(p.x, p.y);
            if (!In(p.x, p.y)) return;
            Masonry(Box(new Vector3(p.x, y - 0.5f, p.y), new Vector3(20f, 2f, 16f), StoneDark, true, 0f, "PalaceBase"));
            Masonry(Box(new Vector3(p.x, y + 4.5f, p.y), new Vector3(18f, 9f, 14f), Plaster, true, 0f, "PalaceHall"));
            Box(new Vector3(p.x, y + 9.3f, p.y), new Vector3(19f, 0.6f, 15f), CityRoofs[1], false, 0f, "PalaceRoof");
            Box(new Vector3(p.x, y + 1.6f, p.y - 7.05f), new Vector3(3.4f, 3.2f, 0.4f), Timber, false, 0f, "PalaceDoor");
            Glow(new Vector3(p.x, y + 5.8f, p.y - 7.1f), new Vector3(7f, 1.4f, 0.2f), new Color(1f, 0.9f, 0.6f), PrimitiveType.Cube);
            Color[] ban = { new Color(0.70f, 0.18f, 0.20f), new Color(0.20f, 0.35f, 0.70f) };
            for (int i = 0; i < 2; i++)
                Box(new Vector3(p.x + (i == 0 ? -4.5f : 4.5f), y + 5f, p.y - 7.2f), new Vector3(1.2f, 3.4f, 0.08f), ban[i], false, 0f, "Banner");
        }

        // ---------- Puerto del desierto ----------

        static void DesertTown()
        {
            Site s = IslandLayout.Desert;
            WalledRing(s, 7f, Sandstone, DesertRoofs[0], new[] { 3, 1 });

            float step = 11.5f;
            for (int gx = -3; gx <= 3; gx++)
            {
                for (int gz = -3; gz <= 3; gz++)
                {
                    if (gx == 0 || gz == 0) continue;
                    float jx = (Rand01(gx + 40, gz) - 0.5f) * 2.2f, jz = (Rand01(gz + 40, gx + 7) - 0.5f) * 2.0f;
                    float x = s.C.x + gx * step + jx, z = s.C.y + gz * step + jz;
                    if (IslandLayout.Norm(s, x, z) > 0.78f) continue;
                    if (gx >= 1 && gz <= 1) continue;   // el muelle ocupa el sureste
                    int v = Mathf.Abs(Hash(gx + 40, gz)) % 3;
                    House(x, z, gz > 0 ? 0f : 180f, v == 1 ? Sandstone : Cream, DesertRoofs[v], 7f, 6f, 3.0f);
                }
            }

            // Muelle y barcos en el sureste.
            Vector2 d0 = new Vector2(s.C.x + 8f, s.C.y - 4f);
            float py = H(d0.x, d0.y) + 0.7f;
            Pier(new Vector2(s.C.x + 6f, s.C.y - 6f), new Vector2(s.C.x + 38f, s.C.y - 6f), py, 3.4f);
            Pier(new Vector2(s.C.x + 6f, s.C.y - 20f), new Vector2(s.C.x + 38f, s.C.y - 20f), py, 3.4f);
            Pier(new Vector2(s.C.x + 22f, s.C.y - 6f), new Vector2(s.C.x + 22f, s.C.y - 20f), py, 3.0f);
            Boat(s.C.x + 14f, py + 0.2f, s.C.y - 13f, 90f, new Color(0.55f, 0.36f, 0.24f));
            Boat(s.C.x + 30f, py + 0.2f, s.C.y - 13f, 90f, new Color(0.30f, 0.45f, 0.58f));
            Crate(s.C.x + 9f, s.C.y - 8.5f); Crate(s.C.x + 10.2f, s.C.y - 8.7f, 0.7f); Barrel(s.C.x + 11f, s.C.y - 3f);

            // Mercado y palmeras.
            Color[] awn = { new Color(0.80f, 0.35f, 0.22f), new Color(0.90f, 0.72f, 0.30f), new Color(0.25f, 0.55f, 0.55f) };
            for (int i = 0; i < 3; i++) Stall(s.C.x - 12f + i * 6f, s.C.y + 2.6f, awn[i], 0f);
            for (int i = 0; i < 7; i++)
            {
                float a = i * 0.9f + 0.4f;
                Palm(s.C.x + Mathf.Cos(a) * (14f + (i % 3) * 7f), s.C.y + Mathf.Sin(a) * (12f + (i % 3) * 6f));
            }
            for (int i = -2; i <= 2; i += 2) { Lamp(s.C.x + i * 11f, s.C.y - 2.5f); Lamp(s.C.x - 2.5f, s.C.y + i * 11f); }

            Citizen(new Vector3(s.C.x - 3f, 0f, s.C.y + 6f), "Comerciante del desierto", new Color(0.85f, 0.60f, 0.25f), new Color(0.15f, 0.10f, 0.08f), true, 160f,
                new[] { "Aquí la arena entra hasta en el té. Pero el puerto nunca duerme.", "Las murallas nos protegen del viento y de cosas peores." });
            Citizen(new Vector3(s.C.x + 14f, 0f, s.C.y - 10f), "Marinero", new Color(0.30f, 0.45f, 0.60f), new Color(0.40f, 0.30f, 0.20f), true, 90f,
                new[] { "Mis barcos van y vienen. Nadie cruza el mar profundo a pie, ¡ni lo intentes!" });
        }

        static void Palm(float x, float z)
        {
            if (!In(x, z)) return;
            float y = H(x, z);
            GameObject trunk = Cyl(new Vector3(x, y + 2.4f, z), new Vector3(0.45f, 2.4f, 0.45f), new Color(0.55f, 0.40f, 0.26f), true, "PalmTrunk");
            if (trunk != null) trunk.transform.rotation = Quaternion.Euler(6f, x * 10f, 5f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f;
                GameObject f = Box(new Vector3(x, y + 5.0f, z), new Vector3(0.5f, 0.1f, 2.8f), new Color(0.30f, 0.55f, 0.25f), false, a, "Frond");
                if (f != null)
                {
                    f.transform.rotation = Quaternion.Euler(-18f, a, 0f);
                    f.transform.position = new Vector3(x, y + 5.0f, z) + f.transform.forward * 1.3f;
                }
            }
        }

        // ---------- Puerto pesquero ----------

        static void FishingPort()
        {
            Site s = IslandLayout.Fishing;
            float y = H(s.C.x, s.C.y);
            float deck = 0.9f;

            // Dos muelles hacia el mar (este) y un paseo de tablas por la orilla.
            Pier(new Vector2(s.C.x + 8f, s.C.y + 8f), new Vector2(s.C.x + 38f, s.C.y + 8f), deck, 3.2f);
            Pier(new Vector2(s.C.x + 8f, s.C.y - 8f), new Vector2(s.C.x + 38f, s.C.y - 8f), deck, 3.2f);
            Pier(new Vector2(s.C.x + 8f, s.C.y - 14f), new Vector2(s.C.x + 8f, s.C.y + 14f), deck, 3.4f);
            Boat(s.C.x + 20f, -0.1f, s.C.y + 14f, 90f, new Color(0.30f, 0.45f, 0.60f));
            Boat(s.C.x + 30f, -0.1f, s.C.y + 0f, 90f, new Color(0.62f, 0.32f, 0.26f));
            Boat(s.C.x + 18f, -0.1f, s.C.y - 14f, 90f, new Color(0.88f, 0.80f, 0.62f));

            // Casas de pescadores y cobertizo.
            Vector2[] spots = { new Vector2(-18f, 14f), new Vector2(-18f, -2f), new Vector2(-6f, 20f), new Vector2(-8f, -18f), new Vector2(-20f, -20f) };
            for (int i = 0; i < spots.Length; i++)
                House(s.C.x + spots[i].x, s.C.y + spots[i].y, 90f + (i % 2) * 0f, Planks, FishRoofs[i % 3], 6.5f, 5.5f, 2.9f);
            // Secaderos de pescado y redes.
            for (int i = 0; i < 3; i++)
            {
                float x = s.C.x - 4f + i * 3.4f, z = s.C.y + 4f;
                if (!In(x, z)) continue;
                float h = H(x, z);
                Cyl(new Vector3(x, h + 1.2f, z), new Vector3(0.14f, 1.2f, 0.14f), Timber, false, "RackPost");
                Box(new Vector3(x, h + 2.1f, z + 0.9f), new Vector3(0.12f, 0.12f, 2.0f), Timber, false, 0f, "RackBar");
                Glow(new Vector3(x, h + 1.6f, z + 0.9f), new Vector3(0.2f, 0.5f, 0.1f), new Color(0.80f, 0.85f, 0.88f), PrimitiveType.Cube);
            }
            Crate(s.C.x + 10f, s.C.y + 10.5f); Crate(s.C.x + 11f, s.C.y + 10.4f, 0.7f); Barrel(s.C.x + 10.5f, s.C.y - 9.6f); Barrel(s.C.x + 11.8f, s.C.y - 9.5f);
            Lamp(s.C.x + 8f, s.C.y + 14f, 3.4f); Lamp(s.C.x + 8f, s.C.y - 14f, 3.4f); Lamp(s.C.x + 36f, s.C.y + 8f, 3.4f); Lamp(s.C.x + 36f, s.C.y - 8f, 3.4f);

            Citizen(new Vector3(s.C.x + 14f, 0f, s.C.y + 6f), "Pescador", new Color(0.35f, 0.52f, 0.62f), new Color(0.50f, 0.50f, 0.52f), true, 90f,
                new[] { "Hoy el mar está tranquilo. Pican las lubinas junto al muelle.", "Mi hijo se fue a la ciudad amurallada. Dice que allí hay más gente, pero menos pescado." });
        }

        // ---------- Árbol gigante ----------

        static void GiantTree()
        {
            Site s = IslandLayout.GiantTree;
            Vector2 c = s.C;
            float baseY = s.H;
            Color bark = new Color(0.36f, 0.26f, 0.18f);
            Color leaf = new Color(0.20f, 0.42f, 0.20f);
            const float trunkR = 11f, trunkH = 64f;

            // Tronco (se construye en el chunk del centro) y raíces.
            if (In(c.x, c.y))
            {
                GameObject trunk = Prim.Make(PrimitiveType.Cylinder, T, new Vector3(c.x, baseY + trunkH * 0.5f - 1f, c.y), new Vector3(trunkR * 2f, trunkH * 0.5f + 1f, trunkR * 2f), bark, true, "GiantTrunk");
                trunk.GetComponent<Renderer>().sharedMaterial = ForestLook.Surface("Bark");
                Cyl(new Vector3(c.x, baseY + 1f, c.y), new Vector3(trunkR * 2.5f, 1.5f, trunkR * 2.5f), bark, true, "TrunkFoot");
            }
            for (int i = 0; i < 9; i++)
            {
                float a = i * 40f + 12f;
                Vector3 dir = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad), 0f, Mathf.Cos(a * Mathf.Deg2Rad));
                Vector3 p = new Vector3(c.x, baseY + 0.8f, c.y) + dir * 14f;
                GameObject r = Box(p, new Vector3(2.8f, 2.4f, 13f), bark, false, a, "Root");
                if (r != null) r.transform.rotation = Quaternion.Euler(-10f, a, 0f);
            }

            // Copa: grandes masas de hojas con racimos de hojas encima.
            for (int i = 0; i < 46; i++)
            {
                float a = i * 2.399963f;
                float rr = 5f + Mathf.Sqrt((i + 1f) / 46f) * 52f;
                float x = c.x + Mathf.Cos(a) * rr, z = c.y + Mathf.Sin(a) * rr;
                if (!In(x, z)) continue;
                float yy = baseY + 42f + Mathf.Sin(i * 1.7f) * 5f + (1f - rr / 57f) * 12f;
                float sc = 18f + Rand01(i, 3) * 10f;
                Ball(new Vector3(x, yy, z), new Vector3(sc, sc * 0.55f, sc), Color.Lerp(leaf, new Color(0.28f, 0.52f, 0.24f), Rand01(i, 5)), false, "GiantCanopy");
                GameObject lv = new GameObject("GiantLeaves");
                lv.transform.SetParent(T, false);
                lv.transform.position = new Vector3(x, yy + sc * 0.12f, z);
                lv.transform.rotation = Quaternion.Euler(0f, i * 47f, 0f);
                ForestLook.Canopy(lv.transform, sc * 0.45f);
            }

            // Escalera de caracol alrededor del tronco hasta un mirador con cofre.
            const int steps = 56;
            const float rise = 0.25f, rad = 16.5f;
            for (int i = 0; i < steps; i++)
            {
                float ang = i * 10.7f * Mathf.Deg2Rad;
                float x = c.x + Mathf.Cos(ang) * rad, z = c.y + Mathf.Sin(ang) * rad;
                if (!In(x, z)) continue;
                GameObject st = Box(new Vector3(x, baseY + 0.12f + i * rise, z), new Vector3(3.4f, 0.3f, 3.7f), Planks, true, -ang * Mathf.Rad2Deg, "Step");
                if (st != null && i % 4 == 0)
                    Cyl(new Vector3(x, baseY + i * rise * 0.5f, z), new Vector3(0.3f, i * rise * 0.5f + 0.2f, 0.3f), Timber, false, "StepPost");
            }
            float topY = baseY + 0.12f + (steps - 1) * rise;
            for (int i = 0; i < 18; i++)
            {
                float ang = i * 20f * Mathf.Deg2Rad;
                float x = c.x + Mathf.Cos(ang) * rad, z = c.y + Mathf.Sin(ang) * rad;
                if (!In(x, z)) continue;
                Box(new Vector3(x, topY + 0.25f, z), new Vector3(4.2f, 0.3f, 6.2f), Planks, true, -ang * Mathf.Rad2Deg, "Lookout");
                Cyl(new Vector3(x, topY + 1.0f, z), new Vector3(0.16f, 0.8f, 0.16f), Timber, false, "Rail");
            }
            string key = "giant";
            if (!IslandWorld.IsConsumed(key))
            {
                float ang = 200f * Mathf.Deg2Rad;
                Vector3 cp = new Vector3(c.x + Mathf.Cos(ang) * rad, topY + 0.55f, c.y + Mathf.Sin(ang) * rad);
                if (In(cp.x, cp.z))
                {
                    Chest chest = Chest.Create(T, cp, -ang * Mathf.Rad2Deg + 90f);
                    chest.OnOpened = () =>
                    {
                        IslandWorld.Consume(key);
                        Sfx.Play("pickup");
                        Game.Gems += 25;
                        Game.Arrows = Mathf.Min(30, Game.Arrows + 15);
                        Game.Say("Desde lo alto del árbol gigante se ve toda la isla. Cofre: +25 gemas, +15 flechas", 5f);
                    };
                }
            }

            Citizen(new Vector3(c.x - 14f, 0f, c.y - 18f), "Anciana del árbol", new Color(0.45f, 0.55f, 0.38f), new Color(0.82f, 0.82f, 0.85f), false, 30f,
                new[] { "Este árbol ya era viejo cuando llegaron los primeros barcos.", "Sube por la escalera: desde la cima se ve la isla entera, hasta el humo de las montañas de lava." });
        }

        // ---------- Fortaleza y muralla norte ----------

        static void KeepAndWall()
        {
            Vector2 a = IslandShape.FromPx(265f, 190f), b = IslandShape.FromPx(545f, 190f);
            WallLine(a, b, 6f, 2.4f, Stone, 0f, true);
            for (int i = 0; i <= 3; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / 3f);
                Tower(p, 2.6f, 9.5f, Stone, CityRoofs[1]);
            }

            Site s = IslandLayout.Keep;
            Vector2 p0 = new Vector2(s.C.x, s.C.y + 2f);
            if (In(p0.x, p0.y))
            {
                float y = s.H;
                float W = 24f, D = 12f, Hh = 9f;
                Masonry(Box(new Vector3(p0.x - W * 0.5f + 0.5f, y + Hh * 0.5f, p0.y), new Vector3(1f, Hh, D), Stone, true, 0f, "KeepWallW"));
                Masonry(Box(new Vector3(p0.x + W * 0.5f - 0.5f, y + Hh * 0.5f, p0.y), new Vector3(1f, Hh, D), Stone, true, 0f, "KeepWallE"));
                Masonry(Box(new Vector3(p0.x, y + Hh * 0.5f, p0.y + D * 0.5f - 0.5f), new Vector3(W, Hh, 1f), Stone, true, 0f, "KeepWallN"));
                Masonry(Box(new Vector3(p0.x - W * 0.25f - 1.5f, y + Hh * 0.5f, p0.y - D * 0.5f + 0.5f), new Vector3(W * 0.5f - 3f, Hh, 1f), Stone, true, 0f, "KeepFrontL"));
                Masonry(Box(new Vector3(p0.x + W * 0.25f + 1.5f, y + Hh * 0.5f, p0.y - D * 0.5f + 0.5f), new Vector3(W * 0.5f - 3f, Hh, 1f), Stone, true, 0f, "KeepFrontR"));
                Masonry(Box(new Vector3(p0.x, y + Hh - 1.0f, p0.y - D * 0.5f + 0.5f), new Vector3(6f, 2.2f, 1f), Stone, false, 0f, "KeepLintel"));
                Box(new Vector3(p0.x, y + Hh + 0.3f, p0.y), new Vector3(W + 1f, 0.6f, D + 1f), StoneDark, false, 0f, "KeepRoof");
                Box(new Vector3(p0.x, y + 0.06f, p0.y), new Vector3(W - 1f, 0.1f, D - 1f), new Color(0.50f, 0.44f, 0.40f), false, 0f, "KeepFloor");
                Box(new Vector3(p0.x, y + 0.12f, p0.y - 1f), new Vector3(3.6f, 0.04f, 9f), new Color(0.65f, 0.20f, 0.22f), false, 0f, "KeepRug");
                Glow(new Vector3(p0.x - 6f, y + 6.5f, p0.y - D * 0.5f - 0.1f), new Vector3(1.2f, 1.6f, 0.2f), new Color(1f, 0.9f, 0.6f), PrimitiveType.Cube);
                Glow(new Vector3(p0.x + 6f, y + 6.5f, p0.y - D * 0.5f - 0.1f), new Vector3(1.2f, 1.6f, 0.2f), new Color(1f, 0.9f, 0.6f), PrimitiveType.Cube);
            }
            Tower(new Vector2(s.C.x - 13f, s.C.y + 2f), 3.0f, 14f, Stone, CityRoofs[0]);
            Tower(new Vector2(s.C.x + 13f, s.C.y + 2f), 3.0f, 14f, Stone, CityRoofs[0]);
            Tower(new Vector2(s.C.x - 13f, s.C.y - 4.5f), 3.0f, 14f, Stone, CityRoofs[0]);
            Tower(new Vector2(s.C.x + 13f, s.C.y - 4.5f), 3.0f, 14f, Stone, CityRoofs[0]);
            Lamp(s.C.x - 4f, s.C.y - 8f); Lamp(s.C.x + 4f, s.C.y - 8f);
            Citizen(new Vector3(s.C.x, 0f, s.C.y + 6f), "Capitán de la fortaleza", new Color(0.40f, 0.18f, 0.20f), new Color(0.30f, 0.30f, 0.32f), true, 180f,
                new[] { "Esta muralla lleva siglos separando el bosque del norte de las tierras del pueblo.", "Más allá, al este, están las montañas de lava. Ten cuidado con el suelo que arde." });
        }

        // ---------- Torre del sur ----------

        static void SouthTower()
        {
            Site s = IslandLayout.Tower;
            if (!In(s.C.x, s.C.y)) return;
            float y = s.H;
            Masonry(Cyl(new Vector3(s.C.x, y + 6f, s.C.y), new Vector3(10f, 7f, 10f), Stone, true, "SouthTower"));
            Cyl(new Vector3(s.C.x, y + 13.2f, s.C.y), new Vector3(11.5f, 0.4f, 11.5f), StoneDark, false, "SouthRim");
            Ball(new Vector3(s.C.x, y + 15f, s.C.y), new Vector3(11f, 6f, 11f), CityRoofs[0], false, "SouthRoof");
            Box(new Vector3(s.C.x, y + 1.4f, s.C.y - 5.1f), new Vector3(2.2f, 2.8f, 0.5f), Timber, false, 0f, "SouthDoor");
            Glow(new Vector3(s.C.x, y + 11f, s.C.y - 5.1f), new Vector3(1.1f, 1.6f, 0.2f), new Color(1f, 0.85f, 0.5f), PrimitiveType.Cube);
            Glow(new Vector3(s.C.x, y + 19f, s.C.y), new Vector3(1.0f, 1.0f, 1.0f), new Color(1f, 0.8f, 0.4f));
        }

        // ---------- Cabañas sueltas ----------

        static void Cabins()
        {
            for (int i = 0; i < IslandLayout.Cabins.Length; i++)
            {
                Vector2 p = IslandLayout.Cabins[i];
                if (!In(p.x, p.y)) continue;
                if (!IslandLayout.CabinOk(p.x, p.y)) continue;
                float yaw = (Mathf.Abs(Hash(i, 17)) % 4) * 90f;
                House(p.x, p.y, yaw, Rand01(i, 2) > 0.5f ? Planks : Plaster, CabinRoofs[i % 4], 5.6f, 4.8f, 2.6f);
                if (i % 3 == 0) Barrel(p.x + 4.2f, p.y + 0.5f);
                if (i % 4 == 1) Crate(p.x - 4f, p.y + 1f, 0.8f);
            }
        }

        // ---------- Lava ----------

        static void LavaPools()
        {
            for (int i = 0; i < IslandLayout.Lava.Length; i++)
            {
                Vector3 p = IslandLayout.Lava[i];
                float r = p.y;
                if (!In(p.x, p.z)) continue;
                float surface = H(p.x, p.z) + 0.32f;
                Color lava = new Color(1f, 0.42f, 0.08f);
                GameObject pool = Prim.Make(PrimitiveType.Cylinder, T, new Vector3(p.x, surface, p.z), new Vector3(r * 2.1f, 0.05f, r * 2.1f), lava, false, "LavaPool", true);
                Collider pc = pool.GetComponent<Collider>();
                if (pc != null) Object.Destroy(pc);
                GameObject lg = new GameObject("LavaLight");
                lg.transform.SetParent(T, false);
                lg.transform.position = new Vector3(p.x, surface + 1.6f, p.z);
                Light l = lg.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.5f, 0.2f);
                l.range = r * 3.5f + 6f;
                l.intensity = 1.6f;
                l.shadows = LightShadows.None;
                LavaPatch.Create(pool.transform, new Vector3(p.x, surface, p.z), r * 1.05f);
            }

            // Ríos de lava que bajan de las cumbres: tramos brillantes (inofensivos al roce, pero llamativos).
            Vector2[][] streams =
            {
                new[] { IslandShape.FromPx(735f, 165f), IslandShape.FromPx(728f, 185f), IslandShape.FromPx(738f, 200f), IslandShape.FromPx(730f, 215f) },
                new[] { IslandShape.FromPx(690f, 235f), IslandShape.FromPx(686f, 252f) },
                new[] { IslandShape.FromPx(748f, 225f), IslandShape.FromPx(744f, 238f) }
            };
            for (int s = 0; s < streams.Length; s++)
            {
                for (int k = 0; k + 1 < streams[s].Length; k++)
                {
                    Vector2 a = streams[s][k], b = streams[s][k + 1];
                    Vector2 m = (a + b) * 0.5f;
                    if (!In(m.x, m.y)) continue;
                    Vector2 u = (b - a).normalized;
                    float yaw = Mathf.Atan2(u.x, u.y) * Mathf.Rad2Deg;
                    Glow(new Vector3(m.x, H(m.x, m.y) + 0.12f, m.y), new Vector3(1.6f, 0.12f, (b - a).magnitude + 0.5f), new Color(1f, 0.45f, 0.10f), PrimitiveType.Cube, yaw);
                }
            }
        }

        // ---------- Niebla del pantano ----------

        static Material MistMaterial()
        {
            if (mist != null) return mist;
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) return Prim.Mat(new Color(0.85f, 0.90f, 0.90f));
            mist = new Material(sh) { name = "Swamp mist" };
            mist.color = new Color(0.88f, 0.93f, 0.93f, 0.20f);
            return mist;
        }

        static void Mist()
        {
            float mx = (X0 + X1) * 0.5f, mz = (Z0 + Z1) * 0.5f;
            if (IslandLayout.SwampWeight(mx, mz) < 0.25f) return;
            System.Random rng = new System.Random(Hash((int)X0, (int)Z0) ^ 0x6a1);
            int n = 5;
            for (int i = 0; i < n; i++)
            {
                float x = X0 + 2f + (float)rng.NextDouble() * (X1 - X0 - 4f);
                float z = Z0 + 2f + (float)rng.NextDouble() * (Z1 - Z0 - 4f);
                if (IslandLayout.SwampWeight(x, z) < 0.3f) continue;
                float h = H(x, z);
                if (h < -0.5f) continue;
                GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                g.name = "Mist";
                Collider col = g.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);
                g.transform.SetParent(T, false);
                float w = 9f + (float)rng.NextDouble() * 9f;
                g.transform.position = new Vector3(x, h + 1.3f + (float)rng.NextDouble() * 1.2f, z);
                g.transform.localScale = new Vector3(w, 1.6f + (float)rng.NextDouble() * 1.4f, w * 0.7f);
                g.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 180f, 0f);
                Renderer r = g.GetComponent<Renderer>();
                r.sharedMaterial = MistMaterial();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }
    }

    /// <summary>Un charco de lava que quema al jugador si lo pisa.</summary>
    public class LavaPatch : MonoBehaviour
    {
        Vector3 center;
        float radius;
        float nextHurt;

        public static LavaPatch Create(Transform parent, Vector3 pos, float radius)
        {
            LavaPatch p = parent.gameObject.AddComponent<LavaPatch>();
            p.center = pos;
            p.radius = radius;
            return p;
        }

        void Update()
        {
            if (Game.State != GameState.Playing || Game.Player == null) return;
            Vector3 pp = Game.Player.transform.position;
            float dx = pp.x - center.x, dz = pp.z - center.z;
            if (dx * dx + dz * dz > radius * radius || pp.y > center.y + 1.2f) return;
            if (Time.time < nextHurt) return;
            nextHurt = Time.time + 0.9f;
            Health h = Game.Player.GetComponent<Health>();
            if (h != null) h.Damage(0.5f, center + new Vector3(dx, 0f, dz).normalized * 0.1f);
            Game.Say("¡La lava quema!", 1.5f);
        }
    }
}
