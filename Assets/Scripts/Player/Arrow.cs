using UnityEngine;

namespace Hearthbound
{
    /// <summary>Flecha física: daña enemigos y activa interruptores a distancia.</summary>
    public class Arrow : MonoBehaviour
    {
        Vector3 vel;
        float damage;
        float life = 4f;
        bool stuck;

        public static Arrow Spawn(Vector3 pos, Vector3 dir, float speed, float damage)
        {
            GameObject go = new GameObject("Arrow");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(dir);
            Prim.Box(go.transform, new Vector3(0f, 0f, 0f), new Vector3(0.04f, 0.04f, 0.7f), new Color(0.65f, 0.48f, 0.30f), false, "Shaft");
            Prim.Box(go.transform, new Vector3(0f, 0f, 0.38f), new Vector3(0.10f, 0.10f, 0.10f), new Color(0.85f, 0.88f, 0.92f), false, "Tip");
            Prim.Box(go.transform, new Vector3(0f, 0f, -0.32f), new Vector3(0.12f, 0.02f, 0.14f), new Color(0.85f, 0.3f, 0.25f), false, "Fletch");
            Arrow a = go.AddComponent<Arrow>();
            a.vel = dir.normalized * speed;
            a.damage = damage;
            return a;
        }

        void Update()
        {
            if (stuck)
            {
                life -= Time.deltaTime;
                if (life <= 0f) Destroy(gameObject);
                return;
            }

            life -= Time.deltaTime;
            if (life <= 0f) { Destroy(gameObject); return; }

            vel += Vector3.down * 1.5f * Time.deltaTime;
            Vector3 step = vel * Time.deltaTime;
            RaycastHit hit;
            if (Physics.Raycast(transform.position, step.normalized, out hit, step.magnitude + 0.3f, ~0, QueryTriggerInteraction.Collide))
            {
                Collider hc = hit.collider;
                bool skip = hc.GetComponentInParent<PlayerController>() != null
                         || hc.GetComponentInParent<Partner>() != null
                         || (hc.isTrigger && hc.GetComponentInParent<IHittable>() == null
                                          && hc.GetComponentInParent<EnemySlime>() == null);
                if (!skip)
                {
                    OnHit(hit);
                    return;
                }
            }
            transform.position += step;
            if (vel.sqrMagnitude > 0.1f) transform.rotation = Quaternion.LookRotation(vel);
        }

        void OnHit(RaycastHit hit)
        {
            EnemySlime e = hit.collider.GetComponentInParent<EnemySlime>();
            if (e != null)
            {
                e.Hurt(damage, transform.position - vel.normalized, 3f);
                Sfx.Play("hit");
                Destroy(gameObject);
                return;
            }

            IHittable h = hit.collider.GetComponentInParent<IHittable>();
            if (h != null)
            {
                h.OnHit(transform.position - vel.normalized, damage);
                Sfx.Play("hit", 1.4f);
                Destroy(gameObject);
                return;
            }

            transform.position = hit.point;
            stuck = true;
            life = 5f;
        }
    }
}
