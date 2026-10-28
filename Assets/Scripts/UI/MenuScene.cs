using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Escena 3D del menú principal: el porche de la casa al atardecer con el héroe y su pareja.
    /// Se construye lejos del mundo de juego y se oculta al empezar la partida.
    /// </summary>
    public static class MenuScene
    {
        public static readonly Vector3 Origin = new Vector3(400f, 0f, 400f);
        public static Transform Root;
        public static CharacterModel Hero;
        public static Transform HeroT;
        public static int PreviewArmor = 1;

        public static void Build()
        {
            if (Root != null) return;
            Root = new GameObject("MenuScene").transform;

            // Luz cálida de atardecer
            GameObject sun = new GameObject("MenuSun");
            sun.transform.SetParent(Root, false);
            sun.transform.rotation = Quaternion.Euler(22f, 140f, 0f);
            Light l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(1f, 0.72f, 0.45f);
            l.intensity = 1.25f;
            l.shadows = LightShadows.Soft;

            Color grass = new Color(0.50f, 0.74f, 0.36f);
            Prim.Cyl(Root, Origin + new Vector3(0f, -0.5f, 0f), new Vector3(34f, 0.5f, 34f), grass, false, "Ground");
            Prim.Cyl(Root, Origin + new Vector3(0f, 0.02f, 1f), new Vector3(9f, 0.03f, 9f), new Color(0.72f, 0.58f, 0.40f), false, "Yard");

            // Casa de fondo
            Vector3 hp = Origin + new Vector3(0f, 0f, 7f);
            Prim.Box(Root, hp + new Vector3(0f, 2.2f, 0f), new Vector3(10f, 4.4f, 6f), new Color(0.96f, 0.90f, 0.78f), false, "HouseWalls");
            GameObject r1 = Prim.Box(Root, hp + new Vector3(0f, 5.4f, -1.9f), new Vector3(11f, 0.4f, 4.6f), new Color(0.78f, 0.35f, 0.28f), false, "RoofA");
            r1.transform.localRotation = Quaternion.Euler(-30f, 0f, 0f);
            GameObject r2 = Prim.Box(Root, hp + new Vector3(0f, 5.4f, 1.9f), new Vector3(11f, 0.4f, 4.6f), new Color(0.78f, 0.35f, 0.28f), false, "RoofB");
            r2.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
            Prim.Box(Root, hp + new Vector3(0f, 1.3f, -3.02f), new Vector3(1.6f, 2.6f, 0.1f), new Color(0.60f, 0.40f, 0.24f), false, "Door");
            Color glass = new Color(1f, 0.85f, 0.45f);
            Prim.Make(PrimitiveType.Cube, Root, hp + new Vector3(-3.2f, 2.0f, -3.02f), new Vector3(1.6f, 1.3f, 0.1f), glass, false, "WindowL", true);
            Prim.Make(PrimitiveType.Cube, Root, hp + new Vector3(3.2f, 2.0f, -3.02f), new Vector3(1.6f, 1.3f, 0.1f), glass, false, "WindowR", true);
            Prim.Box(Root, hp + new Vector3(4.2f, 5.6f, 0f), new Vector3(1.0f, 2.4f, 1.0f), new Color(0.55f, 0.53f, 0.52f), false, "Chimney");

            // Valla y árboles
            for (int i = -4; i <= 4; i++)
                Prim.Box(Root, Origin + new Vector3(i * 1.8f, 0.45f, -3.2f), new Vector3(0.15f, 0.9f, 0.15f), new Color(0.60f, 0.40f, 0.24f), false, "Post");
            Prim.Box(Root, Origin + new Vector3(0f, 0.7f, -3.2f), new Vector3(15f, 0.1f, 0.1f), new Color(0.60f, 0.40f, 0.24f), false, "Rail");
            PrologueWorld.Tree(Root, Origin + new Vector3(-9f, 0f, 3f), 1.3f);
            PrologueWorld.Tree(Root, Origin + new Vector3(10f, 0f, 2f), 1.1f);
            PrologueWorld.Tree(Root, Origin + new Vector3(-12f, 0f, 10f), 1.5f);

            // Fogata pequeña
            Prim.Make(PrimitiveType.Sphere, Root, Origin + new Vector3(2.8f, 0.3f, -0.8f), Vector3.one * 0.4f, new Color(1f, 0.6f, 0.2f), false, "Fire", true);
            Light fl = new GameObject("MenuFire").AddComponent<Light>();
            fl.transform.SetParent(Root, false);
            fl.transform.position = Origin + new Vector3(2.8f, 1f, -0.8f);
            fl.type = LightType.Point;
            fl.color = new Color(1f, 0.65f, 0.35f);
            fl.range = 8f;
            fl.intensity = 1.4f;

            // Pareja junto a la fogata
            GameObject partner = Npc.Create(Root, Origin + new Vector3(3.8f, 0f, -0.4f), "MenuPartner", new Color(0.92f, 0.55f, 0.62f), new Color(0.55f, 0.28f, 0.16f), false, -120f);
            Object.Destroy(partner.GetComponent<CapsuleCollider>());

            // Héroe
            GameObject hero = new GameObject("MenuHero");
            hero.transform.SetParent(Root, false);
            hero.transform.position = Origin + new Vector3(0f, 0f, 0f);
            HeroT = hero.transform;
            Hero = hero.AddComponent<CharacterModel>();
            RefreshHero();
            Hide();
        }

        public static void RefreshHero()
        {
            if (Hero == null) return;
            Hero.Build(Game.Look, PreviewArmor, true, true, false);
        }

        public static void Show()
        {
            Build();
            Root.gameObject.SetActive(true);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.78f, 0.66f, 0.66f);
            RenderSettings.fog = false;
            RenderSettings.sun = Root.GetComponentInChildren<Light>();
            Camera cam = Camera.main;
            if (cam != null) cam.backgroundColor = new Color(0.98f, 0.70f, 0.52f);
        }

        public static void Hide()
        {
            if (Root != null) Root.gameObject.SetActive(false);
        }
    }
}
