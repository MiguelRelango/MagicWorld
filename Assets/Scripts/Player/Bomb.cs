using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Bomba física arrojable. Rebota, detona con temporizador, daña enemigos,
    /// destruye muros agrietados y presiona interruptores a distancia.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Bomb : MonoBehaviour
    {
        public float Fuse = 2.0f;
        public float Radius = 3.2f;
        public float Damage = 3f;

        Renderer body;
        float born;
        bool exploded;

        public static Bomb Spawn(Vector3 pos, Vector3 velocity)
        {
            GameObject go = new GameObject("Bomb");
            go.transform.position = pos;
            GameObject ball = Prim.Make(PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one * 0.5f, new Color(0.15f, 0.15f, 0.18f), false, "Ball");
            Prim.Cyl(go.transform, new Vector3(0f, 0.30f, 0f), new Vector3(0.08f, 0.06f, 0.08f), new Color(0.55f, 0.4f, 0.25f), false, "Cap");
            Prim.Ball(go.transform, new Vector3(0f, 0.42f, 0f), Vector3.one * 0.10f, new Color(1f, 0.6f, 0.2f), false, "Spark")
                .GetComponent<Renderer>().sharedMaterial = Prim.Mat(new Color(1f, 0.6f, 0.2f), true);

            SphereCollider col = go.AddComponent<SphereCollider>();
            col.radius = 0.25f;
            PhysicsMaterial pm = new PhysicsMaterial("bomb");
            pm.bounciness = 0.35f;
            pm.dynamicFriction = 0.6f;
            pm.staticFriction = 0.6f;
            col.material = pm;

            Rigidbody rb = go.GetComponent<Rigidbody>();
            rb.mass = 1f;
            rb.linearDamping = 0.15f;
            rb.angularDamping = 0.6f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.linearVelocity = velocity;

            Bomb b = go.AddComponent<Bomb>();
            b.body = ball.GetComponent<Renderer>();
            b.born = Time.time;
            return b;
        }

        void Update()
        {
            if (exploded) return;
            float t = Time.time - born;
            // Parpadeo que se acelera antes de explotar.
            float rate = Mathf.Lerp(4f, 18f, t / Fuse);
            bool on = Mathf.Sin(t * rate * 2f) > 0f;
            if (body != null) body.sharedMaterial = Prim.Mat(on ? new Color(0.85f, 0.25f, 0.2f) : new Color(0.15f, 0.15f, 0.18f));
            if (t >= Fuse) Explode();
        }

        void Explode()
        {
            if (exploded) return;
            exploded = true;
            Vector3 c = transform.position;
            Sfx.Play("bomb");

            Collider[] cols = Physics.OverlapSphere(c, Radius, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < cols.Length; i++)
            {
                Collider col = cols[i];
                Vector3 p = col.ClosestPoint(c);

                EnemySlime e = col.GetComponentInParent<EnemySlime>();
                if (e != null) e.Hurt(Damage, c, 9f);

                BreakableWall w = col.GetComponentInParent<BreakableWall>();
                if (w != null) w.Break();

                IBombable bm = col.GetComponentInParent<IBombable>();
                if (bm != null) bm.OnBomb(c);

                PlayerController pc = col.GetComponentInParent<PlayerController>();
                if (pc != null)
                {
                    Health h = pc.GetComponent<Health>();
                    if (h != null) h.Damage(1f, c);
                    pc.Knockback(c, 9f);
                }

                Rigidbody rb = col.attachedRigidbody;
                if (rb != null && rb.gameObject != gameObject)
                    rb.AddExplosionForce(14f, c, Radius, 1.2f, ForceMode.Impulse);
            }

            Boom.Spawn(c, Radius);
            Destroy(gameObject);
        }
    }

    /// <summary>Elementos que reaccionan a una explosión (interruptores, rocas...).</summary>
    public interface IBombable
    {
        void OnBomb(Vector3 center);
    }

    /// <summary>Destello y humo de explosión hecho con primitivas.</summary>
    public class Boom : MonoBehaviour
    {
        float t;
        float radius;
        Transform ball;
        Transform[] puffs;

        public static void Spawn(Vector3 pos, float radius)
        {
            GameObject go = new GameObject("Boom");
            go.transform.position = pos;
            Boom b = go.AddComponent<Boom>();
            b.radius = radius;
            b.ball = Prim.Make(PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one * 0.3f, new Color(1f, 0.7f, 0.2f), false, "Flash", true).transform;
            b.puffs = new Transform[8];
            for (int i = 0; i < b.puffs.Length; i++)
            {
                Vector3 d = Random.onUnitSphere;
                d.y = Mathf.Abs(d.y);
                b.puffs[i] = Prim.Ball(go.transform, d * 0.2f, Vector3.one * 0.3f, new Color(0.45f, 0.42f, 0.40f), false, "Puff").transform;
                b.puffs[i].gameObject.name = "Puff";
            }
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = t / 0.7f;
            if (k >= 1f) { Destroy(gameObject); return; }
            float s = Mathf.Lerp(0.3f, radius * 1.7f, Mathf.Sqrt(k));
            ball.localScale = Vector3.one * s * (1f - k);
            for (int i = 0; i < puffs.Length; i++)
            {
                Vector3 dir = puffs[i].localPosition.normalized;
                if (dir.sqrMagnitude < 0.01f) dir = Vector3.up;
                puffs[i].localPosition += dir * Time.deltaTime * radius * 1.4f;
                puffs[i].localScale = Vector3.one * Mathf.Lerp(0.5f, 0.05f, k) * radius * 0.45f;
            }
        }
    }
}
