using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Movimiento 3D relativo a la cámara, esquiva con invulnerabilidad,
    /// fijado de enemigos estilo Zelda y asistencia de orientación al atacar.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float WalkSpeed = 5.2f;
        public float DodgeSpeed = 11f;
        public float DodgeTime = 0.28f;
        public float DodgeCooldown = 0.45f;
        public float Gravity = -24f;

        public CameraRig Rig;
        public CharacterModel Model;
        public Health Hp;
        public PlayerCombat Combat;

        public Lockable LockTarget { get; private set; }
        public bool Dodging { get { return dodgeLeft > 0f; } }
        public bool Locked { get { return LockTarget != null; } }
        public Vector3 Facing { get { return transform.forward; } }

        CharacterController cc;
        float vy;
        float dodgeLeft;
        float dodgeReady;
        Vector3 dodgeDir;
        Vector3 knock;
        float speedNow;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            cc.height = 1.9f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.95f, 0f);
            cc.stepOffset = 0.35f;
        }

        public void Knockback(Vector3 from, float force)
        {
            Vector3 d = Nav.Flat(transform.position - from);
            if (d.sqrMagnitude < 0.001f) d = -transform.forward;
            knock = d.normalized * force;
        }

        void Update()
        {
            if (Game.State != GameState.Playing) return;
            if (Hp != null && Hp.Dead) return;

            HandleLock();

            Vector3 input = Vector3.zero;
            Vector2 m = GameInput.Move();
            if (Rig != null)
                input = Rig.Forward * m.y + Rig.Right * m.x;
            else
                input = new Vector3(m.x, 0f, m.y);

            bool busy = Combat != null && Combat.MovementLocked;
            Vector3 move = Vector3.zero;

            if (GameInput.Pressed(Act.Dodge) && Time.time >= dodgeReady && !busy)
            {
                dodgeDir = input.sqrMagnitude > 0.05f ? input.normalized : transform.forward;
                dodgeLeft = DodgeTime;
                dodgeReady = Time.time + DodgeCooldown;
                transform.rotation = Quaternion.LookRotation(dodgeDir);
                Sfx.Play("swing", 1.4f, 0.5f);
            }

            if (dodgeLeft > 0f)
            {
                dodgeLeft -= Time.deltaTime;
                move = dodgeDir * DodgeSpeed;
            }
            else if (!busy)
            {
                float speed = WalkSpeed * (Combat != null ? Combat.SpeedMultiplier : 1f);
                move = input * speed;
                speedNow = input.magnitude;
                Face(input);
            }
            else
            {
                speedNow = 0f;
            }

            if (cc.isGrounded && vy < 0f) vy = -2f;
            vy += Gravity * Time.deltaTime;

            knock = Vector3.Lerp(knock, Vector3.zero, 1f - Mathf.Exp(-9f * Time.deltaTime));
            Vector3 vel = move + knock + Vector3.up * vy;
            cc.Move(vel * Time.deltaTime);

            if (Model != null) Model.Animate(Dodging ? 1f : speedNow, Time.deltaTime);
        }

        void Face(Vector3 input)
        {
            Vector3 dir = Vector3.zero;
            if (LockTarget != null)
                dir = Nav.Flat(LockTarget.transform.position - transform.position);
            else if (input.sqrMagnitude > 0.05f)
                dir = input;
            if (dir.sqrMagnitude < 0.001f) return;
            Quaternion want = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, want, 1f - Mathf.Exp(-16f * Time.deltaTime));
        }

        /// <summary>Gira de golpe hacia el enemigo más cercano en la dirección del input (asistencia de ataque).</summary>
        public void SnapToward(float range, float coneDeg)
        {
            Lockable target = LockTarget;
            if (target == null)
            {
                Vector2 m = GameInput.Move();
                Vector3 want = Rig != null ? Rig.Forward * m.y + Rig.Right * m.x : transform.forward;
                if (want.sqrMagnitude < 0.05f) want = transform.forward;
                target = Best(range, want.normalized, coneDeg);
            }
            if (target == null) return;
            Vector3 dir = Nav.Flat(target.transform.position - transform.position);
            if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir.normalized);
        }

        Lockable Best(float range, Vector3 dir, float coneDeg)
        {
            Lockable best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < Lockable.All.Count; i++)
            {
                Lockable l = Lockable.All[i];
                if (!l.IsEnemy) continue;
                Vector3 to = Nav.Flat(l.transform.position - transform.position);
                float d = to.magnitude;
                if (d > range || d < 0.01f) continue;
                float ang = Vector3.Angle(dir, to);
                if (ang > coneDeg) continue;
                float score = d + ang * 0.03f;
                if (score < bestScore) { best = l; bestScore = score; }
            }
            return best;
        }

        void HandleLock()
        {
            if (LockTarget != null)
            {
                float d = Vector3.Distance(transform.position, LockTarget.transform.position);
                if (!LockTarget.isActiveAndEnabled || d > 18f) ClearLock();
            }

            if (GameInput.Pressed(Act.Lock))
            {
                if (LockTarget != null) { ClearLock(); return; }
                Lockable best = null;
                float bestD = 16f;
                for (int i = 0; i < Lockable.All.Count; i++)
                {
                    Lockable l = Lockable.All[i];
                    float d = Vector3.Distance(transform.position, l.transform.position);
                    if (d < bestD) { best = l; bestD = d; }
                }
                if (best != null)
                {
                    LockTarget = best;
                    if (Rig != null) Rig.LockTarget = best;
                    Sfx.Play("ui", 1.3f, 0.6f);
                }
            }
        }

        public void ClearLock()
        {
            LockTarget = null;
            if (Rig != null) Rig.LockTarget = null;
        }

        public void Teleport(Vector3 pos)
        {
            cc.enabled = false;
            transform.position = pos;
            cc.enabled = true;
            vy = 0f;
        }
    }
}
