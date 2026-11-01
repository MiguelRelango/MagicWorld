using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Combate del jugador: combo de espada de 3 golpes, escudo con parry,
    /// arco con apuntado y bombas arrojables.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        public PlayerController Pc;
        public CharacterModel Model;
        public Health Hp;

        public bool Blocking { get; private set; }
        public bool Aiming { get; private set; }
        public bool MovementLocked { get { return attackLeft > 0f || recover > 0f; } }
        public float SpeedMultiplier { get { return Aiming ? 0.45f : (Blocking ? 0.55f : 1f); } }
        public int ComboStep { get; private set; }

        const float ParryWindow = 0.22f;
        const float AttackTime = 0.30f;
        const float ComboWindow = 0.45f;
        const float SwordRange = 2.1f;
        const float SwordArc = 110f;

        float attackLeft;
        float recover;
        float comboUntil;
        float blockStart;
        float bowCharge;
        float bowReady;
        float bombReady;
        float wandReady;
        float wandShow;
        float noManaToast;
        float swing;
        bool hitDone;

        void Awake()
        {
            if (Hp == null) Hp = GetComponent<Health>();
            if (Pc == null) Pc = GetComponent<PlayerController>();
            Hp.Block = IncomingBlocked;
        }

        bool IncomingBlocked(Vector3 from)
        {
            if (Game.GodMode) return true;              // modo dev: invulnerable
            if (Pc != null && Pc.Dodging) return true; // invulnerable durante la esquiva
            if (!Blocking || !Game.HasShield) return false;
            Vector3 to = Nav.Flat(from - transform.position);
            if (Vector3.Angle(transform.forward, to) > 80f) return false;
            bool parry = Time.time - blockStart <= ParryWindow;
            if (parry)
            {
                Game.Mana = Mathf.Min(Game.MaxMana, Game.Mana + 20f);
                Sfx.Play("parry");
                Game.Say("¡Parry!", 0.8f);
                StunNear(from);
            }
            else
            {
                Sfx.Play("shield");
            }
            return true;
        }

        void StunNear(Vector3 from)
        {
            for (int i = 0; i < Lockable.All.Count; i++)
            {
                EnemySlime e = Lockable.All[i].GetComponent<EnemySlime>();
                if (e != null && Vector3.Distance(e.transform.position, transform.position) < 3.2f)
                    e.Stun(1.6f);
            }
        }

        void Update()
        {
            if (Game.State != GameState.Playing || Hp.Dead) return;

            float dt = Time.deltaTime;
            if (attackLeft > 0f) attackLeft -= dt;
            if (recover > 0f) recover -= dt;
            if (Time.time > comboUntil) ComboStep = 0;

            if (Game.HasWand) Game.Mana = Mathf.Min(Game.MaxMana, Game.Mana + 4f * dt);
            if (Game.InfMana) Game.Mana = Game.MaxMana;
            if (Game.InfAmmo)
            {
                if (Game.HasBow) Game.Arrows = Mathf.Max(Game.Arrows, 30);
                if (Game.HasBombs) Game.Bombs = Mathf.Max(Game.Bombs, 20);
            }
            if (wandShow > 0f) wandShow -= dt;

            UpdateSwing(dt);
            UpdateShield();
            UpdateBow(dt);
            UpdateBomb();
            UpdateWand();
            UpdateSword();
        }

        void UpdateSword()
        {
            if (!Game.HasSword || Blocking || Aiming) return;
            if (GameInput.Pressed(Act.Attack) && attackLeft <= 0f && !Pc.Dodging)
            {
                Pc.SnapToward(4.5f, 70f);
                ComboStep = (Time.time <= comboUntil) ? (ComboStep % 3) + 1 : 1;
                attackLeft = AttackTime;
                recover = ComboStep == 3 ? 0.18f : 0f;
                comboUntil = Time.time + ComboWindow;
                swing = 0f;
                hitDone = false;
                Sfx.Play("swing", 0.9f + ComboStep * 0.12f);
            }

            if (attackLeft > 0f && !hitDone && attackLeft < AttackTime * 0.6f)
            {
                hitDone = true;
                DoSwordHit(ComboStep == 3);
            }
        }

        void DoSwordHit(bool heavy)
        {
            float dmg = heavy ? 2f : 1f;
            bool any = false;
            Lockable[] snap = Lockable.All.ToArray(); // copia: un enemigo muerto sale de la lista
            for (int i = 0; i < snap.Length; i++)
            {
                Lockable l = snap[i];
                if (l == null || !l.IsEnemy) continue;
                Vector3 to = Nav.Flat(l.transform.position - transform.position);
                float d = to.magnitude;
                if (d > SwordRange + (heavy ? 0.4f : 0f)) continue;
                if (d > 0.3f && Vector3.Angle(transform.forward, to) > SwordArc * 0.5f) continue;
                EnemySlime e = l.GetComponent<EnemySlime>();
                if (e != null)
                {
                    e.Hurt(dmg, transform.position, heavy ? 8f : 4.5f);
                    any = true;
                }
            }
            if (any)
            {
                if (Game.HasWand) Game.Mana = Mathf.Min(Game.MaxMana, Game.Mana + 10f);
                Sfx.Play("hit", 1f + ComboStep * 0.1f);
                RechargeShake();
            }

            // Los interruptores de golpe (puzles) también reaccionan.
            Collider[] cols = Physics.OverlapSphere(transform.position + transform.forward * 1.3f + Vector3.up, 1.2f, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < cols.Length; i++)
            {
                IHittable h = cols[i].GetComponentInParent<IHittable>();
                if (h != null) h.OnHit(transform.position, dmg);
            }
        }

        void RechargeShake()
        {
        }

        void UpdateShield()
        {
            bool want = Game.HasShield && GameInput.Held(Act.Block) && attackLeft <= 0f && !Aiming && !Pc.Dodging;
            if (want && !Blocking) blockStart = Time.time;
            Blocking = want;
        }

        void UpdateBow(float dt)
        {
            if (!Game.HasBow) { Aiming = false; return; }
            if (attackLeft > 0f || Blocking || Pc.Dodging) { Aiming = false; bowCharge = 0f; return; }

            if (GameInput.Held(Act.Bow) && Game.Arrows > 0)
            {
                Aiming = true;
                bowCharge = Mathf.Min(1f, bowCharge + dt / 0.7f);
                Vector3 dir = AimDirection();
                if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir);
            }
            else if (Aiming)
            {
                Aiming = false;
                if (Time.time >= bowReady && bowCharge > 0.15f) FireArrow();
                bowCharge = 0f;
            }
        }

        public Vector3 AimDirection()
        {
            // Con fijado apunta al enemigo; si no, hacia donde mira.
            if (Pc.LockTarget != null)
            {
                Vector3 d = Nav.Flat(Pc.LockTarget.transform.position - transform.position);
                if (d.sqrMagnitude > 0.01f) return d.normalized;
            }
            // Cámara sobre el hombro: se apunta hacia donde mira la cámara.
            if (CameraRig.Mode == CameraMode.Shoulder && Pc.Rig != null) return Pc.Rig.Forward;
            Vector2 m = GameInput.Move();
            Vector3 want = Pc.Rig != null ? Pc.Rig.Forward * m.y + Pc.Rig.Right * m.x : Vector3.zero;
            if (want.sqrMagnitude > 0.1f) return want.normalized;
            return transform.forward;
        }

        void FireArrow()
        {
            Game.Arrows--;
            bowReady = Time.time + 0.35f;
            Vector3 origin = transform.position + Vector3.up * 1.15f + transform.forward * 0.6f;
            Vector3 dir = transform.forward;
            if (Pc.LockTarget != null)
            {
                dir = (Pc.LockTarget.AimPoint - origin).normalized;
            }
            else if (CameraRig.Mode == CameraMode.Shoulder && Pc.Rig != null)
            {
                dir = Pc.Rig.transform.forward;   // con la cámara al hombro la flecha sigue la mira
            }
            Arrow.Spawn(origin, dir, 18f + bowCharge * 14f, 1f + bowCharge);
            Sfx.Play("bow", 1f + bowCharge * 0.3f);
        }

        const float WandCost = 18f;

        void UpdateWand()
        {
            if (!Game.HasWand || !GameInput.Pressed(Act.Wand)) return;
            if (attackLeft > 0f || Blocking || Aiming || Pc.Dodging || Time.time < wandReady) return;
            if (Game.Mana < WandCost)
            {
                if (Time.time >= noManaToast) { noManaToast = Time.time + 1.2f; Game.Say("Sin maná: golpea con la espada para recargar", 1.8f); Sfx.Play("ui", 0.6f, 0.5f); }
                return;
            }

            Game.Mana -= WandCost;
            wandReady = Time.time + 0.45f;
            wandShow = 0.5f;

            Vector3 face = AimDirection();
            if (face.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(Nav.Flat(face).sqrMagnitude > 0.001f ? Nav.Flat(face) : transform.forward);

            Vector3 origin = transform.position + Vector3.up * 1.0f + transform.forward * 0.75f;
            Fireball.Spawn(origin, WandDirection(origin));
            Sfx.Play("wand", 1f, 0.7f);
        }

        /// <summary>Dirección del hechizo: al objetivo fijado, a la mira de la cámara (hombro) o hacia delante.</summary>
        Vector3 WandDirection(Vector3 origin)
        {
            if (Pc.LockTarget != null) return (Pc.LockTarget.AimPoint - origin).normalized;
            if (CameraRig.Mode == CameraMode.Shoulder && Pc.Rig != null)
            {
                Transform cam = Pc.Rig.transform;
                Vector3 fwd = cam.forward;
                // El rayo empieza a la altura del jugador para no chocar con él ni con la cámara.
                Vector3 start = cam.position + fwd * Mathf.Max(0.5f, Vector3.Dot(transform.position - cam.position, fwd));
                Vector3 point = start + fwd * 30f;
                RaycastHit hit;
                if (Physics.Raycast(start, fwd, out hit, 30f, ~0, QueryTriggerInteraction.Ignore)) point = hit.point;
                Vector3 d = (point - origin).normalized;
                return Vector3.Dot(d, fwd) > 0.5f ? d : fwd;
            }
            return transform.forward;
        }

        void UpdateBomb()
        {
            if (!Game.HasBombs || Game.Bombs <= 0) return;
            if (GameInput.Pressed(Act.Bomb) && Time.time >= bombReady && !Pc.Dodging && attackLeft <= 0f)
            {
                bombReady = Time.time + 0.5f;
                Game.Bombs--;
                Vector3 origin = transform.position + Vector3.up * 1.2f + transform.forward * 0.5f;
                Vector3 vel = transform.forward * 7.5f + Vector3.up * 5.5f;
                Bomb.Spawn(origin, vel);
                Sfx.Play("swing", 0.7f, 0.5f);
            }
        }

        void UpdateSwing(float dt)
        {
            if (Model == null) return;
            if (Model.SwordPivot != null)
            {
                Model.SwordPivot.gameObject.SetActive(Game.HasSword && !Aiming);
                float t = attackLeft > 0f ? 1f - attackLeft / AttackTime : 1f;
                float sign = (ComboStep % 2 == 1) ? 1f : -1f;
                float angle = attackLeft > 0f ? Mathf.Lerp(-80f * sign, 90f * sign, t) : 0f;
                Model.SwordPivot.localRotation = Quaternion.Euler(attackLeft > 0f ? 0f : 70f, angle, attackLeft > 0f ? 0f : 0f);
                Model.SwordPivot.localPosition = attackLeft > 0f ? new Vector3(0.1f, 0.85f, 0.35f) : new Vector3(0.45f, 0.62f, 0.10f);
            }
            if (Model.ShieldPivot != null)
            {
                Model.ShieldPivot.gameObject.SetActive(Game.HasShield);
                Vector3 target = Blocking ? new Vector3(-0.15f, 0.95f, 0.55f) : new Vector3(-0.50f, 0.85f, 0.18f);
                Model.ShieldPivot.localPosition = Vector3.Lerp(Model.ShieldPivot.localPosition, target, 1f - Mathf.Exp(-18f * dt));
                Quaternion rot = Blocking ? Quaternion.identity : Quaternion.Euler(0f, 70f, 0f);
                Model.ShieldPivot.localRotation = Quaternion.Slerp(Model.ShieldPivot.localRotation, rot, 1f - Mathf.Exp(-18f * dt));
            }
            if (Model.WandPivot != null)
            {
                Model.WandPivot.gameObject.SetActive(Game.HasWand && wandShow > 0f);
                float k = Mathf.Clamp01(wandShow / 0.5f);
                Model.WandPivot.localRotation = Quaternion.Euler(-35f * k, 0f, 0f);
            }
            if (Model.BowPivot != null)
            {
                Model.BowPivot.gameObject.SetActive(Game.HasBow && Aiming);
                Model.BowPivot.localPosition = new Vector3(-0.15f, 0.95f, 0.45f);
            }
        }

        public float BowCharge { get { return bowCharge; } }
    }

    /// <summary>Objetos que reaccionan a golpes de espada o flecha.</summary>
    public interface IHittable
    {
        void OnHit(Vector3 from, float damage);
    }
}
