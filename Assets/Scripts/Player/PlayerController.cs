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
        public float RunSpeed = 8.2f;
        public float DodgeSpeed = 8.0f;
        public float DodgeTime = 0.48f;
        public float DodgeCooldown = 0.65f;
        public float Gravity = -24f;
        /// <summary>Salto bajo (~0.75 m): sirve para cruzar desniveles pequeños, no para escalar.</summary>
        public float JumpSpeed = 6.0f;

        public CameraRig Rig;
        public CharacterModel Model;
        public Health Hp;
        public PlayerCombat Combat;

        public Lockable LockTarget { get; private set; }
        public bool Dodging { get { return dodgeLeft > 0f; } }
        public bool RollInvulnerable { get { float elapsed = DodgeTime - dodgeLeft; return Dodging && elapsed >= 0.04f && elapsed < 0.32f; } }
        public bool Locked { get { return LockTarget != null; } }
        public Vector3 Facing { get { return transform.forward; } }

        CharacterController cc;
        float vy;
        float dodgeLeft;
        float dodgeReady;
        Vector3 dodgeDir;
        Vector3 knock;
        float speedNow;
        float coyote, jumpBuffer;
        public bool Airborne { get { return cc != null && !cc.isGrounded; } }

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            cc.height = 1.9f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.95f, 0f);
            cc.stepOffset = 0.35f;
        }

        public void ConfigureCollision(Vector3 scale)
        {
            if (cc == null) cc = GetComponent<CharacterController>();
            cc.height = 1.95f * scale.y;
            cc.radius = 0.35f * Mathf.Max(scale.x, scale.z);
            cc.center = new Vector3(0f, cc.height * 0.5f, 0f);
            cc.skinWidth = 0.035f;
            cc.stepOffset = 0.28f * scale.y;
            cc.slopeLimit = 48f;
            cc.minMoveDistance = 0f;
            cc.detectCollisions = true;
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
            if (Hp != null && Hp.Dead) { dodgeLeft = 0f; if (Model != null) Model.ResetRoll(); return; }

            if (!CraftingSystem.IsWorking) HandleLock();

            Vector3 input = Vector3.zero;
            Vector2 m = GameInput.Move();
            if (Rig != null)
                input = Rig.Forward * m.y + Rig.Right * m.x;
            else
                input = new Vector3(m.x, 0f, m.y);

            bool busy = CraftingSystem.IsWorking || (Combat != null && Combat.MovementLocked);
            Vector3 move = Vector3.zero;

            if (GameInput.Pressed(Act.Dodge) && Time.time >= dodgeReady && !busy && !Dodging && cc.isGrounded)
            {
                dodgeDir = input.sqrMagnitude > 0.05f ? input.normalized : transform.forward;
                dodgeLeft = DodgeTime;
                dodgeReady = Time.time + Mathf.Max(DodgeCooldown, DodgeTime + 0.08f);
                if (!Locked) transform.rotation = Quaternion.LookRotation(dodgeDir);
                Sfx.Play("swing", 1.4f, 0.5f);
            }

            if (dodgeLeft > 0f)
            {
                float step = Mathf.Min(dodgeLeft, Time.deltaTime);
                dodgeLeft = Mathf.Max(0f, dodgeLeft - Time.deltaTime);
                move = dodgeDir * DodgeSpeed * (step / Mathf.Max(Time.deltaTime, 0.00001f));
                speedNow = 0f;
            }
            else if (!busy)
            {
                bool running = GameInput.Held(Act.Sprint) && input.sqrMagnitude > 0.01f;
                float speed = (running ? RunSpeed : WalkSpeed) * (Combat != null ? Combat.SpeedMultiplier : 1f);
                move = input * speed;
                speedNow = input.magnitude * (running ? 1.5f : 1f);
                Face(input);
            }
            else
            {
                speedNow = 0f;
            }

            if (cc.isGrounded && vy < 0f) vy = -2f;

            // Salto con pequeño margen antes/después de tocar el suelo (más cómodo).
            coyote = cc.isGrounded ? 0.12f : coyote - Time.deltaTime;
            jumpBuffer = GameInput.Pressed(Act.Jump) ? 0.12f : jumpBuffer - Time.deltaTime;
            if (jumpBuffer > 0f && coyote > 0f && !busy && !Dodging)
            {
                vy = JumpSpeed;
                jumpBuffer = 0f;
                coyote = 0f;
                Sfx.Play("jump", 1f, 0.5f);
            }
            vy += Gravity * Time.deltaTime;

            knock = Vector3.Lerp(knock, Vector3.zero, 1f - Mathf.Exp(-9f * Time.deltaTime));
            Vector3 vel = move + knock + Vector3.up * vy;
            cc.Move(vel * Time.deltaTime);

            if (Model != null)
            {
                if (Dodging) Model.AnimateRoll(1f - dodgeLeft / Mathf.Max(DodgeTime, 0.01f), transform.InverseTransformDirection(dodgeDir));
                else Model.Animate(speedNow, Time.deltaTime);
            }
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
            dodgeLeft = 0f;
            knock = Vector3.zero;
            if (Model != null) Model.ResetRoll();
            cc.enabled = false;
            transform.position = pos;
            cc.enabled = true;
            vy = 0f;
        }
    }
}
