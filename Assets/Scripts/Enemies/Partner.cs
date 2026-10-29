using System.Collections.Generic;
using UnityEngine;

namespace Hearthbound
{
    public enum PartnerOrder { Follow, Wait, Shelter }

    /// <summary>Zona segura donde la pareja espera y no puede ser atacada.</summary>
    public class Refuge : MonoBehaviour
    {
        public static readonly List<Refuge> All = new List<Refuge>();
        public float Radius = 3f;
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
    }

    /// <summary>
    /// Pareja del héroe. Acompaña, cura, recoge materiales y avisa de peligros.
    /// Diseño sin frustración: nunca muere, se desploma y se recupera sola o al llegar el jugador.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class Partner : MonoBehaviour
    {
        public PartnerOrder Order = PartnerOrder.Follow;
        public float Speed = 4.6f;
        public bool Targetable { get { return !Downed && !inRefuge && Order != PartnerOrder.Wait; } }
        public bool Downed { get; private set; }

        public float Morale = 1f;
        public float HealCooldown = 14f;
        public bool CanHeal = true;
        public bool CanCollect = true;

        CharacterController cc;
        Transform visual;
        Renderer dress;
        Color outfit = new Color(0.92f, 0.55f, 0.62f);
        float vy;
        float hitUntil;
        float healReady;
        float downUntil;
        float shelterCheck;
        float bobT;
        bool inRefuge;
        Vector3 waitPoint;
        float lastSay;

        public static Partner Spawn(Vector3 pos, Transform parent)
        {
            GameObject go = new GameObject("Partner");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            CharacterController cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.33f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            Partner p = go.AddComponent<Partner>();
            p.cc = cc;
            p.BuildModel();
            return p;
        }

        void BuildModel()
        {
            GameObject v = new GameObject("Visual");
            v.transform.SetParent(transform, false);
            visual = v.transform;
            Color skin = new Color(0.96f, 0.80f, 0.68f);
            Color hair = new Color(0.55f, 0.28f, 0.16f);
            Prim.Cyl(visual, new Vector3(-0.14f, 0.28f, 0f), new Vector3(0.18f, 0.28f, 0.18f), skin, false, "LegL");
            Prim.Cyl(visual, new Vector3(0.14f, 0.28f, 0f), new Vector3(0.18f, 0.28f, 0.18f), skin, false, "LegR");
            GameObject dr = Prim.Cyl(visual, new Vector3(0f, 0.72f, 0f), new Vector3(0.64f, 0.34f, 0.5f), outfit, false, "Dress");
            dress = dr.GetComponent<Renderer>();
            Prim.Cyl(visual, new Vector3(-0.38f, 0.86f, 0f), new Vector3(0.15f, 0.25f, 0.15f), outfit, false, "ArmL");
            Prim.Cyl(visual, new Vector3(0.38f, 0.86f, 0f), new Vector3(0.15f, 0.25f, 0.15f), outfit, false, "ArmR");
            Prim.Ball(visual, new Vector3(0f, 1.40f, 0f), new Vector3(0.50f, 0.52f, 0.50f), skin, false, "Head");
            Prim.Ball(visual, new Vector3(-0.10f, 1.43f, 0.21f), Vector3.one * 0.07f, new Color(0.1f, 0.1f, 0.12f), false, "EyeL");
            Prim.Ball(visual, new Vector3(0.10f, 1.43f, 0.21f), Vector3.one * 0.07f, new Color(0.1f, 0.1f, 0.12f), false, "EyeR");
            Prim.Ball(visual, new Vector3(0f, 1.55f, -0.03f), new Vector3(0.54f, 0.34f, 0.54f), hair, false, "HairTop");
            Prim.Box(visual, new Vector3(0f, 1.18f, -0.20f), new Vector3(0.46f, 0.55f, 0.14f), hair, false, "HairBack");
            Prim.Ball(visual, new Vector3(0.20f, 1.62f, -0.05f), Vector3.one * 0.16f, new Color(0.95f, 0.85f, 0.35f), false, "Flower");
        }

        void Awake()
        {
            if (cc == null) cc = GetComponent<CharacterController>();
        }

        /// <summary>Golpe recibido: nunca muere. Pierde moral y puede quedar desplomada.</summary>
        public void TakeHit(Vector3 from)
        {
            if (Downed || Time.time < hitUntil) return;
            hitUntil = Time.time + 1.0f;
            Morale = Mathf.Max(0f, Morale - 0.34f);
            Sfx.Play("hurt", 1.5f, 0.6f);
            if (Morale <= 0f)
            {
                Downed = true;
                downUntil = Time.time + 6f;
                Game.Say("Tu pareja se desplomó. ¡Acércate para ayudarla!", 3f);
            }
        }

        void Update()
        {
            if (Game.State != GameState.Playing) return;
            Transform player = Game.Player != null ? Game.Player.transform : null;
            if (player == null) return;

            HandleOrders();
            UpdateDowned(player);

            if (cc.isGrounded && vy < 0f) vy = -2f;
            vy += -24f * Time.deltaTime;

            Vector3 move = Vector3.zero;
            if (!Downed)
            {
                move = Decide(player);
                Morale = Mathf.Min(1f, Morale + Time.deltaTime * 0.05f);
                TryHeal();
                TryCollect();
                Warn();
            }

            cc.Move((move + Vector3.up * vy) * Time.deltaTime);
            Animate(move.magnitude);
        }

        void HandleOrders()
        {
            if (GameInput.Pressed(Act.Partner))
            {
                Order = (PartnerOrder)(((int)Order + 1) % 3);
                if (Order == PartnerOrder.Wait) waitPoint = transform.position;
                string txt = Order == PartnerOrder.Follow ? "Tu pareja te sigue" :
                             Order == PartnerOrder.Wait ? "Tu pareja espera aquí" : "Tu pareja busca refugio";
                Game.Say(txt, 2f);
                Sfx.Play("ui");
            }
        }

        void UpdateDowned(Transform player)
        {
            if (!Downed) return;
            float d = Vector3.Distance(Nav.Flat(player.position), Nav.Flat(transform.position));
            if (d < 2f || Time.time >= downUntil || NoEnemiesNear())
            {
                Downed = false;
                Morale = 0.6f;
                Game.Say("¡Tu pareja se recuperó!", 2f);
                Sfx.Play("heal");
            }
            visual.localRotation = Quaternion.Slerp(visual.localRotation, Quaternion.Euler(80f, 0f, 0f), 1f - Mathf.Exp(-8f * Time.deltaTime));
            visual.localPosition = Vector3.Lerp(visual.localPosition, new Vector3(0f, 0.2f, 0f), 1f - Mathf.Exp(-8f * Time.deltaTime));
        }

        bool NoEnemiesNear()
        {
            for (int i = 0; i < Lockable.All.Count; i++)
            {
                if (!Lockable.All[i].IsEnemy) continue;
                if (Vector3.Distance(Lockable.All[i].transform.position, transform.position) < 9f) return false;
            }
            return true;
        }

        Vector3 Decide(Transform player)
        {
            // Refugio: si hay enemigos cerca y existe un refugio, va allí.
            Refuge shelter = ClosestRefuge();
            bool danger = !NoEnemiesNear();
            inRefuge = shelter != null && Vector3.Distance(Nav.Flat(shelter.transform.position), Nav.Flat(transform.position)) < shelter.Radius;

            Vector3 target = transform.position;
            float stop = 0.3f;

            if (Order == PartnerOrder.Wait)
            {
                target = waitPoint;
            }
            else if (Order == PartnerOrder.Shelter && shelter != null)
            {
                target = shelter.transform.position;
            }
            else if (danger && shelter != null && Morale < 0.7f)
            {
                target = shelter.transform.position;
            }
            else
            {
                Vector3 back = player.position - player.forward * 2.2f + player.right * 1.2f;
                float d = Vector3.Distance(Nav.Flat(transform.position), Nav.Flat(player.position));
                target = d > 3.2f ? player.position : transform.position;
                if (d > 3.2f) stop = 2.4f;
                else target = transform.position;
                if (d > 22f) { Warp(back); return Vector3.zero; }
            }

            Vector3 to = Nav.Flat(target - transform.position);
            if (to.magnitude < stop) return Vector3.zero;
            Vector3 dir = Nav.Steer(transform.position, to, 0.35f);
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 1f - Mathf.Exp(-12f * Time.deltaTime));
            float speed = Speed * (to.magnitude > 8f ? 1.4f : 1f);
            return dir * speed;
        }

