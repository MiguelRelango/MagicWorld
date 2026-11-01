using System;
using System.Collections;
using UnityEngine;

namespace Hearthbound
{
    /// <summary>Muro agrietado: se destruye con bombas. Las grietas brillan en naranja.</summary>
    public class BreakableWall : MonoBehaviour
    {
        public Action OnBroken;
        public string BreakMessage = "¡El muro se derrumbó!";
        /// <summary>Tamaño del destello de luz al romperse (1 = muro agrietado, 0.35 = roca pequeña).</summary>
        public float BurstSize = 0.5f;
        bool broken;

        public static BreakableWall Create(Transform parent, Vector3 pos, Vector3 size, Color stone)
        {
            GameObject go = new GameObject("CrackedWall");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            WorldLook.Surface(Prim.Box(go.transform, Vector3.zero, size, stone, true, "Wall"), "Rock", 2f);
            // Grietas luminosas (señal visual consistente del puzle).
            Color crack = new Color(1f, 0.72f, 0.25f);
            // Se dibujan en las dos caras que se ven al acercarse (±X).
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (size.x * 0.5f + 0.01f);
                Prim.Make(PrimitiveType.Cube, go.transform, new Vector3(x, 0.2f, 0f), new Vector3(0.04f, size.y * 0.7f, 0.12f), crack, false, "Crack1", true);
                Prim.Make(PrimitiveType.Cube, go.transform, new Vector3(x, 0.5f, 0.3f), new Vector3(0.04f, 0.1f, 0.5f), crack, false, "Crack2", true);
                Prim.Make(PrimitiveType.Cube, go.transform, new Vector3(x, -0.2f, -0.3f), new Vector3(0.04f, 0.1f, 0.5f), crack, false, "Crack3", true);
            }
            WorldLook.CrackLight(go.transform);
            return go.AddComponent<BreakableWall>();
        }

