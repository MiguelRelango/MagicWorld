using System.Collections.Generic;
using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Construye por código el mundo del prólogo: casa, aldea, huerto, campana de práctica y cueva.
    /// Coordenadas: casa en el origen, aldea al norte (+Z), cueva al este (+X).
    /// </summary>
    public static partial class PrologueWorld
    {
        public static Transform Root;
        public static Vector3 PlayerStart = new Vector3(-1f, 0.1f, 0f);
        public static Vector3 PartnerStart = new Vector3(1.8f, 0.1f, -1f);
        public static Vector3 GardenCenter = new Vector3(-24f, 0f, 8f);
        public static Vector3 BellPos = new Vector3(22f, 0f, 12f);
        public static Vector3 ElderPos = new Vector3(2f, 0f, 27f);
        public static Vector3 CaveMouth = new Vector3(37f, 0f, 30f);

        static readonly Color Grass = new Color(0.52f, 0.78f, 0.38f);
        static readonly Color Dirt = new Color(0.72f, 0.58f, 0.40f);
        static readonly Color Wood = new Color(0.60f, 0.40f, 0.24f);
        static readonly Color Plaster = new Color(0.96f, 0.90f, 0.78f);
        static readonly Color Roof = new Color(0.78f, 0.35f, 0.28f);
        static readonly Color Stone = new Color(0.58f, 0.56f, 0.55f);

        public static void Build(PrologueDirector dir)
        {
            Clear();
            GameObject rootGo = new GameObject("PrologueWorld");
            Root = rootGo.transform;

            BuildLighting();
            BuildGround();
            BuildHouse(dir);
            BuildVillage(dir);
            BuildGarden(dir);
            BuildBell(dir);
            BuildScenery();
            BuildCave(dir);
        }

        public static void Clear()
        {
            if (Root != null) Object.Destroy(Root.gameObject);
            Root = null;
        }

        // ---------- Utilidades ----------

        public static GameObject Wall(Transform parent, float x1, float z1, float x2, float z2, float h, float thick, Color c, float y0 = 0f)
        {
            float cx = (x1 + x2) * 0.5f, cz = (z1 + z2) * 0.5f;
            float sx = Mathf.Abs(x2 - x1), sz = Mathf.Abs(z2 - z1);
            if (sx < sz) sx = thick; else sz = thick;
            return Prim.Box(parent, new Vector3(cx, y0 + h * 0.5f, cz), new Vector3(sx, h, sz), c, true, "Wall");
        }

        static void BuildLighting()
        {
            GameObject sun = new GameObject("Sun");
            sun.transform.SetParent(Root, false);
            sun.transform.rotation = Quaternion.Euler(52f, -30f, 0f);
            Light l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(1f, 0.95f, 0.82f);
            l.intensity = 1.15f;
            l.shadows = LightShadows.Soft;
            RenderSettings.sun = l;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.64f, 0.72f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.80f, 0.90f, 0.95f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 130f;
        }

        static void BuildGround()
        {
            // Terreno de la aldea (termina donde empieza la cueva).
            Prim.Box(Root, new Vector3(-4.5f, -0.5f, 10f), new Vector3(81f, 1f, 100f), Grass, true, "Ground");
            // Camino de tierra: casa -> aldea y aldea -> cueva.
            Prim.Box(Root, new Vector3(0f, 0.02f, 16f), new Vector3(3.2f, 0.05f, 24f), Dirt, false, "PathNorth");
            Prim.Box(Root, new Vector3(18f, 0.02f, 29f), new Vector3(36f, 0.05f, 3.2f), Dirt, false, "PathEast");
            Prim.Box(Root, new Vector3(8f, 0.02f, 8f), new Vector3(30f, 0.05f, 2.6f), Dirt, false, "PathBell");
            Prim.Box(Root, new Vector3(-12f, 0.02f, 6f), new Vector3(24f, 0.05f, 2.6f), Dirt, false, "PathGarden");
        }

        // ---------- Casa ----------

        static void BuildHouse(PrologueDirector dir)
        {
            Transform h = new GameObject("House").transform;
            h.SetParent(Root, false);

            // Suelo de madera
            Prim.Box(h, new Vector3(0f, 0.04f, 0f), new Vector3(12f, 0.1f, 10f), new Color(0.80f, 0.64f, 0.44f), false, "Floor");

            // Paredes (la puerta queda en el lado norte, x entre -1.6 y 1.6)
            float H = 3.2f, T = 0.5f;
            Wall(h, -6f, -5f, 6f, -5f, H, T, Plaster);
            Wall(h, -6f, -5f, -6f, 5f, H, T, Plaster);
            Wall(h, 6f, -5f, 6f, 5f, H, T, Plaster);
            Wall(h, -6f, 5f, -1.6f, 5f, H, T, Plaster);
            Wall(h, 1.6f, 5f, 6f, 5f, H, T, Plaster);
            // Dintel y vigas
            Prim.Box(h, new Vector3(0f, 2.9f, 5f), new Vector3(3.6f, 0.6f, 0.5f), Wood, false, "Lintel");
            Prim.Box(h, new Vector3(-6f, 3.3f, 0f), new Vector3(0.6f, 0.2f, 10.6f), Roof, false, "EaveL");
            Prim.Box(h, new Vector3(6f, 3.3f, 0f), new Vector3(0.6f, 0.2f, 10.6f), Roof, false, "EaveR");
            Prim.Box(h, new Vector3(0f, 3.3f, -5f), new Vector3(12.6f, 0.2f, 0.6f), Roof, false, "EaveBack");
            Prim.Box(h, new Vector3(-3.8f, 3.3f, 5f), new Vector3(4.6f, 0.2f, 0.6f), Roof, false, "EaveFrontL");
            Prim.Box(h, new Vector3(3.8f, 3.3f, 5f), new Vector3(4.6f, 0.2f, 0.6f), Roof, false, "EaveFrontR");

            // Ventanas luminosas
            Color glass = new Color(1f, 0.92f, 0.60f);
            Prim.Make(PrimitiveType.Cube, h, new Vector3(-6.02f, 1.7f, -1.5f), new Vector3(0.1f, 1.0f, 1.4f), glass, false, "Window1", true);
            Prim.Make(PrimitiveType.Cube, h, new Vector3(6.02f, 1.7f, -1.5f), new Vector3(0.1f, 1.0f, 1.4f), glass, false, "Window2", true);

            // Cama
            Prim.Box(h, new Vector3(-4.3f, 0.35f, -3.4f), new Vector3(2.2f, 0.5f, 3.2f), Wood, true, "BedFrame");
            Prim.Box(h, new Vector3(-4.3f, 0.65f, -3.2f), new Vector3(2.0f, 0.2f, 2.9f), new Color(0.90f, 0.55f, 0.60f), false, "Blanket");
            Prim.Box(h, new Vector3(-4.3f, 0.72f, -4.6f), new Vector3(1.4f, 0.2f, 0.6f), Color.white, false, "Pillow");

            // Mesa con velas
            Prim.Cyl(h, new Vector3(3.5f, 0.45f, -2.5f), new Vector3(1.8f, 0.45f, 1.8f), Wood, true, "Table");
            Prim.Make(PrimitiveType.Sphere, h, new Vector3(3.5f, 1.05f, -2.5f), Vector3.one * 0.18f, new Color(1f, 0.8f, 0.3f), false, "Candle", true);

            // Chimenea
            Prim.Box(h, new Vector3(0f, 0.9f, -4.6f), new Vector3(2.4f, 1.8f, 0.7f), Stone, true, "Fireplace");
            Prim.Make(PrimitiveType.Cube, h, new Vector3(0f, 0.5f, -4.2f), new Vector3(1.0f, 0.6f, 0.1f), new Color(1f, 0.55f, 0.2f), false, "Fire", true);
            Light fl = new GameObject("FireLight").AddComponent<Light>();
            fl.transform.SetParent(h, false);
            fl.transform.position = new Vector3(0f, 1.2f, -3.2f);
            fl.type = LightType.Point;
            fl.color = new Color(1f, 0.7f, 0.4f);
            fl.range = 9f;
            fl.intensity = 1.6f;

            // Perchero de armas
            GameObject rack = Prim.Box(h, new Vector3(5.4f, 1.2f, 3.4f), new Vector3(0.4f, 2.0f, 2.2f), Wood, true, "WeaponRack");
            Prim.Box(h, new Vector3(5.1f, 1.5f, 3.4f), new Vector3(0.08f, 0.1f, 1.4f), new Color(0.82f, 0.86f, 0.92f), false, "SwordDisplay");
            Prim.Make(PrimitiveType.Cylinder, h, new Vector3(5.1f, 0.9f, 3.4f), new Vector3(0.7f, 0.04f, 0.7f), new Color(0.30f, 0.45f, 0.75f), false, "ShieldDisplay")
                .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Interactable gear = rack.AddComponent<Interactable>();
            gear.Prompt = "Tomar espada y escudo";
            gear.Range = 3.2f;
            gear.OnUse = () => { dir.OnGearTaken(); gear.Enabled = false; };
            dir.GearRack = gear;

            // Mesa de trabajo (mejora de armadura)
            GameObject bench = Prim.Box(h, new Vector3(-4.6f, 0.55f, 3.6f), new Vector3(2.4f, 1.1f, 1.2f), Wood, true, "Workbench");
            Prim.Box(h, new Vector3(-4.6f, 1.15f, 3.6f), new Vector3(2.6f, 0.12f, 1.3f), new Color(0.72f, 0.52f, 0.32f), false, "BenchTop");
            Prim.Box(h, new Vector3(-4.9f, 1.3f, 3.5f), new Vector3(0.7f, 0.2f, 0.5f), new Color(0.50f, 0.34f, 0.20f), false, "Leather");
            Interactable it = bench.AddComponent<Interactable>();
            it.Prompt = "Mesa de trabajo";
            it.Range = 3.4f;
            it.OnUse = dir.OnBenchUsed;
            dir.Workbench = it;
        }

        // ---------- Aldea ----------

        static void BuildVillage(PrologueDirector dir)
        {
            Transform v = new GameObject("Village").transform;
            v.SetParent(Root, false);

            SmallHouse(v, new Vector3(-13f, 0f, 24f), 0f, Plaster, Roof);
            SmallHouse(v, new Vector3(13f, 0f, 22f), 0f, new Color(0.93f, 0.85f, 0.70f), new Color(0.35f, 0.50f, 0.72f));
            SmallHouse(v, new Vector3(-6f, 0f, 38f), 0f, Plaster, new Color(0.45f, 0.62f, 0.40f));

            // Pozo
            Prim.Cyl(v, new Vector3(-4f, 0.5f, 18f), new Vector3(2.0f, 0.5f, 2.0f), Stone, true, "Well");
            Prim.Cyl(v, new Vector3(-4f, 0.8f, 18f), new Vector3(1.5f, 0.1f, 1.5f), new Color(0.35f, 0.60f, 0.85f), false, "Water");

            // Anciano
            GameObject elder = Npc.Create(v, ElderPos, "Anciano", new Color(0.55f, 0.42f, 0.70f), new Color(0.90f, 0.90f, 0.92f), true, 180f);
            Interactable it = elder.AddComponent<Interactable>();
            it.Prompt = "Hablar con el anciano";
            it.Range = 3.2f;
            it.OnUse = dir.OnElderTalked;
            dir.Elder = it;

            // Letrero hacia las ruinas
            Prim.Box(v, new Vector3(30f, 0.7f, 27f), new Vector3(0.18f, 1.4f, 0.18f), Wood, false, "SignPost");
            Prim.Box(v, new Vector3(30f, 1.3f, 27f), new Vector3(1.4f, 0.5f, 0.12f), new Color(0.80f, 0.64f, 0.44f), false, "SignBoard");
        }

        static void SmallHouse(Transform parent, Vector3 pos, float yaw, Color wall, Color roof)
        {
            GameObject g = new GameObject("VillageHouse");
            g.transform.SetParent(parent, false);
            g.transform.position = pos;
            g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Prim.Box(g.transform, new Vector3(0f, 1.5f, 0f), new Vector3(7f, 3f, 6f), wall, true, "Walls");
            // Techo a dos aguas con dos planos inclinados
            GameObject r1 = Prim.Box(g.transform, new Vector3(0f, 3.7f, -1.6f), new Vector3(7.8f, 0.3f, 3.8f), roof, false, "RoofA");
            r1.transform.localRotation = Quaternion.Euler(-32f, 0f, 0f);
            GameObject r2 = Prim.Box(g.transform, new Vector3(0f, 3.7f, 1.6f), new Vector3(7.8f, 0.3f, 3.8f), roof, false, "RoofB");
            r2.transform.localRotation = Quaternion.Euler(32f, 0f, 0f);
            Prim.Box(g.transform, new Vector3(0f, 0.9f, -3.02f), new Vector3(1.3f, 1.8f, 0.1f), Wood, false, "Door");
            Prim.Make(PrimitiveType.Cube, g.transform, new Vector3(2.2f, 1.7f, -3.02f), new Vector3(1.0f, 0.9f, 0.1f), new Color(1f, 0.92f, 0.60f), false, "Window", true);
        }

        // ---------- Huerto (primer combate) ----------

        static void BuildGarden(PrologueDirector dir)
        {
            Transform g = new GameObject("Garden").transform;
            g.SetParent(Root, false);
            Vector3 c = GardenCenter;

            Prim.Box(g, c + new Vector3(0f, 0.03f, 0f), new Vector3(15f, 0.06f, 11f), new Color(0.50f, 0.36f, 0.24f), false, "Soil");
            for (int ix = -2; ix <= 2; ix++)
            {
                for (int iz = -1; iz <= 1; iz++)
                {
                    if (iz == 0) continue;
                    Vector3 p = c + new Vector3(ix * 2.6f, 0.35f, iz * 2.8f);
                    Prim.Ball(g, p, new Vector3(0.7f, 0.6f, 0.7f), new Color(0.35f, 0.72f, 0.30f), false, "Crop");
                    Prim.Ball(g, p + new Vector3(0f, 0.35f, 0f), Vector3.one * 0.22f, new Color(0.95f, 0.45f, 0.30f), false, "Fruit");
                }
            }
            // Cerca con entrada por el este (hacia el camino)
            float hx = 7.8f, hz = 5.8f;
            Wall(g, c.x - hx, c.z - hz, c.x + hx, c.z - hz, 0.9f, 0.2f, Wood);
            Wall(g, c.x - hx, c.z + hz, c.x + hx, c.z + hz, 0.9f, 0.2f, Wood);
            Wall(g, c.x - hx, c.z - hz, c.x - hx, c.z + hz, 0.9f, 0.2f, Wood);
            Wall(g, c.x + hx, c.z - hz, c.x + hx, c.z - 1.6f, 0.9f, 0.2f, Wood);
            Wall(g, c.x + hx, c.z + 1.6f, c.x + hx, c.z + hz, 0.9f, 0.2f, Wood);

            Zone.Create(g, c, 11f, dir.OnGardenEntered);

            // Rincón seguro para la pareja durante la defensa del huerto
            GameObject shelter = new GameObject("GardenRefuge");
            shelter.transform.SetParent(g, false);
            shelter.transform.position = c + new Vector3(-5.2f, 0f, 3.6f);
            Prim.Make(PrimitiveType.Cylinder, shelter.transform, new Vector3(0f, 0.04f, 0f), new Vector3(5.2f, 0.03f, 5.2f), new Color(0.55f, 0.85f, 0.70f), false, "Glow", true);
            Prim.Box(shelter.transform, new Vector3(0f, 0.5f, 0f), new Vector3(1.2f, 1.0f, 1.2f), Wood, true, "Crate");
            shelter.AddComponent<Refuge>().Radius = 3f;
        }

        // ---------- Campana de práctica con arco ----------

        static void BuildBell(PrologueDirector dir)
        {
            Transform b = new GameObject("BellPractice").transform;
            b.SetParent(Root, false);
            BellTarget bell = BellTarget.Create(b, BellPos);
            bell.OnRung = dir.OnBellRung;
            dir.Bell = bell;
            // Marca donde pararse
            Prim.Cyl(b, new Vector3(9f, 0.04f, 11f), new Vector3(1.8f, 0.04f, 1.8f), new Color(0.95f, 0.80f, 0.35f), false, "StandMark");
        }

        // ---------- Escenario ----------

        static void BuildScenery()
        {
            Transform s = new GameObject("Scenery").transform;
            s.SetParent(Root, false);
            System.Random rng = new System.Random(42);

            // Árboles en anillo alrededor del terreno y algunos sueltos.
            for (int i = 0; i < 70; i++)
            {
                float x = Mathf.Lerp(-44f, 34f, (float)rng.NextDouble());
                float z = Mathf.Lerp(-38f, 58f, (float)rng.NextDouble());
                bool edge = x < -36f || z < -30f || z > 50f || x > 28f;
                if (!edge && rng.NextDouble() > 0.25) continue;
                if (Near(x, z, 0f, 0f, 11f) || Near(x, z, 0f, 18f, 5f) || Near(x, z, 18f, 29f, 20f) ||
                    Near(x, z, GardenCenter.x, GardenCenter.z, 13f) || Near(x, z, BellPos.x, BellPos.z, 6f) ||
                    Near(x, z, 9f, 11f, 18f) || Near(x, z, -13f, 24f, 7f) || Near(x, z, 13f, 22f, 7f) ||
                    Near(x, z, -6f, 38f, 7f) || Near(x, z, 2f, 27f, 5f) || Near(x, z, -12f, 6f, 14f)) continue;
                Tree(s, new Vector3(x, 0f, z), 0.8f + (float)rng.NextDouble() * 0.7f);
            }

            // Flores
            Color[] fc = { new Color(1f, 0.85f, 0.35f), new Color(0.95f, 0.50f, 0.65f), new Color(0.80f, 0.60f, 0.95f), Color.white };
            for (int i = 0; i < 80; i++)
            {
                float x = Mathf.Lerp(-30f, 30f, (float)rng.NextDouble());
                float z = Mathf.Lerp(-12f, 45f, (float)rng.NextDouble());
                if (Near(x, z, 0f, 0f, 9f) || Near(x, z, GardenCenter.x, GardenCenter.z, 10f)) continue;
                Prim.Ball(s, new Vector3(x, 0.12f, z), Vector3.one * 0.18f, fc[rng.Next(fc.Length)], false, "Flower");
            }
        }

        static bool Near(float x, float z, float cx, float cz, float r)
        {
            return (x - cx) * (x - cx) + (z - cz) * (z - cz) < r * r;
        }

        public static void Tree(Transform parent, Vector3 pos, float scale)
        {
            GameObject t = new GameObject("Tree");
            t.transform.SetParent(parent, false);
            t.transform.position = pos;
            Prim.Cyl(t.transform, new Vector3(0f, 1.1f * scale, 0f), new Vector3(0.55f, 1.1f, 0.55f) * scale, new Color(0.50f, 0.34f, 0.22f), true, "Trunk");
            Prim.Ball(t.transform, new Vector3(0f, 3.1f * scale, 0f), new Vector3(3.0f, 2.6f, 3.0f) * scale, new Color(0.36f, 0.66f, 0.34f), false, "Leaves");
            Prim.Ball(t.transform, new Vector3(0.7f * scale, 2.5f * scale, 0.4f * scale), new Vector3(2.0f, 1.8f, 2.0f) * scale, new Color(0.42f, 0.72f, 0.38f), false, "Leaves2");
        }
    }
}
