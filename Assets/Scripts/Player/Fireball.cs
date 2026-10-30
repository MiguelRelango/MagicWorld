using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Bola de fuego de la varita: vuela recta (cae muy poco), daña slimes, activa interruptores
    /// y al impactar prende las enredaderas cercanas.
    /// </summary>
    public class Fireball : MonoBehaviour
    {
        const float Speed = 15f;
        const float BlastRadius = 1.7f;

        Vector3 vel;
        float life = 2.6f;
        float emberTimer;
        bool firstFrame = true;
        Light lamp;
        Transform core;

        public static Fireball Spawn(Vector3 pos, Vector3 dir)
        {
            GameObject go = new GameObject("Fireball");
            go.transform.position = pos;
            Fireball f = go.AddComponent<Fireball>();
            f.vel = dir.normalized * Speed;
            f.core = Prim.Make(PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one * 0.34f, new Color(1f, 0.55f, 0.12f), false, "Core", true).transform;
            Prim.Make(PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one * 0.18f, new Color(1f, 0.95f, 0.6f), false, "Heart", true);
            f.lamp = go.AddComponent<Light>();
            f.lamp.type = LightType.Point;
            f.lamp.color = new Color(1f, 0.6f, 0.25f);
            f.lamp.range = 6f;
            f.lamp.intensity = 2f;
            f.lamp.shadows = LightShadows.None;
            return f;
        }

        void Update()
        {
            if (Game.State == GameState.Paused) return;
            float dt = Time.deltaTime;
            life -= dt;
            if (life <= 0f) { Explode(transform.position); return; }

            vel += Vector3.down * 0.6f * dt;
            Vector3 step = vel * dt;
            float len = step.magnitude;

            // Si nace pegada a una enredadera o pared, ya está dentro del collider: explota ahí.
            if (firstFrame)
            {
                firstFrame = false;
                Collider[] inside = Physics.OverlapSphere(transform.position, 0.25f, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < inside.Length; i++)
                {
                    if (!Ignored(inside[i])) { Explode(transform.position); return; }
                }
            }

            // Detonación por proximidad contra enemigos (la bola vuela a media altura).
            for (int i = 0; i < Lockable.All.Count; i++)
            {
                Lockable l = Lockable.All[i];
                if (l != null && l.IsEnemy && Vector3.Distance(l.AimPoint, transform.position) < 0.9f)
                {
                    Explode(transform.position);
                    return;
                }
            }

            if (len > 0.0001f)
            {
                RaycastHit[] hits = Physics.SphereCastAll(transform.position, 0.3f, step.normalized, len + 0.1f, ~0, QueryTriggerInteraction.Collide);
                RaycastHit best = default(RaycastHit);
                float bestD = float.MaxValue;
                for (int i = 0; i < hits.Length; i++)
                {
                    if (Ignored(hits[i].collider) || hits[i].distance >= bestD) continue;
                    best = hits[i];
                    bestD = hits[i].distance;
                }
                if (bestD < float.MaxValue)
                {
                    Explode(transform.position + step.normalized * Mathf.Max(0f, bestD - 0.1f));
                    return;
                }
            }
            transform.position += step;

            float pulse = 0.34f + Mathf.Sin(Time.time * 30f) * 0.04f;
            if (core != null) core.localScale = Vector3.one * pulse;
            if (lamp != null) lamp.intensity = 2f + Mathf.Sin(Time.time * 25f) * 0.4f;

            emberTimer -= dt;
            if (emberTimer <= 0f)
            {
                emberTimer = 0.04f;
                Ember.Spawn(transform.position, -vel.normalized * 0.6f + Random.insideUnitSphere * 0.5f, 0.45f, 0.14f);
            }
        }

        static bool Ignored(Collider hc)
        {
            return hc.GetComponentInParent<PlayerController>() != null
                || hc.GetComponentInParent<Partner>() != null
                || (hc.isTrigger && hc.GetComponentInParent<IHittable>() == null
                                 && hc.GetComponentInParent<EnemySlime>() == null);
        }

        void Explode(Vector3 point)
        {
            Sfx.Play("fire", 0.9f + Random.value * 0.2f, 0.8f);
            Embers(point, 10, 3.2f);

            // Fogonazo breve
            GameObject flash = new GameObject("FireFlash");
            flash.transform.position = point;
            Light l = flash.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.65f, 0.3f);
            l.range = 8f;
            l.intensity = 3.5f;
            l.shadows = LightShadows.None;
            Destroy(flash, 0.18f);

            // Slimes
            Lockable[] snap = Lockable.All.ToArray();
            for (int i = 0; i < snap.Length; i++)
            {
                if (snap[i] == null || !snap[i].IsEnemy) continue;
                if (Vector3.Distance(snap[i].AimPoint, point) > BlastRadius + 0.4f) continue;
                EnemySlime e = snap[i].GetComponent<EnemySlime>();
                if (e != null) e.Hurt(1.5f, point, 4f);
            }

            // Enredaderas y demás material inflamable
            for (int i = Vine.All.Count - 1; i >= 0; i--)
            {
                Vine v = Vine.All[i];
                if (v != null && !v.Burning && Vector3.Distance(v.Center, point) <= BlastRadius + 0.6f) v.Ignite();
            }

            // Interruptores que reaccionan a golpes (p. ej. la campana).
            Collider[] cols = Physics.OverlapSphere(point, 0.8f, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < cols.Length; i++)
            {
                IHittable h = cols[i].GetComponentInParent<IHittable>();
                if (h != null) h.OnHit(point - vel.normalized, 1f);
            }

            Destroy(gameObject);
        }

        /// <summary>Chispas de fuego que salen despedidas desde un punto.</summary>
        public static void Embers(Vector3 pos, int count, float speed)
        {
            for (int i = 0; i < count; i++)
                Ember.Spawn(pos, Random.onUnitSphere * speed * Random.Range(0.4f, 1f) + Vector3.up * 1.2f, Random.Range(0.4f, 0.9f), Random.Range(0.12f, 0.24f));
        }
    }

    /// <summary>Chispa luminosa que sube, se encoge y desaparece.</summary>
    public class Ember : MonoBehaviour
    {
        Vector3 vel;
        float life, total, size;

        public static void Spawn(Vector3 pos, Vector3 vel, float life, float size)
        {
            Color c = Random.value < 0.5f ? new Color(1f, 0.55f, 0.15f) : new Color(1f, 0.85f, 0.35f);
            GameObject go = Prim.Make(PrimitiveType.Sphere, null, pos, Vector3.one * size, c, false, "Ember", true);
            Ember e = go.AddComponent<Ember>();
            e.vel = vel;
            e.life = e.total = life;
            e.size = size;
        }

        void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f) { Destroy(gameObject); return; }
            vel.y += 0.8f * Time.deltaTime;
            vel *= 1f - 1.5f * Time.deltaTime;
            transform.position += vel * Time.deltaTime;
            transform.localScale = Vector3.one * size * (life / total);
        }
    }
}
