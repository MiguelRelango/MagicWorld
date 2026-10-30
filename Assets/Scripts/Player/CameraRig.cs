using UnityEngine;

namespace Hearthbound
{
    public enum CameraMode { Aerial, Shoulder }

    /// <summary>
    /// Cámara en tercera persona con dos modos intercambiables (tecla V u Opciones):
    ///  - Aérea: elevada, para leer puzles y combates.
    ///  - Sobre el hombro: cerca y baja, con el personaje a un lado.
    /// Se orienta con las flechas, el stick derecho o arrastrando con el ratón (botón central, o derecho si se elige),
    /// se reencuadra al fijar un enemigo y evita atravesar paredes.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public static CameraMode Mode = CameraMode.Aerial;

        public Transform Target;
        public float Yaw = 0f;
        public float TurnSpeed = 110f;
        public float Follow = 8f;
        public Lockable LockTarget;

        // Ajustes del jugador por modo (la rueda y el arrastre vertical los modifican).
        float aerialPitch = 48f, shoulderPitch = 14f;
        float aerialDist = 9f, shoulderDist = 4.4f;

        Vector3 focus;
        float curPitch = 48f, curDist = 9f, curOffset, curFocusH = 1.2f, curFov = 50f;
        float placedDist = 9f;
        Camera cam;
        static bool loaded;

        public Vector3 Forward { get { return Quaternion.Euler(0f, Yaw, 0f) * Vector3.forward; } }
        public Vector3 Right { get { return Quaternion.Euler(0f, Yaw, 0f) * Vector3.right; } }
        public float Pitch { get { return curPitch; } }

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (!loaded)
            {
                loaded = true;
                Mode = PlayerPrefs.GetInt("camMode", 0) == 1 ? CameraMode.Shoulder : CameraMode.Aerial;
            }
        }

        public static void Toggle()
        {
            Mode = Mode == CameraMode.Aerial ? CameraMode.Shoulder : CameraMode.Aerial;
            PlayerPrefs.SetInt("camMode", Mode == CameraMode.Shoulder ? 1 : 0);
            Game.Say(Mode == CameraMode.Aerial ? "Cámara aérea" : "Cámara sobre el hombro", 2f);
            Sfx.Play("ui", 1.2f, 0.6f);
        }

        float TargetPitch { get { return Mode == CameraMode.Aerial ? aerialPitch : shoulderPitch; } }
        float TargetDist { get { return Mode == CameraMode.Aerial ? aerialDist : shoulderDist; } }
        float TargetOffset { get { return Mode == CameraMode.Aerial ? 0f : 0.85f; } }
        float TargetFocusH { get { return Mode == CameraMode.Aerial ? 1.2f : 1.55f; } }
        float TargetFov { get { return Mode == CameraMode.Aerial ? 50f : 62f; } }

        public void Snap()
        {
            if (Target == null) return;
            curPitch = TargetPitch;
            curDist = TargetDist;
            curOffset = TargetOffset;
            curFocusH = TargetFocusH;
            curFov = TargetFov;
            focus = Target.position + Vector3.up * curFocusH;
            Place(1f);
        }

        void LateUpdate()
        {
            if (Target == null) return;
            if (Game.State == GameState.Paused) return;
            float dt = Time.deltaTime;

            if (Game.State == GameState.Playing && GameInput.Pressed(Act.CameraMode)) Toggle();

            // ---- Entrada: giro, inclinación y zoom ----
            bool drag = GameInput.DragHeld();
            Vector2 d = drag ? GameInput.PointerDelta() : Vector2.zero;
            float sens = 0.12f * MenuUI.CameraSpeed;
            float axis = GameInput.CameraAxis();
            float pAxis = GameInput.CameraPitchAxis();

            if (LockTarget != null)
            {
                // Mantiene al enemigo en pantalla: gira hacia la línea jugador -> enemigo.
                Vector3 dir = Nav.Flat(LockTarget.transform.position - Target.position);
                if (dir.sqrMagnitude > 0.5f)
                {
                    float want = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                    Yaw = Mathf.LerpAngle(Yaw, want, 1f - Mathf.Exp(-5f * dt));
                }
            }
            else
            {
                Yaw += axis * TurnSpeed * dt + d.x * sens;
            }

            float dp = -d.y * sens - pAxis * 60f * dt;   // ratón arriba / flecha arriba = mirar hacia arriba
            if (Mode == CameraMode.Aerial) aerialPitch = Mathf.Clamp(aerialPitch + dp, 30f, 70f);
            else shoulderPitch = Mathf.Clamp(shoulderPitch + dp, -12f, 50f);

            float scroll = GameInput.Scroll();
            if (Mathf.Abs(scroll) > 0.01f && Game.State == GameState.Playing)
            {
                if (Mode == CameraMode.Aerial) aerialDist = Mathf.Clamp(aerialDist - scroll * 0.8f, 5f, 15f);
                else shoulderDist = Mathf.Clamp(shoulderDist - scroll * 0.4f, 2.4f, 7f);
            }

            // ---- Transición suave entre modos ----
            float k = 1f - Mathf.Exp(-10f * dt);
            curPitch = Mathf.Lerp(curPitch, TargetPitch, 1f - Mathf.Exp(-22f * dt));
            curDist = Mathf.Lerp(curDist, TargetDist, k);
            curOffset = Mathf.Lerp(curOffset, TargetOffset, k);
            curFocusH = Mathf.Lerp(curFocusH, TargetFocusH, k);
            curFov = Mathf.Lerp(curFov, TargetFov, k);
            if (cam != null) cam.fieldOfView = curFov;

            Vector3 desiredFocus = Target.position + Vector3.up * curFocusH;
            if (LockTarget != null)
                desiredFocus = Vector3.Lerp(desiredFocus, LockTarget.AimPoint, Mode == CameraMode.Aerial ? 0.35f : 0.2f);
            focus = Vector3.Lerp(focus, desiredFocus, 1f - Mathf.Exp(-Follow * dt));
            Place(1f - Mathf.Exp(-12f * dt));
        }

        void Place(float k)
        {
            Quaternion rot = Quaternion.Euler(curPitch, Yaw, 0f);
            Vector3 back = rot * Vector3.back;
            Vector3 pivot = focus + Right * curOffset;

            // El hombro no debe atravesar paredes: comprobamos también el desplazamiento lateral.
            RaycastHit side;
            if (Mathf.Abs(curOffset) > 0.05f &&
                Physics.Raycast(focus, Right * Mathf.Sign(curOffset), out side, Mathf.Abs(curOffset) + 0.2f, ~0, QueryTriggerInteraction.Ignore) &&
                side.collider.GetComponentInParent<CharacterController>() == null && side.rigidbody == null)
            {
                pivot = focus + Right * Mathf.Max(0f, side.distance - 0.2f) * Mathf.Sign(curOffset);
            }

            float minDist = Mode == CameraMode.Aerial ? 2.5f : 1.2f;
            float dist = curDist;
            RaycastHit hit;
            if (Physics.SphereCast(pivot, 0.25f, back, out hit, curDist, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<CharacterController>() == null && hit.rigidbody == null)
                    dist = Mathf.Max(minDist, hit.distance - 0.2f);
            }
            placedDist = Mathf.Lerp(placedDist, dist, k);
            transform.position = pivot + back * placedDist;
            transform.rotation = rot;
        }
    }
}
