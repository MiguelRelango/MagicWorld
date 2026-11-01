using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Contenido de cada chunk de la isla: árboles, flores, rocas, boulders con botín, cofres,
    /// ruinas con guardianes y slimes salvajes. Todo es determinista según la semilla y las coordenadas.
    /// </summary>
    public static class IslandProps
    {
        static readonly Color StoneLight = new Color(0.72f, 0.70f, 0.66f);
        static readonly Color StoneDark = new Color(0.52f, 0.50f, 0.48f);
        static readonly Color[] FlowerColors =
        {
            new Color(1f, 0.85f, 0.35f), new Color(0.95f, 0.50f, 0.65f),
            new Color(0.80f, 0.60f, 0.95f), new Color(1f, 1f, 1f), new Color(0.55f, 0.75f, 1f)
        };

        static int Hash(int cx, int cz, int salt)
        {
            unchecked
            {
                return IslandTerrain.Seed * 73856093 ^ cx * 19349663 ^ cz * 83492791 ^ salt * 668265263;
            }
        }

        static float DistCamp(float x, float z)
        {
            Vector3 c = IslandTerrain.Camp;
            float dx = x - c.x, dz = z - c.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Busca un punto válido (en tierra firme, poco inclinado) dentro del chunk.</summary>
        static bool Spot(System.Random rng, int ox, int oz, float minH, float maxH, float maxSlope, float minCamp, out Vector3 p)
        {
            int N = IslandWorld.ChunkSize;
            for (int t = 0; t < 8; t++)
            {
                float x = ox + 2f + (float)rng.NextDouble() * (N - 4f);
                float z = oz + 2f + (float)rng.NextDouble() * (N - 4f);
                float h = IslandTerrain.Height(x, z);
                if (h < minH || h > maxH) continue;
                if (IslandTerrain.Slope(x, z) > maxSlope) continue;
                if (DistCamp(x, z) < minCamp) continue;
                p = new Vector3(x, h, z);
                return true;
            }
            p = Vector3.zero;
            return false;
        }

        // ---------- Chunks ----------

        public static void Populate(Transform chunk, int cx, int cz)
        {
            int N = IslandWorld.ChunkSize;
            int ox = cx * N, oz = cz * N;
            System.Random rng = new System.Random(Hash(cx, cz, 1));
            Vector3 p;

            // Árboles: más densos en manchas de bosque.
            float forest = IslandTerrain.Forest(ox + N * 0.5f, oz + N * 0.5f);
            int trees = Mathf.Clamp(Mathf.RoundToInt((forest - 0.38f) * 60f), 0, 16);
            for (int i = 0; i < trees; i++)
            {
                if (!Spot(rng, ox, oz, 1.6f, 12f, 0.55f, 14f, out p)) continue;
                PrologueWorld.Tree(chunk, p, 0.8f + (float)rng.NextDouble() * 0.8f);
            }

            // Flores y piedras pequeñas (sin colisión).
            for (int i = 0; i < 10; i++)
            {
                if (!Spot(rng, ox, oz, 1.3f, 9f, 0.6f, 6f, out p)) continue;
                WorldLook.Flower(chunk, p, FlowerColors[rng.Next(FlowerColors.Length)]);
            }
            for (int i = 0; i < 3; i++)
            {
                if (!Spot(rng, ox, oz, 0.4f, 14f, 0.8f, 6f, out p)) continue;
                float s = 0.5f + (float)rng.NextDouble() * 0.7f;
                WorldLook.Rock(chunk, p + Vector3.up * (0.17f * s), new Vector3(s, s * 0.7f, s * 0.9f), Mathf.RoundToInt(p.x * 13f + p.z * 31f));
            }

            // Boulder con grietas: se rompe con bombas y suelta botín.
            if (rng.NextDouble() < 0.40 && Spot(rng, ox, oz, 1.5f, 13f, 0.5f, 10f, out p))
                MakeBoulder(chunk, p, "b" + cx + "," + cz, (float)rng.NextDouble() * 360f);

            // Cofre suelto.
            if (rng.NextDouble() < 0.10 && Spot(rng, ox, oz, 2f, 13f, 0.4f, 25f, out p))
                MakeChest(chunk, p, "c" + cx + "," + cz, (float)rng.NextDouble() * 360f);

            // Cofre sellado por enredaderas: hay que quemarlas con la varita (R).
            if (rng.NextDouble() < 0.14 && Spot(rng, ox, oz, 2f, 12f, 0.25f, 25f, out p))
                MakeVineChest(chunk, p, "v" + cx + "," + cz, (float)rng.NextDouble() * 360f);

            // Ruinas con guardianes (una de cada ~9 chunks).
            if (Mathf.Abs(Hash(cx, cz, 7)) % 9 == 0) TryRuin(chunk, cx, cz, ox, oz, rng);

            // Slimes salvajes (nunca cerca del campamento).
            int slimes = rng.NextDouble() < 0.55 ? (rng.NextDouble() < 0.3 ? 2 : 1) : 0;
            for (int i = 0; i < slimes; i++)
            {
                if (!Spot(rng, ox, oz, 1.6f, 11f, 0.5f, 55f, out p)) continue;
                EnemySlime.Spawn(p + Vector3.up * 0.1f, chunk);
            }
        }

        // ---------- Piezas ----------

        static void MakeBoulder(Transform chunk, Vector3 p, string key, float yaw)
        {
            if (IslandWorld.IsConsumed(key)) return;
            Vector3 pos = p + Vector3.up * 1.0f;
            BreakableWall w = BreakableWall.Create(chunk, pos, new Vector3(2.2f, 2.0f, 2.2f), new Color(0.55f, 0.52f, 0.50f));
            w.BurstSize = 0.35f;
            w.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            w.BreakMessage = "¡La roca se rompió!";
            w.OnBroken = () =>
            {
                IslandWorld.Consume(key);
                Pickup.Spawn(p + new Vector3(0.7f, 0.5f, 0f), PickupKind.Bombs, 2);
                Pickup.Spawn(p + new Vector3(-0.7f, 0.5f, 0.3f), PickupKind.Gem, 2);
            };
        }

        static Chest MakeChest(Transform chunk, Vector3 p, string key, float yaw)
        {
            if (IslandWorld.IsConsumed(key)) return null;
            Chest c = Chest.Create(chunk, p, yaw);
            c.OnOpened = () => { IslandWorld.Consume(key); Reward(); };
            return c;
        }

        static void MakeVineChest(Transform chunk, Vector3 p, string key, float yaw)
        {
            if (IslandWorld.IsConsumed(key)) return;
            Prim.Cyl(chunk, p + Vector3.up * 0.02f, new Vector3(5.2f, 0.03f, 5.2f), new Color(0.40f, 0.34f, 0.26f), false, "Clearing");
            Chest c = Chest.Create(chunk, p, yaw);
            c.OnOpened = () => { IslandWorld.Consume(key); Reward(); Reward(); Game.Mana = Game.MaxMana; };
            int n = 7;
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                string vkey = key + "n" + i;   // cada mata quemada se recuerda aunque el chunk se descargue
                if (IslandWorld.IsConsumed(vkey)) continue;
                Vector3 vp = p + new Vector3(Mathf.Cos(a) * 1.9f, 0f, Mathf.Sin(a) * 1.9f);
                vp.y = IslandTerrain.Height(vp.x, vp.z);
                Vine v = Vine.Create(chunk, vp, -a * Mathf.Rad2Deg + 90f, 1f);
                v.OnBurnt = () => IslandWorld.Consume(vkey);
            }
        }

        static void Reward()
        {
            Sfx.Play("pickup");
            int r = Random.Range(0, 4);
            if (r == 0) { Game.Gems += 6; Game.Say("Cofre: +6 gemas", 3f); }
            else if (r == 1) { Game.HasBombs = true; Game.Bombs = Mathf.Min(20, Game.Bombs + 5); Game.Say("Cofre: +5 bombas", 3f); }
            else if (r == 2) { Game.Arrows = Mathf.Min(30, Game.Arrows + 12); Game.Say("Cofre: +12 flechas", 3f); }
            else
            {
                Health h = Game.Player != null ? Game.Player.GetComponent<Health>() : null;
                if (h != null) h.Heal(99f);
                Game.Say("Cofre: corazones restaurados", 3f);
            }
        }

        static void TryRuin(Transform chunk, int cx, int cz, int ox, int oz, System.Random rng)
        {
            string key = "r" + cx + "," + cz;
            bool done = IslandWorld.IsConsumed(key);   // la ruina permanece; solo cofre y guardianes se consumen

            // Busca un claro llano y en tierra para el círculo de piedras.
            Vector3 c = Vector3.zero;
            bool ok = false;
            for (int t = 0; t < 10 && !ok; t++)
            {
                float x = ox + 10f + (float)rng.NextDouble() * (IslandWorld.ChunkSize - 20f);
                float z = oz + 10f + (float)rng.NextDouble() * (IslandWorld.ChunkSize - 20f);
                float h = IslandTerrain.Height(x, z);
                if (h < 2.5f || h > 11f || IslandTerrain.Slope(x, z) > 0.12f || DistCamp(x, z) < 60f) continue;
                bool flat = true;
                for (int k = 0; k < 8 && flat; k++)
                {
                    float a = k * Mathf.PI * 0.25f;
                    float hh = IslandTerrain.Height(x + Mathf.Cos(a) * 7f, z + Mathf.Sin(a) * 7f);
                    if (Mathf.Abs(hh - h) > 0.9f) flat = false;
                }
                if (!flat) continue;
                c = new Vector3(x, h, z);
                ok = true;
            }
            if (!ok) return;

            WorldLook.Surface(Prim.Cyl(chunk, c + Vector3.up * -0.1f, new Vector3(15f, 0.5f, 15f), StoneDark, false, "RuinFloor"), "Masonry", 3f);
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI * 0.25f;
                float height = (k % 3 == 0) ? 1.4f : 3.2f + (float)rng.NextDouble() * 1.8f;   // algunas rotas
                Vector3 pp = c + new Vector3(Mathf.Cos(a) * 6.2f, height * 0.5f + 0.1f, Mathf.Sin(a) * 6.2f);
                WorldLook.Surface(Prim.Box(chunk, pp, new Vector3(1.1f, height, 1.1f), StoneLight, true, "Pillar"), "Masonry", 2f);
                if (k % 3 != 0) WorldLook.Surface(Prim.Box(chunk, pp + Vector3.up * (height * 0.5f + 0.15f), new Vector3(1.4f, 0.3f, 1.4f), StoneDark, false, "Capital"), "Masonry", 2f);
            }
            WorldLook.Surface(Prim.Box(chunk, c + new Vector3(0f, 0.35f, 0f), new Vector3(2.6f, 0.5f, 2.6f), StoneLight, true, "Altar"), "Masonry", 2f);

            if (done) return;

            Chest chest = Chest.Create(chunk, c + new Vector3(0f, 0.6f, 0f), (float)rng.NextDouble() * 360f);
            chest.OnOpened = () => { IslandWorld.Consume(key); Reward(); Reward(); };

            for (int k = 0; k < 3; k++)
            {
                float a = k * Mathf.PI * 2f / 3f + 0.4f;
                EnemySlime.Spawn(c + new Vector3(Mathf.Cos(a) * 3.8f, 0.2f, Mathf.Sin(a) * 3.8f), chunk);
            }
        }

        // ---------- Campamento ----------

        public static void BuildCamp(Transform root, Vector3 camp)
        {
            Transform t = new GameObject("Camp").transform;
            t.SetParent(root, false);

            // Fogata con troncos
            Vector3 fire = At(camp.x + 0f, camp.z + 0f);
            Prim.Cyl(t, fire + new Vector3(0f, 0.15f, 0f), new Vector3(1.5f, 0.1f, 1.5f), new Color(0.35f, 0.32f, 0.30f), false, "FireBase");
            for (int i = 0; i < 4; i++)
            {
                GameObject log = Prim.Cyl(t, fire + new Vector3(Mathf.Cos(i * 1.57f) * 0.5f, 0.3f, Mathf.Sin(i * 1.57f) * 0.5f), new Vector3(0.18f, 0.5f, 0.18f), new Color(0.50f, 0.34f, 0.22f), false, "Log");
                log.transform.localRotation = Quaternion.Euler(70f, -i * 90f, 0f);
            }
            Prim.Make(PrimitiveType.Sphere, t, fire + new Vector3(0f, 0.55f, 0f), new Vector3(0.45f, 0.6f, 0.45f), new Color(1f, 0.6f, 0.2f), false, "Flame", true);
            GameObject lg = new GameObject("CampLight");
            lg.transform.SetParent(t, false);
            lg.transform.position = fire + Vector3.up * 1.4f;
            Light l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.7f, 0.4f);
            l.range = 12f;
            l.intensity = 1.2f;

            // Tienda de campaña
            Vector3 tent = At(camp.x + 5f, camp.z + 3f);
            GameObject r1 = Prim.Box(t, tent + new Vector3(-0.9f, 1.1f, 0f), new Vector3(0.15f, 3.0f, 3.6f), new Color(0.88f, 0.48f, 0.36f), true, "TentL");
            r1.transform.localRotation = Quaternion.Euler(0f, 0f, 38f);
            GameObject r2 = Prim.Box(t, tent + new Vector3(0.9f, 1.1f, 0f), new Vector3(0.15f, 3.0f, 3.6f), new Color(0.92f, 0.56f, 0.40f), true, "TentR");
            r2.transform.localRotation = Quaternion.Euler(0f, 0f, -38f);

            // Refugio de la pareja
            Vector3 refuge = At(camp.x - 5f, camp.z + 2f);
            GameObject rg = new GameObject("CampRefuge");
            rg.transform.SetParent(t, false);
            rg.transform.position = refuge;
            Prim.Make(PrimitiveType.Cylinder, rg.transform, new Vector3(0f, 0.04f, 0f), new Vector3(6f, 0.03f, 6f), new Color(0.55f, 0.85f, 0.70f), false, "Glow", true);
            Prim.Cyl(rg.transform, new Vector3(0f, 1.0f, 0f), new Vector3(0.12f, 1.0f, 0.12f), new Color(0.60f, 0.40f, 0.24f), false, "Pole");
            Prim.Box(rg.transform, new Vector3(0.35f, 1.7f, 0f), new Vector3(0.7f, 0.5f, 0.05f), new Color(0.95f, 0.55f, 0.62f), false, "Banner");
            rg.AddComponent<Refuge>().Radius = 3.5f;

            CraftingSystem.Bench(t, At(camp.x + 4f, camp.z - 4f));

            // Letrero
            Vector3 sign = At(camp.x + 1.5f, camp.z - 5f);
            Prim.Box(t, sign + new Vector3(0f, 0.7f, 0f), new Vector3(0.18f, 1.4f, 0.18f), new Color(0.60f, 0.40f, 0.24f), false, "SignPost");
            Prim.Box(t, sign + new Vector3(0f, 1.3f, 0f), new Vector3(1.6f, 0.55f, 0.12f), new Color(0.80f, 0.64f, 0.44f), false, "SignBoard");
        }

        static Vector3 At(float x, float z)
        {
            return new Vector3(x, IslandTerrain.Height(x, z), z);
        }
    }
}
