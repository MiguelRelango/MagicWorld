using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Minimapa cuadrado. Una cámara ortográfica cenital renderiza a una textura y el HUD la dibuja
    /// en la esquina superior derecha. Gira con la cámara del juego (arriba = hacia donde miras).
    /// Tecla M (o cruz arriba del mando) cambia el tamaño de la zona visible.
    /// </summary>
    public class Minimap : MonoBehaviour
    {
        public static Minimap Instance;

        static readonly float[] HalfSizes = { 22f, 38f, 64f };   // metros desde el centro al borde
        int sizeIdx = 1;

        Camera cam;
        RenderTexture rt;
        Texture2D white;
        CameraRig mainRig;
        float yaw;
        float half = 38f;

        void Awake()
        {
            Instance = this;
            white = new Texture2D(1, 1);
            white.SetPixel(0, 0, Color.white);
            white.Apply();

            rt = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
            rt.name = "MinimapRT";
            rt.filterMode = FilterMode.Bilinear;

            GameObject go = new GameObject("MinimapCamera");
            go.transform.SetParent(transform, false);
            cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.targetTexture = rt;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.20f, 0.36f, 0.52f);
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 90f;
            cam.depth = -10f;
            cam.allowHDR = false;
            cam.allowMSAA = false;
            cam.useOcclusionCulling = false;
            cam.enabled = false;
        }

        void OnDestroy()
        {
            if (rt != null) { rt.Release(); Destroy(rt); }
            if (Instance == this) Instance = null;
        }

        void LateUpdate()
        {
            bool playing = Game.State == GameState.Playing && Game.Player != null;
            cam.enabled = playing;
            if (!playing) return;

            if (GameInput.Pressed(Act.Map))
            {
                sizeIdx = (sizeIdx + 1) % HalfSizes.Length;
                Sfx.Play("ui", 1.1f, 0.5f);
            }
            half = Mathf.Lerp(half, HalfSizes[sizeIdx], 1f - Mathf.Exp(-8f * Time.deltaTime));

            if (mainRig == null && Camera.main != null) mainRig = Camera.main.GetComponent<CameraRig>();
            yaw = mainRig != null ? mainRig.Yaw : 0f;

            Vector3 p = Game.Player.transform.position;
            cam.orthographicSize = half;
            cam.transform.position = new Vector3(p.x, p.y + 32f, p.z);
            cam.transform.rotation = Quaternion.Euler(90f, yaw, 0f);
        }

        // ---------- Dibujo (se llama desde Hud.OnGUI) ----------

        void Fill(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, white);
            GUI.color = old;
        }

        /// <summary>Posición en el recuadro (0..w, 0..h) de un punto del mundo.</summary>
        Vector2 ToMap(Vector3 world, Vector3 center, float w)
        {
            float rad = yaw * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            float dx = world.x - center.x, dz = world.z - center.z;
            float mx = dx * c - dz * s;
            float my = dx * s + dz * c;
            return new Vector2(w * 0.5f + mx / (2f * half) * w, w * 0.5f - my / (2f * half) * w);
        }

        void Dot(Vector2 p, float size, Color c, float w)
        {
            if (p.x < 2f || p.y < 2f || p.x > w - 2f || p.y > w - 2f) return;
            Fill(new Rect(p.x - size * 0.5f, p.y - size * 0.5f, size, size), c);
        }

        public void Draw(Rect r)
        {
            if (Game.Player == null || rt == null) return;
            float w = r.width;

            // Marco
            Fill(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), new Color(0.95f, 0.78f, 0.4f, 0.95f));
            Fill(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), new Color(0.12f, 0.10f, 0.14f, 1f));

            GUI.BeginGroup(r);
            GUI.DrawTexture(new Rect(0, 0, w, w), rt, ScaleMode.StretchToFill, false);

            Vector3 center = Game.Player.transform.position;

            // Refugios y cofres
            for (int i = 0; i < Refuge.All.Count; i++)
                Dot(ToMap(Refuge.All[i].transform.position, center, w), 7f, new Color(0.45f, 0.95f, 0.65f), w);
            for (int i = 0; i < Interactable.All.Count; i++)
            {
                Interactable it = Interactable.All[i];
                if (!it.Enabled || it.Prompt == null || !it.Prompt.ToLower().Contains("cofre")) continue;
                Dot(ToMap(it.transform.position, center, w), 7f, new Color(1f, 0.85f, 0.25f), w);
            }
            // Enemigos
            for (int i = 0; i < Lockable.All.Count; i++)
            {
                if (!Lockable.All[i].IsEnemy) continue;
                Dot(ToMap(Lockable.All[i].transform.position, center, w), 6f, new Color(0.95f, 0.25f, 0.25f), w);
            }
            // Pareja
            if (Game.Partner != null)
                Dot(ToMap(Game.Partner.transform.position, center, w), 8f, new Color(1f, 0.55f, 0.72f), w);

            // Jugador: rombo blanco con punta amarilla hacia donde mira
            Vector2 pp = ToMap(center, center, w);
            float ang = Game.Player.transform.eulerAngles.y - yaw;
            Matrix4x4 old = GUI.matrix;
            GUIUtility.RotateAroundPivot(ang, pp);
            Fill(new Rect(pp.x - 5f, pp.y - 5f, 10f, 10f), Color.white);
            Fill(new Rect(pp.x - 2f, pp.y - 11f, 4f, 7f), new Color(1f, 0.85f, 0.2f));
            GUI.matrix = old;

            // Norte
            float rad = yaw * Mathf.Deg2Rad;
            Vector2 n = new Vector2(-Mathf.Sin(rad), -Mathf.Cos(rad));   // hacia el norte en pantalla (y crece hacia abajo)
            Vector2 np = new Vector2(w * 0.5f, w * 0.5f) + n * (w * 0.5f - 12f);
            Fill(new Rect(np.x - 8, np.y - 8, 16, 16), new Color(0f, 0f, 0f, 0.6f));
            GUIStyle st = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            st.normal.textColor = Color.white;
            GUI.Label(new Rect(np.x - 8, np.y - 8, 16, 16), "N", st);

            GUI.EndGroup();
        }
    }
}
