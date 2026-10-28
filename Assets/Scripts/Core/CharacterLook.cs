using UnityEngine;

namespace Hearthbound
{
    /// <summary>Datos del creador de personajes: cuerpo, peinado y colores.</summary>
    [System.Serializable]
    public class CharacterLook
    {
        public int Body = 1;
        public int Hair = 0;
        public int Skin = 1;
        public int HairColor = 0;
        public int Cloth = 0;

        public static readonly string[] BodyNames = { "Esbelto", "Normal", "Robusto" };
        public static readonly string[] HairNames = { "Corto", "Largo", "Moño", "Rapado" };

        public static readonly Color[] SkinColors =
        {
            new Color(1.00f, 0.87f, 0.75f),
            new Color(0.93f, 0.74f, 0.58f),
            new Color(0.78f, 0.56f, 0.40f),
            new Color(0.55f, 0.37f, 0.26f),
            new Color(0.36f, 0.23f, 0.17f),
        };

        public static readonly Color[] HairColors =
        {
            new Color(0.25f, 0.15f, 0.10f),
            new Color(0.92f, 0.75f, 0.35f),
            new Color(0.80f, 0.30f, 0.18f),
            new Color(0.10f, 0.10f, 0.12f),
            new Color(0.55f, 0.75f, 0.95f),
            new Color(0.88f, 0.88f, 0.90f),
        };

        public static readonly Color[] ClothColors =
        {
            new Color(0.25f, 0.55f, 0.75f),
            new Color(0.78f, 0.35f, 0.30f),
            new Color(0.35f, 0.65f, 0.40f),
            new Color(0.85f, 0.65f, 0.25f),
            new Color(0.60f, 0.42f, 0.75f),
            new Color(0.30f, 0.30f, 0.36f),
        };

        public Color SkinCol { get { return SkinColors[Mathf.Clamp(Skin, 0, SkinColors.Length - 1)]; } }
        public Color HairCol { get { return HairColors[Mathf.Clamp(HairColor, 0, HairColors.Length - 1)]; } }
        public Color ClothCol { get { return ClothColors[Mathf.Clamp(Cloth, 0, ClothColors.Length - 1)]; } }

        public Vector3 BodyScale
        {
            get
            {
                if (Body == 0) return new Vector3(0.88f, 1.06f, 0.88f);
                if (Body == 2) return new Vector3(1.16f, 0.97f, 1.16f);
                return Vector3.one;
            }
        }

        public static int Cycle(int value, int count, int step)
        {
            return ((value + step) % count + count) % count;
        }
    }
}
