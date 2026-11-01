using System.Collections.Generic;
using UnityEngine;

namespace Hearthbound
{
    // Shared materials and meshes keep the streamed forest inexpensive.
    public static class ForestLook
    {
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        static Mesh canopy;

        public static Material Surface(string texture, bool cutout = false, bool charred = false)
        {
            string key = texture + cutout + charred;
            if (materials.TryGetValue(key, out Material m) && m != null) return m;
            m = new Material(Prim.Mat(Color.white));
            m.name = "Forest " + key;
            Texture2D tex = Resources.Load<Texture2D>("ForestLook/" + texture);
            m.mainTexture = tex;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            Color tint = charred ? new Color(0.12f, 0.10f, 0.08f) : Color.white;
            m.color = tint;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.05f);
            if (cutout)
            {
                m.SetFloat("_AlphaClip", 1f);
                m.SetFloat("_Cutoff", 0.4f);
                m.SetFloat("_Cull", 0f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.SetOverrideTag("RenderType", "TransparentCutout");
                m.renderQueue = 2450;
                m.doubleSidedGI = true;
            }
            materials[key] = m;
            return m;
        }

        public static void Ground(GameObject go)
        {
            const string key = "PrologueGround";
            if (!materials.TryGetValue(key, out Material m) || m == null)
            {
                m = new Material(Surface("Grass"));
                m.mainTextureScale = new Vector2(81f / 8f, 100f / 8f);
                if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", m.mainTextureScale);
                materials[key] = m;
            }
            go.GetComponent<Renderer>().sharedMaterial = m;
        }

        public static GameObject Leaf(Transform parent, Vector3 pos, float size, float angle)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Leaf";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(size, size * 1.2f, size);
            go.transform.localRotation = Quaternion.Euler(12f, angle, angle * 0.15f);
            go.GetComponent<Renderer>().sharedMaterial = Surface("Leaves", true);
            return go;
        }

        public static void Canopy(Transform parent, float scale)
        {
            if (canopy == null)
            {
                var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
                var rng = new System.Random(520);
                for (int i = 0; i < 56; i++)
                {
                    float a = i * 2.399963f;
                    float y = 1f - 2f * (i + 0.5f) / 56f;
                    float r = Mathf.Sqrt(1f - y * y);
                    Vector3 p = new Vector3(Mathf.Cos(a) * r * 1.25f, 3f + y * 1.05f, Mathf.Sin(a) * r * 1.25f);
                    Quaternion q = Quaternion.Euler((float)rng.NextDouble() * 100f - 50f, a * Mathf.Rad2Deg, (float)rng.NextDouble() * 70f - 35f);
                    float s = 0.85f + (float)rng.NextDouble() * 0.55f;
                    Quad(v, uv, t, p, q * Vector3.right * s * 0.5f, q * Vector3.up * s * 0.65f);
                }
                canopy = new Mesh { name = "Forest shared canopy" };
                canopy.SetVertices(v); canopy.SetUVs(0, uv); canopy.SetTriangles(t, 0);
                canopy.RecalculateNormals(); canopy.RecalculateBounds();
            }
            GameObject go = new GameObject("Leaves");
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * scale;
            go.AddComponent<MeshFilter>().sharedMesh = canopy;
            go.AddComponent<MeshRenderer>().sharedMaterial = Surface("Leaves", true);
        }

        static void Quad(List<Vector3> v, List<Vector2> uv, List<int> t, Vector3 p, Vector3 right, Vector3 up)
        {
            int n = v.Count;
            v.Add(p - right - up); v.Add(p + right - up); v.Add(p + right + up); v.Add(p - right + up);
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
            t.Add(n); t.Add(n + 2); t.Add(n + 1); t.Add(n); t.Add(n + 3); t.Add(n + 2);
        }

        // One mesh per patch, no per-blade GameObjects or colliders.
        public static void GrassPatch(Transform parent, int seed, float x0, float z0, float width, float depth,
            int count, System.Func<float, float, float> height, System.Func<float, float, bool> allowed)
        {
            var rng = new System.Random(seed);
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int i = 0; i < count; i++)
            {
                float x = x0 + (float)rng.NextDouble() * width, z = z0 + (float)rng.NextDouble() * depth;
                if (!allowed(x, z)) continue;
                Vector3 p = new Vector3(x, height(x, z) - 0.025f, z);
                for (int b = 0; b < 4; b++)
                {
                    float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float h = 0.20f + (float)rng.NextDouble() * 0.32f;
                    Vector3 side = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.045f;
                    Vector3 center = p + new Vector3((float)rng.NextDouble() * 0.25f, 0, (float)rng.NextDouble() * 0.25f);
                    int n = v.Count;
                    v.Add(center - side); v.Add(center + side); v.Add(center + Vector3.up * h + side * 1.6f);
                    float u = (float)rng.NextDouble();
                    uv.Add(new Vector2(u, 0.25f)); uv.Add(new Vector2(u + 0.03f, 0.25f)); uv.Add(new Vector2(u, 0.7f));
                    t.Add(n); t.Add(n + 2); t.Add(n + 1); t.Add(n); t.Add(n + 1); t.Add(n + 2);
                }
            }
            if (v.Count == 0) return;
            Mesh mesh = new Mesh { name = "Forest grass patch" };
            if (v.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0);
            var normals = new Vector3[v.Count];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;
            mesh.normals = normals; mesh.RecalculateBounds();
            GameObject go = new GameObject("Grass"); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = Surface("Grass");
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<ForestMeshOwner>().OwnedMesh = mesh;
        }
    }
}
