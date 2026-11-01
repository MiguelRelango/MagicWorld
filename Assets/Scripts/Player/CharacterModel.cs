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
        public Transform WandPivot;
        public Transform AxePivot;

        Transform root;
        Transform body;
        Transform bobRoot;
        CharacterJoints joints;
        Vector3 modelScale = Vector3.one;

        public void Build(CharacterLook look, int armorLevel, bool showSword, bool showShield, bool showBow)
        {
            if (root != null) { root.gameObject.SetActive(false); Destroy(root.gameObject); }
            GameObject r = new GameObject("Model");
            root = r.transform;
            root.SetParent(transform, false);
            modelScale = look.BodyScale;
            root.localScale = modelScale;

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

            // Varita: bastón corto con una gema ígnea en la punta (solo se ve al lanzar).
            GameObject wp = new GameObject("WandPivot");
            WandPivot = wp.transform;
            WandPivot.SetParent(root, false);
            WandPivot.localPosition = new Vector3(0.30f, 0.98f, 0.40f);
            Prim.Box(WandPivot, new Vector3(0f, 0f, 0.22f), new Vector3(0.05f, 0.05f, 0.62f), new Color(0.45f, 0.30f, 0.18f), false, "WandStick");
            Prim.Make(PrimitiveType.Sphere, WandPivot, new Vector3(0f, 0f, 0.58f), Vector3.one * 0.14f, new Color(1f, 0.5f, 0.1f), false, "WandGem", true);
            WandPivot.gameObject.SetActive(false);

            // Hacha: mango de madera y cabeza de metal (solo se ve al talar).
            GameObject ap = new GameObject("AxePivot");
            AxePivot = ap.transform;
            AxePivot.SetParent(root, false);
            AxePivot.localPosition = new Vector3(0.42f, 0.95f, 0.30f);
            Prim.Box(AxePivot, new Vector3(0f, 0f, 0.32f), new Vector3(0.06f, 0.06f, 0.78f), new Color(0.52f, 0.34f, 0.20f), false, "AxeHandle");
            Prim.Box(AxePivot, new Vector3(0f, 0.10f, 0.62f), new Vector3(0.06f, 0.26f, 0.26f), new Color(0.72f, 0.75f, 0.80f), false, "AxeHead");
            AxePivot.gameObject.SetActive(false);

            Transform armature = new GameObject("Armature").transform;
            armature.SetParent(root, false);
            joints = armature.gameObject.AddComponent<CharacterJoints>();
            joints.Build(root, body, this, cloth, skin);
            PlayerController controller = GetComponent<PlayerController>();
            if (controller != null) controller.ConfigureCollision(modelScale);
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

        public void ResetRoll()
        {
            if (joints != null) joints.ClearRoll();
            if (root == null) return;
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = modelScale;
        }

        public void AnimateRoll(float progress, Vector3 localDirection)
        {
            if (root == null) return;
            progress = Mathf.Clamp01(progress);
            if (joints != null) joints.Roll(progress);
            localDirection.y = 0f;
            if (localDirection.sqrMagnitude < 0.001f) localDirection = Vector3.forward;
            Vector3 axis = Vector3.Cross(Vector3.up, localDirection.normalized);
            Quaternion rotation = Quaternion.AngleAxis(360f * progress, axis);
            float tuck = Mathf.Sin(progress * Mathf.PI);
            Vector3 scale = Vector3.Scale(modelScale, new Vector3(1f - 0.15f * tuck, 1f - 0.30f * tuck, 1f - 0.15f * tuck));
            Vector3 pivot = Vector3.Scale(new Vector3(0f, 0.95f, 0f), scale);
            // Rotate around the torso, keeping the visual body above the floor.
            float support = Mathf.Abs((rotation * Vector3.right).y) * 0.5f * scale.x
                + Mathf.Abs((rotation * Vector3.up).y) * 0.95f * scale.y
                + Mathf.Abs((rotation * Vector3.forward).y) * 0.4f * scale.z;
            root.localScale = scale;
            root.localRotation = rotation;
            root.localPosition = Vector3.up * support - rotation * pivot;
            if (bobRoot != null) { bobRoot.localPosition = Vector3.zero; bobRoot.localRotation = Quaternion.identity; }
        }

        public void Animate(float speed01, float dt)
        {
            ResetRoll();
            if (joints != null) joints.Motion(speed01);
        }
    }
}
