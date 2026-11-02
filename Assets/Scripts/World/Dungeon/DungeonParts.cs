using UnityEngine;

namespace Hearthbound
{
    /// <summary>Antorcha de pared: llama que parpadea y luz que solo se enciende cerca del jugador.</summary>
    public class Torch : MonoBehaviour
    {
        public float GateRange = 28f;
        Light lamp;
        Transform flame;
        Transform flameCore;
        float seed;
        float baseIntensity = 2.2f;
        float nextGate;

        /// <param name="worldPos">Punto de la pared donde se apoya la antorcha.</param>
        /// <param name="outward">Dirección horizontal que sale de la pared hacia la sala.</param>
        public static Torch Create(Transform parent, Vector3 worldPos, Vector3 outward, bool withLight)
        {
            GameObject go = new GameObject("Torch");
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            if (outward.sqrMagnitude < 0.001f) outward = Vector3.forward;
            go.transform.rotation = Quaternion.LookRotation(outward, Vector3.up);
            Transform t = go.transform;

            Color wood = new Color(0.34f, 0.22f, 0.13f);
            Color iron = new Color(0.20f, 0.19f, 0.20f);
            Prim.Box(t, new Vector3(0f, 0f, 0.03f), new Vector3(0.26f, 0.38f, 0.06f), iron, false, "Plate");
            Prim.Cyl(t, new Vector3(0f, 0.08f, 0.17f), new Vector3(0.07f, 0.32f, 0.07f), wood, false, "Stick");
            Prim.Cyl(t, new Vector3(0f, 0.43f, 0.17f), new Vector3(0.16f, 0.07f, 0.16f), iron, false, "Cup");
            GameObject f = Prim.Make(PrimitiveType.Sphere, t, new Vector3(0f, 0.66f, 0.17f), new Vector3(0.24f, 0.40f, 0.24f), new Color(1f, 0.52f, 0.12f), false, "Flame", true);
            GameObject c = Prim.Make(PrimitiveType.Sphere, t, new Vector3(0f, 0.60f, 0.17f), new Vector3(0.13f, 0.24f, 0.13f), new Color(1f, 0.92f, 0.55f), false, "FlameCore", true);
            f.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            c.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Torch torch = go.AddComponent<Torch>();
            torch.flame = f.transform;
            torch.flameCore = c.transform;
            torch.seed = Random.value * 100f;
            if (withLight)
            {
                GameObject lg = new GameObject("TorchLight");
                lg.transform.SetParent(t, false);
                lg.transform.localPosition = new Vector3(0f, 0.8f, 0.55f);
                torch.lamp = lg.AddComponent<Light>();
                torch.lamp.type = LightType.Point;
                torch.lamp.color = new Color(1f, 0.62f, 0.28f);
                torch.lamp.range = 10f;
                torch.lamp.intensity = torch.baseIntensity;
                torch.lamp.shadows = LightShadows.None;
            }
            return torch;
        }

        void Update()
        {
            float t = Time.time * 9f + seed;
            float n = 0.5f + 0.5f * Mathf.Sin(t) * Mathf.Sin(t * 0.37f + 1.3f);
            if (flame != null) flame.localScale = new Vector3(0.24f, 0.34f + n * 0.12f, 0.24f);
            if (flameCore != null) flameCore.localScale = new Vector3(0.13f, 0.20f + n * 0.08f, 0.13f);
            if (lamp == null) return;
            if (Time.time >= nextGate)
            {
                nextGate = Time.time + 0.3f;
                bool near = Game.Player != null &&
                    (Game.Player.transform.position - transform.position).sqrMagnitude < GateRange * GateRange;
                lamp.enabled = near;
            }
            lamp.intensity = baseIntensity * (0.85f + n * 0.3f);
        }
    }

    /// <summary>Columna de luz: la "estela" que se abre en la roca y lleva dentro y fuera de la mazmorra.</summary>
    public class PortalBeam : MonoBehaviour
    {
        const int Motes = 14;
        const int Rays = 8;
        float radius = 1f, height = 4f;
        Transform core;
        Transform raysRoot;
        Transform[] motes = new Transform[Motes];
        Light lamp;
        float t;

        public static PortalBeam Create(Transform parent, Vector3 worldPos, float radius, float height, bool skyStreak)
        {
            GameObject go = new GameObject("PortalBeam");
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            PortalBeam p = go.AddComponent<PortalBeam>();
            p.radius = radius;
            p.height = height;
            p.Build(skyStreak);
            return p;
        }

