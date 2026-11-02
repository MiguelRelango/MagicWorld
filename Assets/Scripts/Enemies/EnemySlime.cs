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
        /// <summary>Variante goblin: más vida, garrote, necesita línea de visión (no te huele a través de las paredes).</summary>
        public bool Goblin;

        public Health Hp;
        CharacterController cc;
        Transform visual;
        Renderer rend;
        Color baseColor = new Color(0.45f, 0.85f, 0.45f);

        // Solo goblin
        Renderer[] gobRends;
        Material[] gobMats;
        Transform clubPivot;
        bool gobLit;
        float lastSeen = -99f;
        float lungeSpeed = 8f;

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
            s.rend.sharedMaterial = WorldLook.Gel(s.baseColor);
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

        /// <summary>Goblin de mazmorra: piel verde, orejas puntiagudas y un garrote.</summary>
        public static EnemySlime SpawnGoblin(Vector3 pos, Transform parent)
        {
            GameObject go = new GameObject("Goblin");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            CharacterController cc = go.AddComponent<CharacterController>();
            cc.radius = 0.4f;
            cc.height = 1.5f;
            cc.center = new Vector3(0f, 0.75f, 0f);

            EnemySlime s = go.AddComponent<EnemySlime>();
            s.cc = cc;
            s.Goblin = true;
            s.Speed = 3.4f;
            s.AggroRange = 10f;
            s.lungeSpeed = 6.5f;
            s.home = pos;

            Color skin = new Color(0.42f, 0.66f, 0.28f);
            Color cloth = new Color(0.45f, 0.30f, 0.20f);
            Color dark = new Color(0.20f, 0.14f, 0.10f);
            GameObject v = new GameObject("Visual");
            v.transform.SetParent(go.transform, false);
            s.visual = v.transform;
            Transform t = s.visual;
            Prim.Cyl(t, new Vector3(-0.16f, 0.30f, 0f), new Vector3(0.20f, 0.30f, 0.20f), dark, false, "LegL");
            Prim.Cyl(t, new Vector3(0.16f, 0.30f, 0f), new Vector3(0.20f, 0.30f, 0.20f), dark, false, "LegR");
            Prim.Cyl(t, new Vector3(0f, 0.78f, 0f), new Vector3(0.62f, 0.34f, 0.46f), cloth, false, "Tunic");
            Prim.Cyl(t, new Vector3(-0.40f, 0.82f, 0f), new Vector3(0.15f, 0.26f, 0.15f), skin, false, "ArmL");
            Prim.Ball(t, new Vector3(0f, 1.28f, 0.04f), new Vector3(0.70f, 0.60f, 0.62f), skin, false, "Head");
            Prim.Box(t, new Vector3(-0.42f, 1.34f, 0.02f), new Vector3(0.36f, 0.07f, 0.16f), skin, false, "EarL")
                .transform.localRotation = Quaternion.Euler(0f, 0f, 22f);
            Prim.Box(t, new Vector3(0.42f, 1.34f, 0.02f), new Vector3(0.36f, 0.07f, 0.16f), skin, false, "EarR")
                .transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
            Prim.Ball(t, new Vector3(0f, 1.24f, 0.33f), new Vector3(0.14f, 0.17f, 0.14f), new Color(0.36f, 0.58f, 0.24f), false, "Nose");
            Prim.Ball(t, new Vector3(-0.15f, 1.36f, 0.28f), Vector3.one * 0.10f, new Color(1f, 0.85f, 0.2f), false, "EyeL");
            Prim.Ball(t, new Vector3(0.15f, 1.36f, 0.28f), Vector3.one * 0.10f, new Color(1f, 0.85f, 0.2f), false, "EyeR");

            // Brazo derecho con garrote: pivota en el hombro para el golpe.
            GameObject pivot = new GameObject("ClubPivot");
            pivot.transform.SetParent(t, false);
            pivot.transform.localPosition = new Vector3(0.42f, 1.02f, 0f);
            s.clubPivot = pivot.transform;
            Prim.Cyl(s.clubPivot, new Vector3(0f, -0.20f, 0.05f), new Vector3(0.15f, 0.26f, 0.15f), skin, false, "ArmR");
            Prim.Cyl(s.clubPivot, new Vector3(0f, -0.12f, 0.45f), new Vector3(0.10f, 0.45f, 0.10f), new Color(0.40f, 0.27f, 0.16f), false, "Club")
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Prim.Ball(s.clubPivot, new Vector3(0f, -0.12f, 0.92f), new Vector3(0.26f, 0.26f, 0.26f), new Color(0.34f, 0.22f, 0.13f), false, "ClubHead");

            s.gobRends = s.visual.GetComponentsInChildren<Renderer>();
            s.gobMats = new Material[s.gobRends.Length];
            for (int i = 0; i < s.gobRends.Length; i++) s.gobMats[i] = s.gobRends[i].sharedMaterial;

            Health hp = go.AddComponent<Health>();
            hp.Init(4f);
            hp.InvulTime = 0.12f;
            s.Hp = hp;
            hp.OnDied = s.Die;

            Lockable l = go.AddComponent<Lockable>();
            l.IsEnemy = true;
            l.AimHeight = 0.9f;
            return s;
        }

        void Awake()
        {
            cc = GetComponent<CharacterController>();
        }

        bool HasSight(Transform t)
        {
            Vector3 o = transform.position + Vector3.up * 1.0f;
            Vector3 d = t.position + Vector3.up * 1.0f - o;
            float len = d.magnitude;
            if (len < 1f) return true;
            RaycastHit hit;
            if (Physics.Raycast(o, d / len, out hit, len - 0.4f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<CharacterController>() != null || hit.rigidbody != null) return true;
                return false;
            }
            return true;
        }

        void SetGoblinFlash(bool on)
        {
            if (gobRends == null || on == gobLit) return;
            gobLit = on;
            Material white = Prim.Mat(Color.white, true);
            for (int i = 0; i < gobRends.Length; i++)
                if (gobRends[i] != null) gobRends[i].sharedMaterial = on ? white : gobMats[i];
        }

        void GoblinVisual()
        {
            wobble += Time.deltaTime * 9f;
            bool moving = state == State.Chase;
            float bob = moving ? Mathf.Abs(Mathf.Sin(wobble)) * 0.07f : Mathf.Sin(wobble * 0.3f) * 0.015f;
            Vector3 lp = visual.localPosition;
            lp.y = bob;
            visual.localPosition = lp;
            float lean = state == State.Windup ? -14f : state == State.Lunge ? 18f : 0f;
            visual.localRotation = Quaternion.Slerp(visual.localRotation, Quaternion.Euler(lean, 0f, 0f), 1f - Mathf.Exp(-18f * Time.deltaTime));
            float swing = state == State.Windup ? -150f : state == State.Lunge ? 35f : (state == State.Stunned ? -20f : -25f);
            if (clubPivot != null)
                clubPivot.localRotation = Quaternion.Slerp(clubPivot.localRotation, Quaternion.Euler(swing, 0f, 0f), 1f - Mathf.Exp(-22f * Time.deltaTime));

            bool flash = false;
            if (hurtFlash > 0f) { hurtFlash -= Time.deltaTime; flash = true; }
            else if (state == State.Windup) flash = Mathf.Sin(Time.time * 40f) > 0f;
            SetGoblinFlash(flash);
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
            if (Goblin)
            {
                Pickup.Spawn(transform.position + Vector3.up * 0.3f, PickupKind.Gem, 2);
                if (Random.value < 0.35f) Pickup.Spawn(transform.position + new Vector3(0.5f, 0.3f, 0.2f), Random.value < 0.5f ? PickupKind.Heart : PickupKind.Arrows);
            }
            else Pickup.Spawn(transform.position + Vector3.up * 0.3f, Random.value < 0.5f ? PickupKind.Arrows : PickupKind.Gem);
            Destroy(gameObject);
        }

        Transform PickTarget()
        {
            Transform best = null;
            float bestD = AggroRange;
            if (Game.Player != null)
            {
                float d = Vector3.Distance(transform.position, Game.Player.transform.position);
                if (Goblin && d < AggroRange)
                {
                    // Los goblins necesitan verte; si te pierden de vista siguen tu rastro un momento.
                    if (HasSight(Game.Player.transform)) lastSeen = Time.time;
                    if (Time.time - lastSeen > 2.5f) d = float.MaxValue;
                }
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

            if (Goblin) GoblinVisual();
            else
            {
                wobble += Time.deltaTime * 6f;
                float sq = 1f + Mathf.Sin(wobble) * 0.05f;
                if (state != State.Windup) visual.localScale = new Vector3(1f / sq, sq, 1f / sq);
                if (hurtFlash > 0f)
                {
                    hurtFlash -= Time.deltaTime;
                    rend.sharedMaterial = WorldLook.Gel(Color.white);
                }
                else
                {
                    rend.sharedMaterial = WorldLook.Gel(state == State.Stunned ? new Color(0.7f, 0.7f, 0.95f) : baseColor);
                }
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
                    if (!Goblin)
                    {
                        visual.localScale = new Vector3(1.15f, 0.65f, 1.15f);
                        rend.sharedMaterial = WorldLook.Gel(Mathf.Sin(Time.time * 40f) > 0f ? new Color(1f, 0.55f, 0.35f) : baseColor);
                    }
                    if (target != null)
                    {
                        Vector3 look = Nav.Flat(target.position - transform.position);
                        if (look.sqrMagnitude > 0.01f) lungeDir = look.normalized;
                    }
                    transform.rotation = Quaternion.LookRotation(lungeDir);
                    if (Time.time >= stateUntil) { state = State.Lunge; stateUntil = Time.time + 0.30f; lungeHit = false; }
                    break;
                case State.Lunge:
                    move = lungeDir * lungeSpeed;
                    TryLungeHit();
                    if (Time.time >= stateUntil) { state = State.Recover; stateUntil = Time.time + 0.9f; }
                    break;
                case State.Recover:
                    if (Time.time >= stateUntil) state = State.Idle;
                    break;
                case State.Stunned:
                    if (!Goblin) visual.localScale = new Vector3(1.1f, 0.8f, 1.1f);
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
