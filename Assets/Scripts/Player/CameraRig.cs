using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Cámara en tercera persona elevada. Orbita con las flechas (o stick derecho),
    /// se reencuadra al fijar un enemigo y evita atravesar paredes.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public Transform Target;
        public float Distance = 9f;
        public float Pitch = 48f;
        public float Yaw = 0f;
        public float TurnSpeed = 110f;
        public float Follow = 8f;
        public Lockable LockTarget;

        Vector3 focus;
        float curDistance;
        Camera cam;

        public Vector3 Forward
        {
            get
            {
                Vector3 f = Quaternion.Euler(0f, Yaw, 0f) * Vector3.forward;
                return f;
            }
        }

        public Vector3 Right
        {
            get { return Quaternion.Euler(0f, Yaw, 0f) * Vector3.right; }
        }

        void Awake()
        {
            cam = GetComponent<Camera>();
            curDistance = Distance;
        }

        public void Snap()
        {
            if (Target == null) return;
            focus = Target.position + Vector3.up * 1.2f;
            Place(1f);
        }

        void LateUpdate()
        {
            if (Target == null) return;
            if (Game.State == GameState.Paused) return;

            float axis = GameInput.CameraAxis();
            if (LockTarget != null)
            {
                // Mantiene al enemigo en pantalla: gira hacia la línea jugador -> enemigo.
                Vector3 dir = Nav.Flat(LockTarget.transform.position - Target.position);
                if (dir.sqrMagnitude > 0.5f)
                {
                    float want = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                    Yaw = Mathf.LerpAngle(Yaw, want, 1f - Mathf.Exp(-5f * Time.deltaTime));
                }
            }
            else
            {
                Yaw += axis * TurnSpeed * Time.deltaTime;
            }

            Vector3 desiredFocus = Target.position + Vector3.up * 1.2f;
            if (LockTarget != null)
                desiredFocus = Vector3.Lerp(desiredFocus, LockTarget.AimPoint, 0.35f);
            focus = Vector3.Lerp(focus, desiredFocus, 1f - Mathf.Exp(-Follow * Time.deltaTime));
            Place(1f - Mathf.Exp(-12f * Time.deltaTime));
        }

        void Place(float k)
        {
            Quaternion rot = Quaternion.Euler(Pitch, Yaw, 0f);
            Vector3 back = rot * Vector3.back;
            float dist = Distance;
            RaycastHit hit;
            if (Physics.SphereCast(focus, 0.3f, back, out hit, Distance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<CharacterController>() == null && hit.rigidbody == null)
                    dist = Mathf.Max(2.5f, hit.distance - 0.2f);
            }
            curDistance = Mathf.Lerp(curDistance, dist, k);
            transform.position = focus + back * curDistance;
            transform.rotation = rot;
        }
    }
}