        public void Break()
        {
            if (broken) return;
            broken = true;
            Sfx.Play("door", 0.8f);
            Game.Say(BreakMessage, 2.5f);
            // Destello de luz: el muro "se abre" con un resplandor.
            LightBurst.Spawn(transform.position + Vector3.up * 0.3f, BurstSize, BurstSize >= 0.6f);
            // Escombros que salen volando.
            for (int i = 0; i < 12; i++)
            {
                GameObject r = Prim.Box(null, transform.position + new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(0f, 1.5f), UnityEngine.Random.Range(-0.5f, 0.5f)),
                    Vector3.one * UnityEngine.Random.Range(0.2f, 0.45f), new Color(0.50f, 0.48f, 0.46f), true, "Rubble");
                WorldLook.Surface(r, "Rock", 0.5f);
                Rigidbody rb = r.AddComponent<Rigidbody>();
                rb.AddExplosionForce(6f, transform.position, 3f, 0.8f, ForceMode.Impulse);
                Destroy(r, 4f);
            }
            if (OnBroken != null) OnBroken();
            Destroy(gameObject);
        }
    }

    /// <summary>Puerta que sube al abrirse. Se abre cuando se activa un interruptor.</summary>
    public class SlidingDoor : MonoBehaviour
    {
        public float OpenHeight = 3.2f;
        Vector3 closedPos;
        bool open;

        public static SlidingDoor Create(Transform parent, Vector3 pos, Vector3 size, Color color)
        {
            GameObject go = new GameObject("Door");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            Prim.Box(go.transform, Vector3.zero, size, color, true, "Slab");
            Prim.Box(go.transform, new Vector3(0f, 0f, size.z * 0.5f + 0.02f), new Vector3(size.x * 0.6f, 0.15f, 0.04f), new Color(0.9f, 0.75f, 0.3f), false, "Trim");
            SlidingDoor d = go.AddComponent<SlidingDoor>();
            d.closedPos = go.transform.position;
            return d;
        }

        public void Open()
        {
            if (open) return;
            open = true;
            Sfx.Play("door");
            StopAllCoroutines();
            StartCoroutine(Slide(closedPos + Vector3.up * OpenHeight));
        }

        IEnumerator Slide(Vector3 to)
        {
            while ((transform.position - to).sqrMagnitude > 0.001f)
            {
                transform.position = Vector3.MoveTowards(transform.position, to, Time.deltaTime * 2.2f);
                yield return null;
            }
        }
    }

    /// <summary>
    /// Interruptor de presión: se activa con una bomba que cae encima, una caja física,
    /// o el propio jugador. Alternativa a la bomba: empujar la caja.
    /// </summary>
    public class PressurePlate : MonoBehaviour, IBombable
    {
        public Action OnPressed;
        public bool Latched = true;
        bool pressed;
        int inside;
        Transform plate;

        public static PressurePlate Create(Transform parent, Vector3 pos, float size = 1.6f)
        {
            GameObject go = new GameObject("PressurePlate");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            Prim.Box(go.transform, new Vector3(0f, 0.05f, 0f), new Vector3(size, 0.1f, size), new Color(0.38f, 0.36f, 0.34f), false, "Base");
            GameObject top = Prim.Make(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.14f, 0f), new Vector3(size * 0.75f, 0.08f, size * 0.75f), new Color(0.95f, 0.65f, 0.25f), false, "Plate", true);
            BoxCollider trig = go.AddComponent<BoxCollider>();
            trig.isTrigger = true;
            trig.center = new Vector3(0f, 0.4f, 0f);
            trig.size = new Vector3(size * 0.8f, 0.8f, size * 0.8f);
            PressurePlate p = go.AddComponent<PressurePlate>();
            p.plate = top.transform;
            return p;
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<Bomb>() != null || other.attachedRigidbody != null ||
                other.GetComponentInParent<PlayerController>() != null)
            {
                inside++;
                Press();
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<Bomb>() != null || other.attachedRigidbody != null ||
                other.GetComponentInParent<PlayerController>() != null)
                inside = Mathf.Max(0, inside - 1);
        }

        public void OnBomb(Vector3 center)
        {
            Press();
        }

        void Press()
        {
            if (pressed) return;
            pressed = true;
            plate.localPosition = new Vector3(0f, 0.07f, 0f);
            Prim.Tint(plate.gameObject, new Color(0.45f, 0.95f, 0.5f), true);
            Sfx.Play("pickup", 0.7f);
            Game.Say("¡Clic! Algo se abrió.", 2.5f);
            if (OnPressed != null) OnPressed();
        }
    }

    /// <summary>Campana/diana a distancia: se activa con una flecha o un golpe.</summary>
    public class BellTarget : MonoBehaviour, IHittable
    {
        public Action OnRung;
        bool rung;

        public static BellTarget Create(Transform parent, Vector3 pos)
        {
            GameObject go = new GameObject("Bell");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            Prim.Cyl(go.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.12f, 0.9f, 0.12f), new Color(0.45f, 0.32f, 0.2f), false, "Post");
            Prim.Box(go.transform, new Vector3(0f, 1.8f, 0f), new Vector3(1.0f, 0.1f, 0.1f), new Color(0.45f, 0.32f, 0.2f), false, "Beam");
            GameObject bell = Prim.Make(PrimitiveType.Sphere, go.transform, new Vector3(0f, 1.45f, 0f), new Vector3(0.6f, 0.55f, 0.6f), new Color(0.95f, 0.78f, 0.30f), false, "Bell", true);
            SphereCollider sc = go.AddComponent<SphereCollider>();
            sc.center = new Vector3(0f, 1.45f, 0f);
            sc.radius = 0.5f;
            Lockable l = go.AddComponent<Lockable>();
            l.AimHeight = 1.45f;
            return go.AddComponent<BellTarget>();
        }

        public void OnHit(Vector3 from, float damage)
        {
            if (rung) return;
            rung = true;
            Sfx.Play("parry", 0.7f);
            Game.Say("¡Ding! La campana sonó.", 2.5f);
            if (OnRung != null) OnRung();
        }
    }

    /// <summary>Cofre que entrega un objeto al abrirse.</summary>
    public class Chest : MonoBehaviour
    {
        public Action OnOpened;
        bool opened;
        Transform lid;

        public static Chest Create(Transform parent, Vector3 pos, float yaw = 0f)
        {
            GameObject go = new GameObject("Chest");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            WorldLook.ChestBody(go.transform);
            GameObject lidPivot = new GameObject("LidPivot");
            lidPivot.transform.SetParent(go.transform, false);
            lidPivot.transform.localPosition = new Vector3(0f, 0.6f, -0.35f);
            WorldLook.Surface(Prim.Box(lidPivot.transform, new Vector3(0f, 0.12f, 0.35f), new Vector3(1.14f, 0.24f, 0.74f), new Color(0.62f, 0.42f, 0.24f), false, "Lid"), "ChestWood", 1.2f);
            Prim.Box(go.transform, new Vector3(0f, 0.52f, 0.36f), new Vector3(0.16f, 0.2f, 0.06f), new Color(0.95f, 0.8f, 0.3f), false, "Lock");
            // Dark keyhole on the front of the golden lock plate.
            Prim.Ball(go.transform, new Vector3(0f, 0.55f, 0.394f), new Vector3(0.052f, 0.052f, 0.008f), new Color(0.025f, 0.02f, 0.015f), false, "KeyholeRound");
            Prim.Box(go.transform, new Vector3(0f, 0.515f, 0.394f), new Vector3(0.024f, 0.055f, 0.008f), new Color(0.025f, 0.02f, 0.015f), false, "KeyholeSlot");
            Prim.Box(lidPivot.transform, new Vector3(0f, -0.007f, 0.35f), new Vector3(0.96f, 0.012f, 0.56f), new Color(0.045f, 0.03f, 0.02f), false, "LidInside");
            WorldLook.ChestBands(go.transform, lidPivot.transform);
            Chest c = go.AddComponent<Chest>();
            c.lid = lidPivot.transform;
            Interactable it = go.AddComponent<Interactable>();
            it.Prompt = "Abrir cofre";
            it.Range = 2.4f;
            it.OnUse = c.Open;
            return c;
        }

        public void Open()
        {
            if (opened) return;
            opened = true;
            GetComponent<Interactable>().Enabled = false;
            Sfx.Play("door", 1.6f, 0.7f);
            StartCoroutine(Anim());
            if (OnOpened != null) OnOpened();
        }

        IEnumerator Anim()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 2f;
                lid.localRotation = Quaternion.Euler(Mathf.Lerp(0f, -110f, t), 0f, 0f);
                yield return null;
            }
        }
    }
}
