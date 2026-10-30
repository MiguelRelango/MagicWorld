using UnityEngine;

namespace Hearthbound
{
    /// <summary>Esquiva de obstáculos simple (sin NavMesh): prueba la dirección deseada y desvíos.</summary>
    public static class Nav
    {
        static readonly float[] Angles = { 0f, 35f, -35f, 70f, -70f, 110f, -110f };

        public static Vector3 Steer(Vector3 pos, Vector3 dir, float radius, float look = 1.6f)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return Vector3.zero;
            dir.Normalize();
            Vector3 origin = pos + Vector3.up * 0.7f;
            for (int i = 0; i < Angles.Length; i++)
            {
                Vector3 d = Quaternion.Euler(0f, Angles[i], 0f) * dir;
                RaycastHit hit;
                if (!Physics.SphereCast(origin, radius, d, out hit, look, ~0, QueryTriggerInteraction.Ignore))
                    return d;
                // Las pendientes transitables (terreno de la isla) no cuentan como pared.
                if (hit.normal.y > 0.55f) return d;
                // Personajes y objetos físicos no cuentan como pared.
                if (hit.collider.GetComponent<CharacterController>() != null || hit.rigidbody != null)
                    return d;
            }
            return Vector3.zero;
        }

        public static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
