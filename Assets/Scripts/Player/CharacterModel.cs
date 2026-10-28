using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Modelo 3D del héroe construido con primitivas. Aplica el aspecto del creador
    /// (cuerpo, peinado, colores) y el nivel de armadura en tiempo real.
    /// </summary>
    public class CharacterModel : MonoBehaviour
    {
        public Transform SwordPivot;
        public Transform ShieldPivot;
        public Transform BowPivot;

        Transform root;
        Transform body;
        Transform bobRoot;
        float bob;

        public void Build(CharacterLook look, int armorLevel, bool showSword, bool showShield, bool showBow)
        {
            if (root != null) Destroy(root.gameObject);
            GameObject r = new GameObject("Model");
            root = r.transform;
            root.SetParent(transform, false);
            root.localScale = look.BodyScale;

            GameObject b = new GameObject("Body");
            body = b.transform;
            body.SetParent(root, false);
            bobRoot = body;

            Color skin = look.SkinCol;
            Color cloth = look.ClothCol;
            Color hair = look.HairCol;

            // Piernas y botas
            Color boots = armorLevel >= 2 ? new Color(0.35f, 0.24f, 0.16f) : new Color(0.45f, 0.32f, 0.22f);
            Prim.Cyl(body, new Vector3(-0.17f, 0.30f, 0f), new Vector3(0.22f, 0.30f, 0.22f), cloth * 0.75f, false, "LegL");
            Prim.Cyl(body, new Vector3(0.17f, 0.30f, 0f), new Vector3(0.22f, 0.30f, 0.22f), cloth * 0.75f, false, "LegR");
            Prim.Box(body, new Vector3(-0.17f, 0.08f, 0.04f), new Vector3(0.26f, 0.16f, 0.34f), boots, false, "BootL");
            Prim.Box(body, new Vector3(0.17f, 0.08f, 0.04f), new Vector3(0.26f, 0.16f, 0.34f), boots, false, "BootR");

            // Torso
            Prim.Cyl(body, new Vector3(0f, 0.88f, 0f), new Vector3(0.62f, 0.38f, 0.46f), cloth, false, "Torso");
            Prim.Box(body, new Vector3(0f, 0.62f, 0f), new Vector3(0.66f, 0.10f, 0.50f), new Color(0.40f, 0.28f, 0.18f), false, "Belt");

            // Brazos
            Prim.Cyl(body, new Vector3(-0.42f, 0.88f, 0f), new Vector3(0.18f, 0.28f, 0.18f), cloth, false, "ArmL");
            Prim.Cyl(body, new Vector3(0.42f, 0.88f, 0f), new Vector3(0.18f, 0.28f, 0.18f), cloth, false, "ArmR");
            Prim.Ball(body, new Vector3(-0.42f, 0.56f, 0f), Vector3.one * 0.18f, skin, false, "HandL");
            Prim.Ball(body, new Vector3(0.42f, 0.56f, 0f), Vector3.one * 0.18f, skin, false, "HandR");

            // Cabeza y rostro
            Prim.Ball(body, new Vector3(0f, 1.50f, 0f), new Vector3(0.52f, 0.54f, 0.52f), skin, false, "Head");
            Prim.Ball(body, new Vector3(-0.11f, 1.53f, 0.22f), Vector3.one * 0.07f, new Color(0.1f, 0.1f, 0.12f), false, "EyeL");
            Prim.Ball(body, new Vector3(0.11f, 1.53f, 0.22f), Vector3.one * 0.07f, new Color(0.1f, 0.1f, 0.12f), false, "EyeR");

            BuildHair(look.Hair, hair);
            BuildArmor(armorLevel, cloth);

            // Armas
            GameObject sp = new GameObject("SwordPivot");
            SwordPivot = sp.transform;
            SwordPivot.SetParent(root, false);
            SwordPivot.localPosition = new Vector3(0.45f, 0.62f, 0.10f);
            Prim.Box(SwordPivot, new Vector3(0f, 0f, 0.05f), new Vector3(0.07f, 0.07f, 0.20f), new Color(0.45f, 0.30f, 0.18f), false, "Grip");
            Prim.Box(SwordPivot, new Vector3(0f, 0f, 0.17f), new Vector3(0.26f, 0.05f, 0.05f), new Color(0.80f, 0.68f, 0.30f), false, "Guard");
            Prim.Box(SwordPivot, new Vector3(0f, 0f, 0.62f), new Vector3(0.08f, 0.03f, 0.88f), new Color(0.82f, 0.86f, 0.92f), false, "Blade");
            SwordPivot.gameObject.SetActive(showSword);

            GameObject sh = new GameObject("ShieldPivot");
            ShieldPivot = sh.transform;
            ShieldPivot.SetParent(root, false);
            ShieldPivot.localPosition = new Vector3(-0.50f, 0.85f, 0.18f);
            Prim.Cyl(ShieldPivot, Vector3.zero, new Vector3(0.62f, 0.04f, 0.62f), new Color(0.30f, 0.45f, 0.75f), false, "Face")
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Prim.Cyl(ShieldPivot, new Vector3(0f, 0f, 0.03f), new Vector3(0.22f, 0.04f, 0.22f), new Color(0.90f, 0.75f, 0.30f), false, "Boss")
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ShieldPivot.gameObject.SetActive(showShield);

            GameObject bp = new GameObject("BowPivot");
            BowPivot = bp.transform;
            BowPivot.SetParent(root, false);
            BowPivot.localPosition = new Vector3(-0.45f, 0.62f, 0.22f);
            Prim.Box(BowPivot, new Vector3(0f, 0f, 0f), new Vector3(0.05f, 0.80f, 0.05f), new Color(0.55f, 0.36f, 0.20f), false, "BowWood");
            Prim.Box(BowPivot, new Vector3(0f, 0f, -0.06f), new Vector3(0.01f, 0.76f, 0.01f), new Color(0.9f, 0.9f, 0.85f), false, "BowString");
            BowPivot.gameObject.SetActive(showBow);
        }

        void BuildHair(int style, Color c)
        {
            switch (style)
            {
                case 0: // Corto
                    Prim.Ball(body, new Vector3(0f, 1.66f, -0.02f), new Vector3(0.56f, 0.34f, 0.56f), c, false, "HairTop");
                    break;
                case 1: // Largo
                    Prim.Ball(body, new Vector3(0f, 1.66f, -0.02f), new Vector3(0.58f, 0.34f, 0.58f), c, false, "HairTop");
                    Prim.Box(body, new Vector3(0f, 1.30f, -0.22f), new Vector3(0.50f, 0.62f, 0.14f), c, false, "HairBack");
                    break;
                case 2: // Moño
                    Prim.Ball(body, new Vector3(0f, 1.66f, -0.02f), new Vector3(0.56f, 0.32f, 0.56f), c, false, "HairTop");
                    Prim.Ball(body, new Vector3(0f, 1.92f, -0.05f), Vector3.one * 0.24f, c, false, "HairBun");
                    break;
                default: // Rapado
                    Prim.Ball(body, new Vector3(0f, 1.62f, 0f), new Vector3(0.53f, 0.24f, 0.53f), c * 0.85f, false, "HairBuzz");
                    break;
            }
        }

        void BuildArmor(int level, Color cloth)
        {
            if (level <= 1) return;
            Color leather = new Color(0.50f, 0.34f, 0.20f);
            Color metal = new Color(0.72f, 0.76f, 0.82f);
            Color gold = new Color(0.92f, 0.76f, 0.30f);

            // Nivel 2: cuero reforzado, hombreras y hebillas
            Prim.Cyl(body, new Vector3(0f, 0.92f, 0f), new Vector3(0.68f, 0.30f, 0.52f), leather, false, "ChestLeather");
            Prim.Ball(body, new Vector3(-0.42f, 1.12f, 0f), new Vector3(0.30f, 0.20f, 0.30f), leather, false, "PauldronL");
            Prim.Ball(body, new Vector3(0.42f, 1.12f, 0f), new Vector3(0.30f, 0.20f, 0.30f), leather, false, "PauldronR");
            Prim.Box(body, new Vector3(0f, 0.92f, 0.24f), new Vector3(0.14f, 0.14f, 0.04f), gold, false, "Buckle");

            if (level < 3) return;

            // Nivel 3: metal pulido, casco y capa
            Prim.Cyl(body, new Vector3(0f, 0.96f, 0f), new Vector3(0.72f, 0.30f, 0.56f), metal, false, "ChestPlate");
            Prim.Ball(body, new Vector3(-0.44f, 1.14f, 0f), new Vector3(0.36f, 0.24f, 0.36f), metal, false, "PlateL");
            Prim.Ball(body, new Vector3(0.44f, 1.14f, 0f), new Vector3(0.36f, 0.24f, 0.36f), metal, false, "PlateR");
            Prim.Ball(body, new Vector3(0f, 1.68f, 0f), new Vector3(0.58f, 0.34f, 0.58f), metal, false, "Helm");
            Prim.Box(body, new Vector3(0f, 0.98f, -0.34f), new Vector3(0.62f, 0.95f, 0.05f), cloth * 0.8f, false, "Cape");

            if (level < 4) return;

            // Nivel 4: ornamentos dorados con brillo
            Prim.Box(body, new Vector3(0f, 1.95f, 0f), new Vector3(0.06f, 0.26f, 0.30f), gold, false, "Crest");
            Prim.Ball(body, new Vector3(0f, 1.0f, 0.28f), Vector3.one * 0.14f, new Color(0.5f, 0.85f, 1f), false, "Gem")
                .GetComponent<Renderer>().sharedMaterial = Prim.Mat(new Color(0.5f, 0.85f, 1f), true);
        }

        public void Animate(float speed01, float dt)
        {
            if (bobRoot == null) return;
            bob += dt * (3f + speed01 * 9f);
            float y = Mathf.Abs(Mathf.Sin(bob)) * 0.06f * speed01;
            bobRoot.localPosition = new Vector3(0f, y, 0f);
            bobRoot.localRotation = Quaternion.Euler(Mathf.Sin(bob) * 4f * speed01, 0f, Mathf.Sin(bob * 0.5f) * 3f * speed01);
        }
    }
}
