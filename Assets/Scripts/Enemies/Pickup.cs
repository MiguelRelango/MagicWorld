using UnityEngine;

namespace Hearthbound
{
    public enum PickupKind { Arrows, Gem, Bombs, Wood, Heart }

    /// <summary>Objeto recogible que flota y gira. Se recoge al acercarse.</summary>
    public class Pickup : MonoBehaviour
    {
        public PickupKind Kind;
        public int Amount = 1;
        Vector3 basePos;
        float t;
        float born;

        public static Pickup Spawn(Vector3 pos, PickupKind kind, int amount = 1)
        {
            GameObject go = new GameObject("Pickup_" + kind);
            go.transform.position = pos;
            switch (kind)
            {
                case PickupKind.Arrows:
                    Prim.Box(go.transform, Vector3.zero, new Vector3(0.05f, 0.05f, 0.5f), new Color(0.65f, 0.48f, 0.30f), false, "Shaft");
                    Prim.Box(go.transform, new Vector3(0f, 0f, 0.3f), new Vector3(0.12f, 0.12f, 0.12f), new Color(0.85f, 0.88f, 0.92f), false, "Tip");
                    break;
                case PickupKind.Gem:
                    Prim.Make(PrimitiveType.Cube, go.transform, Vector3.zero, Vector3.one * 0.28f, new Color(0.35f, 0.85f, 0.95f), false, "Gem", true)
                        .transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
                    break;
                case PickupKind.Bombs:
                    Prim.Ball(go.transform, Vector3.zero, Vector3.one * 0.38f, new Color(0.15f, 0.15f, 0.18f), false, "Bomb");
                    Prim.Ball(go.transform, new Vector3(0f, 0.25f, 0f), Vector3.one * 0.1f, new Color(1f, 0.6f, 0.2f), false, "Spark");
                    break;
                case PickupKind.Wood:
                    Prim.Cyl(go.transform, Vector3.zero, new Vector3(0.22f, 0.3f, 0.22f), new Color(0.55f, 0.36f, 0.2f), false, "Log")
                        .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    break;
                case PickupKind.Heart:
                    Prim.Ball(go.transform, new Vector3(-0.08f, 0f, 0f), Vector3.one * 0.2f, new Color(0.95f, 0.3f, 0.4f), false, "L");
                    Prim.Ball(go.transform, new Vector3(0.08f, 0f, 0f), Vector3.one * 0.2f, new Color(0.95f, 0.3f, 0.4f), false, "R");
                    break;
            }
            SphereCollider sc = go.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.9f;
            Pickup p = go.AddComponent<Pickup>();
            p.Kind = kind;
            p.Amount = amount;
            p.basePos = pos;
            p.born = Time.time;
            return p;
        }

        /// <summary>La pareja atrae el objeto hacia el jugador.</summary>
        public void Pull(Vector3 to, float step)
        {
            basePos = Vector3.MoveTowards(basePos, to, step);
        }

        void Update()
        {
            t += Time.deltaTime;
            transform.position = basePos + Vector3.up * (0.15f + Mathf.Sin(t * 3f) * 0.08f);
            transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);

            if (Game.Player == null || Time.time - born < 0.4f) return;
            Vector3 d = Game.Player.transform.position + Vector3.up - transform.position;
            if (d.magnitude < 1.4f) Collect();
        }

        void Collect()
        {
            switch (Kind)
            {
                case PickupKind.Arrows: Game.Arrows = Mathf.Min(30, Game.Arrows + Mathf.Max(2, Amount)); Game.Say("+ flechas"); break;
                case PickupKind.Gem: Game.Gems += Amount; Game.Say("+ gema"); break;
                case PickupKind.Bombs: Game.HasBombs = true; Game.Bombs = Mathf.Min(20, Game.Bombs + Amount); Game.Say("+ bombas"); break;
                case PickupKind.Wood: Game.Wood += Amount; Game.Say("+ leña"); break;
                case PickupKind.Heart:
                    Health h = Game.Player.GetComponent<Health>();
                    if (h != null) h.Heal(1f);
                    break;
            }
            Sfx.Play("pickup");
            Destroy(gameObject);
        }
    }
}
