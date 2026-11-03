using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Pueblo de la isla: una plaza con pozo, casas pequeñas, vecinos y, al norte, el edificio del gremio
    /// con el tablón de misiones y la maestra del gremio. Se coloca en el terreno más llano que se encuentra
    /// cerca del campamento y es determinista según la semilla.
    /// </summary>
    public static class Village
    {
        public const float Radius = 28f;

        public static bool Exists;
        public static Vector3 Center;
        public static Vector2Int Chunk;

        static readonly Color Plaster = new Color(0.96f, 0.90f, 0.78f);
        static readonly Color Roof = new Color(0.78f, 0.35f, 0.28f);
        static readonly Color Wood = new Color(0.60f, 0.40f, 0.24f);
        static readonly Color Stone = new Color(0.58f, 0.56f, 0.55f);
        static readonly Color Dirt = new Color(0.72f, 0.58f, 0.40f);

        /// <summary>Punto delante de la puerta del gremio (para el modo dev).</summary>
        public static Vector3 GuildDoor
        {
            get { return Center + new Vector3(0f, 0.6f, 8.5f); }
        }

        public static bool Blocks(float x, float z, float margin)
        {
            if (!Exists) return false;
            float dx = x - Center.x, dz = z - Center.z;
            float r = Radius + margin;
            return dx * dx + dz * dz < r * r;
        }

        // ---------- Colocación ----------

        public static void Init()
        {
            Clear();
            // El pueblo del centro de la isla (IslandLayout.Town): el terreno ya está aplanado ahí.
            Site t = IslandLayout.Town;
            float h = IslandTerrain.Height(t.C.x, t.C.y);
            Exists = true;
            Center = new Vector3(t.C.x, h, t.C.y);
            Chunk = new Vector2Int(Mathf.FloorToInt(Center.x / IslandWorld.ChunkSize), Mathf.FloorToInt(Center.z / IslandWorld.ChunkSize));
        }

        public static void Clear()
        {
            Exists = false;
        }

        public static void PopulateChunk(Transform chunk, int cx, int cz)
        {
            if (Exists && Chunk.x == cx && Chunk.y == cz) Build(chunk);
        }

        // ---------- Construcción ----------

        static void Build(Transform chunk)
        {
            GameObject rg = new GameObject("Village");
            rg.transform.SetParent(chunk, false);
            rg.transform.position = Center;
            Transform v = rg.transform;

            // Plaza de tierra (gruesa, para asentarse en la pendiente suave del terreno).
            Prim.Cyl(v, new Vector3(0f, -0.85f, 0f), new Vector3(20f, 1f, 20f), Dirt, false, "Plaza");
            Prim.Box(v, new Vector3(0f, -0.85f, 8.5f), new Vector3(4.5f, 1f, 9f), Dirt, false, "Path");

            // Pozo
            Prim.Cyl(v, new Vector3(-4f, 0.5f, -2f), new Vector3(2.0f, 0.5f, 2.0f), Stone, true, "Well");
            Prim.Cyl(v, new Vector3(-4f, 0.8f, -2f), new Vector3(1.5f, 0.1f, 1.5f), new Color(0.35f, 0.60f, 0.85f), false, "Water");

            // Casas pequeñas, con la puerta hacia la plaza.
            House(v, new Vector3(-16f, 0f, 4f), -90f, Plaster, Roof);
            House(v, new Vector3(16f, 0f, 3f), 90f, new Color(0.93f, 0.85f, 0.70f), new Color(0.35f, 0.50f, 0.72f));
            House(v, new Vector3(-12f, 0f, -14f), 180f, Plaster, new Color(0.45f, 0.62f, 0.40f));
            House(v, new Vector3(13f, 0f, -13f), 180f, new Color(0.90f, 0.82f, 0.70f), Roof);

            // Faroles de la plaza (decorativos) y algunas cajas.
            Vector3[] lamps = { new Vector3(-7f, 0f, 6f), new Vector3(7f, 0f, 6f), new Vector3(-7f, 0f, -7f), new Vector3(7f, 0f, -7f) };
            for (int i = 0; i < lamps.Length; i++)
            {
                Prim.Cyl(v, lamps[i] + new Vector3(0f, 1.1f, 0f), new Vector3(0.14f, 1.1f, 0.14f), new Color(0.30f, 0.24f, 0.20f), false, "LampPost");
                Prim.Make(PrimitiveType.Sphere, v, lamps[i] + new Vector3(0f, 2.4f, 0f), new Vector3(0.38f, 0.45f, 0.38f), new Color(1f, 0.82f, 0.45f), false, "LampGlow", true);
            }
            Prim.Box(v, new Vector3(8.5f, 0.4f, -3f), new Vector3(0.9f, 0.8f, 0.9f), Wood, true, "Crate");
            Prim.Box(v, new Vector3(9.5f, 0.3f, -3.4f), new Vector3(0.7f, 0.6f, 0.7f), Wood, true, "Crate");

            BuildGuild(v);

            // Vecinos
            Villager(v, new Vector3(-6.5f, 0f, -3.5f), "Vecina", new Color(0.78f, 0.45f, 0.55f), new Color(0.25f, 0.15f, 0.10f), false, 60f,
                new[]
                {
                    "Este pozo nunca se seca. Desde que los goblins se instalaron en la montaña, ya nadie sube a por hierbas.",
                    "El gremio paga bien a quien se atreve con los goblins. Pregunta a la maestra."
                });
            Villager(v, new Vector3(5.5f, 0f, 2.5f), "Cazador", new Color(0.38f, 0.52f, 0.34f), new Color(0.45f, 0.30f, 0.18f), true, -120f,
                new[]
                {
                    "Vi una cueva sellada al pie de la montaña. Por mucho que empujé, la roca no cedía.",
                    "Dicen que el emblema del gremio abre ese sello. Coge una misión del tablón, en el gremio.",
                    "Lleva bombas y un buen escudo: los goblins golpean fuerte, pero se les ve venir."
                });
        }

        static void Villager(Transform parent, Vector3 pos, string name, Color outfit, Color hair, bool beard, float yaw, string[] lines)
        {
            GameObject npc = Npc.Create(parent, parent.TransformPoint(pos), name, outfit, hair, beard, yaw);
            Interactable it = npc.AddComponent<Interactable>();
            it.Prompt = "Hablar con " + name.ToLower();
            it.Range = 3.0f;
            int n = 0;
            it.OnUse = () => { Game.Dialog(name, lines[n % lines.Length], 6f); n++; };
        }

        static void House(Transform parent, Vector3 localPos, float yaw, Color wall, Color roof)
        {
            GameObject g = new GameObject("VillageHouse");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Transform t = g.transform;
            Prim.Box(t, new Vector3(0f, -0.9f, 0f), new Vector3(7.4f, 2f, 6.4f), Stone, true, "Foundation");
            Prim.Box(t, new Vector3(0f, 1.5f, 0f), new Vector3(7f, 3f, 6f), wall, true, "Walls");
            GameObject r1 = Prim.Box(t, new Vector3(0f, 3.7f, -1.6f), new Vector3(7.8f, 0.3f, 3.8f), roof, false, "RoofA");
            r1.transform.localRotation = Quaternion.Euler(-32f, 0f, 0f);
            GameObject r2 = Prim.Box(t, new Vector3(0f, 3.7f, 1.6f), new Vector3(7.8f, 0.3f, 3.8f), roof, false, "RoofB");
            r2.transform.localRotation = Quaternion.Euler(32f, 0f, 0f);
            Prim.Box(t, new Vector3(0f, 0.9f, -3.02f), new Vector3(1.3f, 1.8f, 0.1f), Wood, false, "Door");
            Prim.Make(PrimitiveType.Cube, t, new Vector3(2.2f, 1.7f, -3.02f), new Vector3(1.0f, 0.9f, 0.1f), new Color(1f, 0.92f, 0.60f), false, "Window", true);
        }

        // ---------- El gremio ----------

        static void BuildGuild(Transform village)
        {
            GameObject hg = new GameObject("Guild");
            hg.transform.SetParent(village, false);
            hg.transform.localPosition = new Vector3(0f, 0f, 16f);
            Transform h = hg.transform;

            Prim.Box(h, new Vector3(0f, -1.0f, 0f), new Vector3(15f, 2f, 11f), Stone, true, "Foundation");
            Prim.Box(h, new Vector3(0f, 0.04f, 0f), new Vector3(14f, 0.1f, 10f), new Color(0.80f, 0.64f, 0.44f), false, "Floor");

            // Paredes: la puerta mira a la plaza (lado sur, x entre -1.8 y 1.8).
            float H = 3.6f, T = 0.5f;
            PrologueWorld.Wall(h, -7f, 5f, 7f, 5f, H, T, Plaster);
            PrologueWorld.Wall(h, -7f, -5f, -7f, 5f, H, T, Plaster);
            PrologueWorld.Wall(h, 7f, -5f, 7f, 5f, H, T, Plaster);
            PrologueWorld.Wall(h, -7f, -5f, -1.8f, -5f, H, T, Plaster);
            PrologueWorld.Wall(h, 1.8f, -5f, 7f, -5f, H, T, Plaster);
            Prim.Box(h, new Vector3(0f, 3.25f, -5f), new Vector3(3.8f, 0.7f, 0.5f), Wood, false, "Lintel");

            // Aleros y vigas (sin techo cerrado, como la casa del prólogo, para que la cámara no estorbe).
            Prim.Box(h, new Vector3(-7f, 3.8f, 0f), new Vector3(0.7f, 0.2f, 10.8f), Roof, false, "EaveL");
            Prim.Box(h, new Vector3(7f, 3.8f, 0f), new Vector3(0.7f, 0.2f, 10.8f), Roof, false, "EaveR");
            Prim.Box(h, new Vector3(0f, 3.8f, 5f), new Vector3(14.8f, 0.2f, 0.7f), Roof, false, "EaveBack");
            Prim.Box(h, new Vector3(-4.4f, 3.8f, -5f), new Vector3(5.6f, 0.2f, 0.7f), Roof, false, "EaveFrontL");
            Prim.Box(h, new Vector3(4.4f, 3.8f, -5f), new Vector3(5.6f, 0.2f, 0.7f), Roof, false, "EaveFrontR");
            for (int i = -2; i <= 2; i++)
                Prim.Box(h, new Vector3(i * 3.2f, 3.65f, 0f), new Vector3(0.25f, 0.25f, 10.4f), Wood, false, "Beam");

            // Cartel sobre la puerta y estandartes.
            Prim.Box(h, new Vector3(0f, 4.6f, -5.2f), new Vector3(5.2f, 1.2f, 0.22f), new Color(0.38f, 0.25f, 0.15f), false, "SignBoard");
            SignText(h, new Vector3(0f, 4.6f, -5.36f), "GREMIO", 0.16f, new Color(1f, 0.88f, 0.45f));
            Color[] banner = { new Color(0.70f, 0.18f, 0.20f), new Color(0.20f, 0.35f, 0.70f) };
            for (int s = 0; s < 2; s++)
            {
                float x = s == 0 ? -3.6f : 3.6f;
                Prim.Box(h, new Vector3(x, 2.2f, -5.32f), new Vector3(0.9f, 1.8f, 0.06f), banner[s], false, "Banner");
                Prim.Make(PrimitiveType.Cube, h, new Vector3(x, 2.4f, -5.37f), new Vector3(0.34f, 0.34f, 0.03f), new Color(1f, 0.85f, 0.35f), false, "Emblem", true)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }

            // Ventanas luminosas
            Color glass = new Color(1f, 0.92f, 0.60f);
            Prim.Make(PrimitiveType.Cube, h, new Vector3(-7.02f, 2.0f, -2.2f), new Vector3(0.1f, 1.1f, 1.5f), glass, false, "Window1", true);
            Prim.Make(PrimitiveType.Cube, h, new Vector3(7.02f, 2.0f, -2.2f), new Vector3(0.1f, 1.1f, 1.5f), glass, false, "Window2", true);

            // Tablón de misiones en la pared del fondo.
            GameObject board = Prim.Box(h, new Vector3(0f, 1.9f, 4.55f), new Vector3(5.2f, 2.6f, 0.2f), Wood, true, "QuestBoard");
            Prim.Box(h, new Vector3(0f, 1.9f, 4.43f), new Vector3(4.8f, 2.2f, 0.05f), new Color(0.62f, 0.46f, 0.30f), false, "Cork");
            Color paper = new Color(0.95f, 0.92f, 0.80f);
            for (int i = 0; i < 7; i++)
            {
                float px = -1.9f + (i % 4) * 1.25f + (i >= 4 ? 0.6f : 0f);
                float py = i < 4 ? 2.5f : 1.45f;
                GameObject note = Prim.Box(h, new Vector3(px, py, 4.38f), new Vector3(0.7f, 0.9f, 0.02f), paper, false, "Note");
                note.transform.localRotation = Quaternion.Euler(0f, 0f, (i * 37 % 9) - 4f);
                Prim.Ball(h, new Vector3(px, py + 0.38f, 4.36f), Vector3.one * 0.07f, new Color(0.85f, 0.2f, 0.2f), false, "Pin");
            }
            Prim.Make(PrimitiveType.Sphere, h, new Vector3(0f, 3.5f, 4.35f), new Vector3(0.32f, 0.32f, 0.32f), new Color(1f, 0.85f, 0.3f), false, "BoardMarker", true);
            Interactable it = board.AddComponent<Interactable>();
            it.Prompt = "Ver el tablón de misiones";
            it.Range = 3.8f;
            it.OnUse = () => { if (Panels.Instance != null) Panels.Instance.OpenQuests(); };

            // Mostrador y maestra del gremio.
            Prim.Box(h, new Vector3(3.6f, 0.55f, 0.8f), new Vector3(4.2f, 1.1f, 1.0f), Wood, true, "Counter");
            Prim.Box(h, new Vector3(3.6f, 1.15f, 0.8f), new Vector3(4.4f, 0.1f, 1.2f), new Color(0.72f, 0.52f, 0.32f), false, "CounterTop");
            GameObject master = Npc.Create(h, h.TransformPoint(new Vector3(3.6f, 0.09f, 2.3f)), "Maestra del gremio", new Color(0.62f, 0.20f, 0.24f), new Color(0.80f, 0.78f, 0.82f), false, 180f);
            Interactable mi = master.AddComponent<Interactable>();
            mi.Prompt = "Hablar con la maestra del gremio";
            mi.Range = 4.4f;
            mi.OnUse = Quests.TalkToMaster;

            // Mobiliario
            Prim.Box(h, new Vector3(0f, 0.11f, -1.2f), new Vector3(4.2f, 0.03f, 3.2f), new Color(0.65f, 0.20f, 0.22f), false, "Rug");
            Prim.Cyl(h, new Vector3(-4.2f, 0.5f, -0.5f), new Vector3(1.8f, 0.45f, 1.8f), Wood, true, "Table");
            Prim.Cyl(h, new Vector3(-4.2f, 0.5f, 2.4f), new Vector3(1.8f, 0.45f, 1.8f), Wood, true, "Table2");
            Vector3[] stools = { new Vector3(-5.4f, 0f, -0.5f), new Vector3(-3.0f, 0f, -0.5f), new Vector3(-5.4f, 0f, 2.4f), new Vector3(-3.0f, 0f, 2.4f) };
            for (int i = 0; i < stools.Length; i++)
                Prim.Cyl(h, stools[i] + new Vector3(0f, 0.3f, 0f), new Vector3(0.5f, 0.25f, 0.5f), new Color(0.50f, 0.34f, 0.20f), false, "Stool");
            Prim.Cyl(h, new Vector3(6.1f, 0.6f, 3.9f), new Vector3(0.9f, 0.5f, 0.9f), Wood, true, "Barrel");
            Prim.Cyl(h, new Vector3(5.2f, 0.6f, 4.1f), new Vector3(0.9f, 0.5f, 0.9f), Wood, true, "Barrel");
            Prim.Box(h, new Vector3(-6.6f, 1.9f, 1.0f), new Vector3(0.1f, 0.1f, 1.8f), new Color(0.82f, 0.86f, 0.92f), false, "SwordDisplay");
            Prim.Make(PrimitiveType.Cylinder, h, new Vector3(-6.7f, 1.4f, 1.0f), new Vector3(0.8f, 0.04f, 0.8f), new Color(0.30f, 0.45f, 0.75f), false, "ShieldDisplay")
                .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            // Luz cálida dentro
            for (int i = 0; i < 2; i++)
            {
                GameObject lg = new GameObject("GuildLamp");
                lg.transform.SetParent(h, false);
                lg.transform.localPosition = new Vector3(i == 0 ? -3.2f : 3.2f, 2.9f, 1.5f);
                Light l = lg.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.78f, 0.45f);
                l.range = 10f;
                l.intensity = 1.5f;
                l.shadows = LightShadows.None;
            }
        }

        static void SignText(Transform parent, Vector3 localPos, string text, float size, Color color)
        {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) return;
            GameObject g = new GameObject("SignText");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            TextMesh tm = g.AddComponent<TextMesh>();
            tm.font = f;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            MeshRenderer mr = g.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = f.material;
        }
    }
}
