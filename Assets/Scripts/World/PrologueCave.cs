using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Cueva del prólogo (tutorial de bombas):
    ///  Sala A (segura) -> muro agrietado -> pasillo -> Sala B (foso + interruptor de presión + puerta)
    ///  -> Sala C (emboscada de slimes + roca final) -> alcoba con cofre.
    /// </summary>
    public static partial class PrologueWorld
    {
        public static float CaveStartX = 36f;
        public static Vector3 PitRespawn = new Vector3(63f, 0.2f, 30f);
        public static Vector3[] AmbushPoints = { new Vector3(88f, 0f, 25f), new Vector3(88f, 0f, 35f) };

        static readonly Color Rock = new Color(0.40f, 0.38f, 0.42f);
        static readonly Color RockDark = new Color(0.30f, 0.29f, 0.33f);
        static readonly Color Floor = new Color(0.50f, 0.47f, 0.50f);

        // Bloque de roca sólido entre (x1,z1) y (x2,z2), de profundidad -8 a altura 3.4.
        static void Fill(Transform p, float x1, float z1, float x2, float z2, Color c)
        {
            Prim.Box(p, new Vector3((x1 + x2) * 0.5f, -2.3f, (z1 + z2) * 0.5f),
                new Vector3(x2 - x1, 11.4f, z2 - z1), c, true, "Rock");
        }

        static void BuildCave(PrologueDirector dir)
        {
            Transform c = new GameObject("Cave").transform;
            c.SetParent(Root, false);

            // Suelos (el foso queda entre x=66 y x=70).
            Prim.Box(c, new Vector3(51f, -0.5f, 30f), new Vector3(30f, 1f, 36f), Floor, true, "FloorWest");
            Prim.Box(c, new Vector3(85f, -0.5f, 30f), new Vector3(30f, 1f, 36f), Floor, true, "FloorEast");
            Prim.Box(c, new Vector3(68f, -9f, 30f), new Vector3(6f, 0.5f, 36f), new Color(0.12f, 0.11f, 0.14f), false, "PitBottom");

            // Sala A: x 36..50, z 24..36
            Fill(c, 36f, 36f, 50f, 48f, Rock);
            Fill(c, 36f, 12f, 50f, 24f, Rock);
            // Muro agrietado del pasillo (x 50..51)
            Fill(c, 50f, 32f, 51f, 48f, RockDark);
            Fill(c, 50f, 12f, 51f, 28f, RockDark);
            BreakableWall w1 = BreakableWall.Create(c, new Vector3(50.5f, 1.7f, 30f), new Vector3(1f, 3.4f, 4f), new Color(0.52f, 0.48f, 0.46f));
            w1.OnBroken = dir.OnWallBroken;
            dir.CrackedWall = w1;
            // Pasillo x 51..58, z 28..32
            Fill(c, 51f, 32f, 58f, 48f, Rock);
            Fill(c, 51f, 12f, 58f, 28f, Rock);
            // Sala B: x 58..78, z 20..40
            Fill(c, 58f, 40f, 78f, 48f, Rock);
            Fill(c, 58f, 12f, 78f, 20f, Rock);
            // Puerta entre B y C (x 78..79, z 28..32)
            Fill(c, 78f, 32f, 79f, 48f, RockDark);
            Fill(c, 78f, 12f, 79f, 28f, RockDark);
            // Sala C: x 79..90, z 22..38
            Fill(c, 79f, 38f, 90f, 48f, Rock);
            Fill(c, 79f, 12f, 90f, 22f, Rock);
            // Alcoba: x 90..96, z 27..33
            Fill(c, 90f, 33f, 96f, 48f, Rock);
            Fill(c, 90f, 12f, 96f, 27f, Rock);
            Fill(c, 96f, 12f, 100f, 48f, Rock);
            // Cierres norte y sur para que no se pueda rodear la cueva
            Fill(c, 36f, -40f, 100f, 12f, Rock);
            Fill(c, 36f, 48f, 100f, 60f, Rock);
            // Cierre oeste (junto a la entrada)
            Fill(c, 30f, 36f, 36f, 40f, Rock);
            Fill(c, 30f, 20f, 36f, 24f, Rock);

            // Entrada en arco
            Prim.Box(c, new Vector3(36f, 2.2f, 25f), new Vector3(2f, 4.4f, 2f), new Color(0.50f, 0.48f, 0.50f), true, "ArchL");
            Prim.Box(c, new Vector3(36f, 2.2f, 35f), new Vector3(2f, 4.4f, 2f), new Color(0.50f, 0.48f, 0.50f), true, "ArchR");
            Prim.Box(c, new Vector3(36f, 4.6f, 30f), new Vector3(2.4f, 0.8f, 12f), new Color(0.46f, 0.44f, 0.47f), false, "ArchTop");

            // Interruptor de presión al otro lado del foso y su puerta
            PressurePlate plate = PressurePlate.Create(c, new Vector3(74f, 0f, 30f));
            SlidingDoor door = SlidingDoor.Create(c, new Vector3(78.5f, 1.7f, 30f), new Vector3(1f, 3.4f, 4f), new Color(0.55f, 0.40f, 0.26f));
            // Al pulsar el interruptor aparece un puente sobre el foso y se abre la puerta.
            GameObject bridge = Prim.Box(c, new Vector3(68f, -0.25f, 30f), new Vector3(4.4f, 0.5f, 6f), new Color(0.55f, 0.40f, 0.26f), true, "Bridge");
            bridge.SetActive(false);
            plate.OnPressed = () => { door.Open(); bridge.SetActive(true); dir.OnDoorOpened(); };
            dir.Plate = plate;

            // Roca final que tapa la alcoba
            BreakableWall rock = BreakableWall.Create(c, new Vector3(90.6f, 1.7f, 30f), new Vector3(1.4f, 3.4f, 6f), new Color(0.55f, 0.52f, 0.50f));
            rock.OnBroken = dir.OnRockBroken;
            dir.FinalRock = rock;

            // Cofre
            Chest chest = Chest.Create(c, new Vector3(94.2f, 0f, 30f), -90f);
            chest.OnOpened = dir.OnTreasureOpened;
            dir.Treasure = chest;

            // Refugios para la pareja
            MakeRefuge(c, new Vector3(61f, 0f, 22.5f));
            MakeRefuge(c, new Vector3(82f, 0f, 24f));
            MakeRefuge(c, new Vector3(41f, 0f, 26f));

            // Zonas de eventos
            Zone.Create(c, new Vector3(40f, 0f, 30f), 3f, dir.OnCaveEntered);
            Zone.Create(c, new Vector3(82f, 0f, 30f), 4f, dir.OnAmbush);

            // Decorado: antorchas, cristales y luces
            Torch(c, new Vector3(37f, 0f, 24.3f));
            Torch(c, new Vector3(37f, 0f, 35.7f));
            Torch(c, new Vector3(48f, 0f, 24.3f));
            Torch(c, new Vector3(54f, 0f, 28.3f));
            Torch(c, new Vector3(60f, 0f, 20.3f));
            Torch(c, new Vector3(76f, 0f, 20.3f));
            Torch(c, new Vector3(84f, 0f, 22.3f));
            Torch(c, new Vector3(93f, 0f, 27.3f));
            Crystal(c, new Vector3(44f, 0f, 34.6f), 1.0f);
            Crystal(c, new Vector3(62f, 0f, 38.8f), 1.3f);
            Crystal(c, new Vector3(75f, 0f, 38.6f), 0.9f);
            Crystal(c, new Vector3(86f, 0f, 36.8f), 1.1f);

            PointLight(c, new Vector3(43f, 2.4f, 30f), new Color(1f, 0.75f, 0.45f), 12f);
            PointLight(c, new Vector3(54.5f, 2.4f, 30f), new Color(1f, 0.75f, 0.45f), 9f);
            PointLight(c, new Vector3(63f, 2.6f, 30f), new Color(1f, 0.75f, 0.45f), 13f);
            PointLight(c, new Vector3(74f, 2.6f, 30f), new Color(0.65f, 0.80f, 1f), 13f);
            PointLight(c, new Vector3(84f, 2.6f, 30f), new Color(1f, 0.70f, 0.45f), 13f);
            PointLight(c, new Vector3(93f, 2.4f, 30f), new Color(1f, 0.85f, 0.5f), 8f);
        }

        static void MakeRefuge(Transform parent, Vector3 pos)
        {
            GameObject go = new GameObject("Refuge");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            Prim.Make(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 0.04f, 0f), new Vector3(5.2f, 0.03f, 5.2f), new Color(0.55f, 0.85f, 0.70f), false, "Glow", true);
            Prim.Cyl(go.transform, new Vector3(0f, 1.0f, 0f), new Vector3(0.12f, 1.0f, 0.12f), Wood, false, "Pole");
            Prim.Box(go.transform, new Vector3(0.35f, 1.7f, 0f), new Vector3(0.7f, 0.5f, 0.05f), new Color(0.95f, 0.55f, 0.62f), false, "Banner");
            Refuge r = go.AddComponent<Refuge>();
            r.Radius = 3f;
        }

        static void Torch(Transform parent, Vector3 pos)
        {
            Prim.Cyl(parent, pos + new Vector3(0f, 0.6f, 0f), new Vector3(0.14f, 0.6f, 0.14f), Wood, false, "TorchPole");
            Prim.Make(PrimitiveType.Sphere, parent, pos + new Vector3(0f, 1.35f, 0f), Vector3.one * 0.3f, new Color(1f, 0.6f, 0.2f), false, "Flame", true);
        }

        static void Crystal(Transform parent, Vector3 pos, float scale)
        {
            Color c = new Color(0.45f, 0.85f, 1f);
            GameObject a = Prim.Make(PrimitiveType.Cube, parent, pos + new Vector3(0f, 0.6f * scale, 0f), new Vector3(0.4f, 1.2f, 0.4f) * scale, c, false, "Crystal", true);
            a.transform.localRotation = Quaternion.Euler(8f, 20f, 6f);
            GameObject b = Prim.Make(PrimitiveType.Cube, parent, pos + new Vector3(0.4f * scale, 0.4f * scale, 0.1f), new Vector3(0.3f, 0.8f, 0.3f) * scale, c, false, "Crystal2", true);
            b.transform.localRotation = Quaternion.Euler(-10f, 40f, -14f);
        }

        static void PointLight(Transform parent, Vector3 pos, Color col, float range)
        {
            GameObject go = new GameObject("CaveLight");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = col;
            l.range = range;
            l.intensity = 1.5f;
            l.shadows = LightShadows.None;
        }
    }
}
