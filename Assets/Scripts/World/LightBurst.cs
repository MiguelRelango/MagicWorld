using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Destello de luz que "abre" un muro al romperse: un núcleo brillante que se expande, rayos que
    /// salen en todas direcciones, una luz que se desvanece despacio (deja ver lo que había detrás)
    /// y un fogonazo blanco en pantalla si el jugador está cerca.
    /// </summary>
    public class LightBurst : MonoBehaviour
    {
        const int Beams = 14;

        float size = 1f;
        float t;
        float duration = 1.1f;
        Light lamp;
        float lampPeak;
        Transform core;
        Transform[] beams = new Transform[Beams];
        float[] beamLen = new float[Beams];

        /// <param name="size">1 = destello de pared agrietada; 0.35 = roca pequeña.</param>
        public static LightBurst Spawn(Vector3 pos, float size, bool screenFlash = true)
        {
            GameObject go = new GameObject("LightBurst");
            go.transform.position = pos;
            LightBurst b = go.AddComponent<LightBurst>();
            b.Build(size);

            Sfx.Play("flash", 1f + (1f - Mathf.Clamp01(size)) * 0.4f, Mathf.Clamp(0.4f + size * 0.5f, 0.4f, 1f));

            if (screenFlash && Game.Player != null)
            {
                float d = Vector3.Distance(Game.Player.transform.position, pos);
                float reach = 6f + size * 10f;
                if (d < reach) Game.Flash(Mathf.Lerp(0.35f, 0.9f, Mathf.Clamp01(size)) * Mathf.Lerp(1f, 0.6f, d / reach));
            }
            return b;
        }

        void Build(float s)
        {
            size = Mathf.Max(0.2f, s);
            duration = 0.9f + size * 1.2f;

            Color warm = new Color(1f, 0.93f, 0.70f);
            core = Prim.Make(PrimitiveType.Sphere, transform, Vector3.zero, Vector3.zero, new Color(1f, 0.98f, 0.85f), false, "Core", true).transform;

            for (int i = 0; i < Beams; i++)
            {
                Vector3 dir = Random.onUnitSphere;
                if (dir.y < -0.3f) dir.y = -dir.y;   // pocos rayos hacia el suelo
                GameObject beam = Prim.Make(PrimitiveType.Cube, transform, Vector3.zero, Vector3.zero, warm, false, "Beam", true);
                beam.transform.rotation = Quaternion.LookRotation(dir.normalized);
                beams[i] = beam.transform;
                beamLen[i] = Random.Range(2.5f, 5.0f) * (0.6f + size);
            }

            GameObject lg = new GameObject("Lamp");
            lg.transform.SetParent(transform, false);
            lamp = lg.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = new Color(1f, 0.90f, 0.62f);
            lamp.range = 10f + 14f * size;
            lampPeak = 6f + 8f * size;
            lamp.intensity = lampPeak;
            lamp.shadows = LightShadows.None;
        }

        void Update()
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);

            // Fase rápida de expansión (primer 25 %) y luego desvanecimiento.
            float grow = Mathf.Clamp01(u / 0.25f);
            float fade = u < 0.25f ? 1f : 1f - Mathf.Clamp01((u - 0.25f) / 0.75f);
            float ease = 1f - (1f - grow) * (1f - grow);

            if (core != null) core.localScale = Vector3.one * (0.4f + 2.2f * ease) * size * fade;

            for (int i = 0; i < Beams; i++)
            {
                if (beams[i] == null) continue;
                float len = beamLen[i] * ease;
                float thick = 0.14f * size * fade;
                beams[i].localScale = new Vector3(thick, thick, Mathf.Max(0.001f, len));
                beams[i].localPosition = beams[i].forward * (len * 0.5f);
            }

            if (lamp != null)
            {
                // El pico es breve; después la luz se queda un rato más antes de apagarse.
                float lampFade = u < 0.15f ? 1f : Mathf.Pow(1f - Mathf.Clamp01((u - 0.15f) / 0.85f), 1.6f);
                lamp.intensity = lampPeak * lampFade;
            }

            if (t >= duration) Destroy(gameObject);
        }
    }
}
