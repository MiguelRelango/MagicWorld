using System.Collections.Generic;
using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Crea geometría con primitivas y materiales de color. Toma el shader del
    /// material por defecto de las primitivas, así funciona en URP, HDRP o Built-in.
    /// </summary>
    public static class Prim
    {
        static Shader shader;
        static readonly Dictionary<int, Material> cache = new Dictionary<int, Material>();

        static Shader GetShader()
        {
            if (shader == null)
            {
                GameObject t = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shader = t.GetComponent<Renderer>().sharedMaterial.shader;
                Object.Destroy(t);
            }
            return shader;
        }

        public static void ClearCache()
        {
            cache.Clear();
        }

        public static Material Mat(Color c, bool glow = false)
        {
            Color32 c32 = c;
            int key = (c32.r << 24) ^ (c32.g << 16) ^ (c32.b << 8) ^ c32.a ^ (glow ? 0x5A5A5A : 0);
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(GetShader());
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.12f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.12f);
            if (glow && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * 1.6f);
            }
            cache[key] = m;
            return m;
        }

        public static GameObject Make(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale,
            Color color, bool collider = false, string name = null, bool glow = false)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            if (name != null) go.name = name;
            if (!collider)
            {
                Collider col = go.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);
            }
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color, glow);
            return go;
        }

        public static GameObject Box(Transform parent, Vector3 pos, Vector3 scale, Color color, bool collider = false, string name = null)
        {
            return Make(PrimitiveType.Cube, parent, pos, scale, color, collider, name);
        }

        public static GameObject Ball(Transform parent, Vector3 pos, Vector3 scale, Color color, bool collider = false, string name = null)
        {
            return Make(PrimitiveType.Sphere, parent, pos, scale, color, collider, name);
        }

        public static GameObject Cyl(Transform parent, Vector3 pos, Vector3 scale, Color color, bool collider = false, string name = null)
        {
            return Make(PrimitiveType.Cylinder, parent, pos, scale, color, collider, name);
        }

        public static void Tint(GameObject go, Color c, bool glow = false)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = Mat(c, glow);
        }

        public static void TintAll(GameObject root, Color c, bool glow = false)
        {
            Renderer[] rs = root.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < rs.Length; i++) rs[i].sharedMaterial = Mat(c, glow);
        }
    }
}
