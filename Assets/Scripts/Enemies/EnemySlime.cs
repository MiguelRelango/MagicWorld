using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Slime enemigo. Persigue al jugador (prioridad) o a la pareja si está cerca y
    /// el jugador queda lejos. Telegrafía su embestida con un destello.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class EnemySlime : MonoBehaviour
    {
        public float Speed = 2.6f;
        public float AggroRange = 11f;
        public float Damage = 1f;
        public bool Passive;

        public Health Hp;
        CharacterController cc;
        Transform visual;
        Renderer rend;
        Color baseColor = new Color(0.45f, 0.85f, 0.45f);

        enum State { Idle, Chase, Windup, Lunge, Recover, Stunned }
        State state = State.Idle;
        float stateUntil;
        Vector3 lungeDir;
        Vector3 knock;
        float vy;
        float hurtFlash;
        float wobble;
        Vector3 home;
        bool lungeHit;

        public static EnemySlime Spawn(Vector3 pos, Transform parent, bool passive = false, float scale = 1f)
        {
            GameObject go = new GameObject("Slime");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            CharacterController cc = go.AddComponent<CharacterController>();
            cc.radius = 0.45f * scale;
            cc.height = 0.9f * scale;
            cc.center = new Vector3(0f, 0.45f * scale, 0f);

            EnemySlime s = go.AddComponent<EnemySlime>();
            s.cc = cc;
            s.Passive = passive;
            s.home = pos;

            GameObject v = new GameObject("Visual");
            v.transform.SetParent(go.transform, false);
            s.visual = v.transform;
            Prim.Ball(s.visual, new Vector3(0f, 0.40f * scale, 0f), new Vector3(0.9f, 0.75f, 0.9f) * scale, s.baseColor, false, "Gel");
            s.rend = s.visual.GetChild(0).GetComponent<Renderer>();
            Prim.Ball(s.visual, new Vector3(-0.17f * scale, 0.5f * scale, 0.34f * scale), Vector3.one * 0.11f * scale, new Color(0.08f, 0.1f, 0.1f), false, "EyeL");
            Prim.Ball(s.visual, new Vector3(0.17f * scale, 0.5f * scale, 0.34f * scale), Vector3.one * 0.11f * scale, new Color(0.08f, 0.1f, 0.1f), false, "EyeR");

            Health hp = go.AddComponent<Health>();
            hp.Init(passive ? 2f : 3f);
            hp.InvulTime = 0.12f;
            s.Hp = hp;
            hp.OnDied = s.Die;

            Lockable l = go.AddComponent<Lockable>();
            l.IsEnemy = true;
            l.AimHeight = 0.5f * scale;
            return s;
        }

        void Awake()
        {
            cc = GetComponent<CharacterController>();
        }

        public void Stun(float seconds)
        {
            state = State.Stunned;
            stateUntil = Time.time + seconds;
        }

        public void Hurt(float dmg, Vector3 from, float force)
        {
            if (Hp == null || Hp.Dead) return;
            if (Hp.Damage(dmg, from))
            {
                hurtFlash = 0.18f;
                Vector3 d = Nav.Flat(transform.position - from);
                if (d.sqrMagnitude < 0.001f) d = transform.forward;
                knock = d.normalized * force;
                if (state == State.Windup || state == State.Lunge) { state = State.Recover; stateUntil = Time.time + 0.4f; }
            }
        }

        void Die()
        {
            Game.Kills++;
            Sfx.Play("hit", 0.6f);
            Lockable l = GetComponent<Lockable>();
            if (l != null) l.enabled = false;
            Pickup.Spawn(transform.position + Vector3.up * 0.3f, Random.value < 0.5f ? PickupKind.Arrows : PickupKind.Gem);
            Destroy(gameObject);
        }

        Transform PickTarget()
        {
            Transform best = null;
            float bestD = AggroRange;
            if (Game.Player != null)
            {
                float d = Vector3.Distance(transform.position, Game.Player.transform.position);
                if (d < bestD) { best = Game.Player.transform; bestD = d; }
            }
            if (Game.Partner != null && Game.Partner.Targetable)
            {
                float d = Vector3.Distance(transform.position, Game.Partner.transform.position);
                // La pareja solo es objetivo si está muy cerca y el jugador queda lejos.
                bool playerFar = Game.Player == null || Vector3.Distance(transform.position, Game.Player.transform.position) > 6f;
                if (d < 4f && playerFar && d < bestD) { best = Game.Partner.transform; bestD = d; }
            }
            return best;
        }

        void Update()
        {
            if (Game.State != GameState.Playing) return;
            if (Hp.Dead) return;

            wobble += Time.deltaTime * 6f;
            float sq = 1f + Mathf.Sin(wobble) * 0.05f;
            if (state != State.Windup) visual.localScale = new Vector3(1f / sq, sq, 1f / sq);
            if (hurtFlash > 0f)
            {
                hurtFlash -= Time.deltaTime;
                rend.sharedMaterial = Prim.Mat(Color.white);
            }
            else
            {
                rend.sharedMaterial = Prim.Mat(state == State.Stunned ? new Color(0.7f, 0.7f, 0.95f) : baseColor);
            }

            if (cc.isGrounded && vy < 0f) vy = -2f;
            vy += -24f * Time.deltaTime;
            knock = Vector3.Lerp(knock, Vector3.zero, 1f - Mathf.Exp(-8f * Time.deltaTime));

            Vector3 move = Vector3.zero;
            Transform target = Passive ? null : PickTarget();

            switch (state)
            {
                case State.Idle:
                    if (target != null) { state = State.Chase; }
                    break;
                case State.Chase:
                    if (target == null) { state = State.Idle; break; }
                    Vector3 to = Nav.Flat(target.position - transform.position);
                    float dist = to.magnitude;
                    if (dist < 2.4f)
                    {
                        state = State.Windup;
                        stateUntil = Time.time + 0.55f;
                        lungeDir = to.normalized;
                        break;
                    }
                    Vector3 dir = Nav.Steer(transform.position, to, 0.4f);
                    move = dir * Speed;
                    if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);
                    break;
                case State.Windup:
                    // Se agacha y brilla: aviso para esquivar o parar.
                    visual.localScale = new Vector3(1.15f, 0.65f, 1.15f);
                    rend.sharedMaterial = Prim.Mat(Mathf.Sin(Time.time * 40f) > 0f ? new Color(1f, 0.55f, 0.35f) : baseColor);
                    if (target != null)
                    {
                        Vector3 look = Nav.Flat(target.position - transform.position);
                        if (look.sqrMagnitude > 0.01f) lungeDir = look.normalized;
                    }
                    transform.rotation = Quaternion.LookRotation(lungeDir);
                    if (Time.time >= stateUntil) { state = State.Lunge; stateUntil = Time.time + 0.30f; lungeHit = false; }
                    break;
                case State.Lunge:
                    move = lungeDir * 8f;
                    TryLungeHit();
                    if (Time.time >= stateUntil) { state = State.Recover; stateUntil = Time.time + 0.9f; }
                    break;
                case State.Recover:
                    if (Time.time >= stateUntil) state = State.Idle;
                    break;
                case State.Stunned:
                    visual.localScale = new Vector3(1.1f, 0.8f, 1.1f);
                    if (Time.time >= stateUntil) state = State.Idle;
                    break;
            }

            cc.Move((move + knock + Vector3.up * vy) * Time.deltaTime);
        }

        void TryLungeHit()
        {
            if (lungeHit) return;
            if (Game.Player != null && Vector3.Distance(Nav.Flat(transform.position), Nav.Flat(Game.Player.transform.position)) < 1.2f)
            {
                Health ph = Game.Player.GetComponent<Health>();
                if (ph != null && ph.Damage(Damage, transform.position))
                {
                    Game.Player.Knockback(transform.position, 7f);
                    Sfx.Play("hurt");
                }
                lungeHit = true;
            }
            else if (Game.Partner != null && Vector3.Distance(Nav.Flat(transform.position), Nav.Flat(Game.Partner.transform.position)) < 1.2f)
            {
                Game.Partner.TakeHit(transform.position);
                lungeHit = true;
            }
        }
    }
}