        void Build(bool skyStreak)
        {
            Transform tr = transform;
            Color warm = new Color(1f, 0.90f, 0.55f);
            Color white = new Color(1f, 0.98f, 0.88f);

            Prim.Make(PrimitiveType.Cylinder, tr, new Vector3(0f, 0.04f, 0f), new Vector3(radius * 2.4f, 0.03f, radius * 2.4f), warm, false, "Pad", true);
            core = Prim.Make(PrimitiveType.Cylinder, tr, new Vector3(0f, height * 0.5f, 0f), new Vector3(radius * 0.9f, height * 0.5f, radius * 0.9f), white, false, "Core", true).transform;

            GameObject rr = new GameObject("Rays");
            rr.transform.SetParent(tr, false);
            raysRoot = rr.transform;
            for (int i = 0; i < Rays; i++)
            {
                float a = i * Mathf.PI * 2f / Rays;
                Prim.Make(PrimitiveType.Cube, raysRoot, new Vector3(Mathf.Cos(a) * radius * 0.85f, height * 0.5f, Mathf.Sin(a) * radius * 0.85f),
                    new Vector3(0.10f, height, 0.10f), warm, false, "Ray", true);
            }
            for (int i = 0; i < Motes; i++)
                motes[i] = Prim.Make(PrimitiveType.Cube, tr, Vector3.zero, Vector3.one * 0.12f, white, false, "Mote", true).transform;

            if (skyStreak)
            {
                // Una estela fina que sube al cielo, para ver desde lejos dónde está la entrada.
                Prim.Make(PrimitiveType.Cube, tr, new Vector3(0f, height + 22f, 0f), new Vector3(0.28f, 44f, 0.28f), warm, false, "Streak", true);
            }

            GameObject lg = new GameObject("PortalLight");
            lg.transform.SetParent(tr, false);
            lg.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            lamp = lg.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = new Color(1f, 0.93f, 0.68f);
            lamp.range = 16f;
            lamp.intensity = 3.2f;
            lamp.shadows = LightShadows.None;
        }

        void Update()
        {
            t += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(t * 3.2f) * 0.08f;
            if (core != null) core.localScale = new Vector3(radius * 0.9f * pulse, height * 0.5f, radius * 0.9f * pulse);
            if (raysRoot != null) raysRoot.localRotation = Quaternion.Euler(0f, t * 70f, 0f);
            for (int i = 0; i < Motes; i++)
            {
                if (motes[i] == null) continue;
                float u = Mathf.Repeat(t * 0.45f + i / (float)Motes, 1f);
                float a = t * 1.4f + i * 1.9f;
                float r = radius * (0.55f + 0.5f * Mathf.Sin(i * 2.3f));
                motes[i].localPosition = new Vector3(Mathf.Cos(a) * r, u * height, Mathf.Sin(a) * r);
                motes[i].localRotation = Quaternion.Euler(a * 40f, a * 57f, 0f);
            }
            if (lamp != null) lamp.intensity = 3.2f * pulse;
        }
    }

    /// <summary>Aldeano prisionero: una vez liberado te sigue hasta la salida de luz.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class DungeonVillager : MonoBehaviour
    {
        public bool Following;
        CharacterController cc;
        float vy;

        public static DungeonVillager Attach(GameObject npc)
        {
            // El NPC trae un collider de cápsula para las interacciones; aquí se mueve con CharacterController.
            CapsuleCollider cap = npc.GetComponent<CapsuleCollider>();
            if (cap != null) Destroy(cap);
            CharacterController c = npc.AddComponent<CharacterController>();
            c.radius = 0.35f;
            c.height = 1.7f;
            c.center = new Vector3(0f, 0.9f, 0f);
            DungeonVillager v = npc.AddComponent<DungeonVillager>();
            v.cc = c;
            return v;
        }

        void Update()
        {
            if (!Following || cc == null || Game.Player == null || Game.State != GameState.Playing) return;
            Vector3 pp = Game.Player.transform.position;
            Vector3 to = Nav.Flat(pp - transform.position);
            float dist = to.magnitude;

            if (dist > 30f)
            {
                cc.enabled = false;
                transform.position = pp - Game.Player.transform.forward * 2f;
                cc.enabled = true;
                return;
            }

            Vector3 move = Vector3.zero;
            if (dist > 3.0f)
            {
                Vector3 dir = Nav.Steer(transform.position, to, 0.35f);
                move = dir * Mathf.Min(5f, 2.4f + dist * 0.6f);
                if (dir.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 1f - Mathf.Exp(-10f * Time.deltaTime));
            }
            else if (to.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 1f - Mathf.Exp(-4f * Time.deltaTime));
            }

            if (cc.isGrounded && vy < 0f) vy = -2f;
            vy += -24f * Time.deltaTime;
            cc.Move((move + Vector3.up * vy) * Time.deltaTime);
        }
    }
}