        Refuge ClosestRefuge()
        {
            Refuge best = null;
            float bd = 40f;
            for (int i = 0; i < Refuge.All.Count; i++)
            {
                float d = Vector3.Distance(transform.position, Refuge.All[i].transform.position);
                if (d < bd) { bd = d; best = Refuge.All[i]; }
            }
            return best;
        }

        void Warp(Vector3 pos)
        {
            cc.enabled = false;
            transform.position = pos;
            cc.enabled = true;
        }

        void TryHeal()
        {
            if (!CanHeal || Game.Player == null || Time.time < healReady) return;
            Health h = Game.Player.GetComponent<Health>();
            if (h == null || h.Dead) return;
            if (h.Hp <= h.Max * 0.4f && Vector3.Distance(transform.position, Game.Player.transform.position) < 6f)
            {
                h.Heal(2f);
                healReady = Time.time + HealCooldown;
                Game.Say("Tu pareja te curó", 2f);
                Sfx.Play("heal");
            }
        }

        void TryCollect()
        {
            // Pasivo: acercarse al jugador ya recoge; además atrae objetos cercanos.
            if (!CanCollect) return;
            Pickup[] ps = FindObjectsByType<Pickup>(FindObjectsSortMode.None);
            for (int i = 0; i < ps.Length; i++)
            {
                if (Vector3.Distance(ps[i].transform.position, transform.position) < 1.3f)
                {
                    ps[i].Pull(Game.Player.transform.position, Time.deltaTime * 8f);
                }
            }
        }

        void Warn()
        {
            if (Time.time - lastSay < 12f) return;
            for (int i = 0; i < Lockable.All.Count; i++)
            {
                Lockable l = Lockable.All[i];
                if (!l.IsEnemy) continue;
                float d = Vector3.Distance(l.transform.position, Game.Player.transform.position);
                if (d > 8f && d < 13f)
                {
                    Game.Say("¡Cuidado! Hay slimes cerca", 2.2f);
                    lastSay = Time.time;
                    return;
                }
            }
        }

        void Animate(float speed)
        {
            if (Downed) return;
            bobT += Time.deltaTime * (3f + speed * 1.6f);
            float y = Mathf.Abs(Mathf.Sin(bobT)) * 0.05f * Mathf.Clamp01(speed);
            visual.localPosition = Vector3.Lerp(visual.localPosition, new Vector3(0f, y, 0f), 0.4f);
            visual.localRotation = Quaternion.Slerp(visual.localRotation, Quaternion.identity, 1f - Mathf.Exp(-8f * Time.deltaTime));
        }
    }
}
