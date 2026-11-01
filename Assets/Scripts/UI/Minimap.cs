using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Minimapa cuadrado. Una cámara ortográfica cenital renderiza a una textura y el HUD la dibuja
    /// en la esquina superior derecha. Gira con la cámara del juego (arriba = hacia donde miras).
    /// Tecla N (o cruz izquierda del mando) cambia el tamaño de la zona visible.
    /// El mapa grande (tecla M) usa una segunda cámara que se renderiza a mano mientras el panel está abierto.
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

        // Mapa grande (panel de pantalla completa)
        Camera bigCam;
        RenderTexture bigRt;
        public Vector3 BigCenter;
        public float BigHalf = 70f;
        // Isla: mapa pintado por celdas (de ~7 m a toda la isla, x50). Prólogo: cámara cenital, hasta 130 m.
        public float MinHalf { get { return IslandMap.Active ? 7f : 24f; } }
        public float MaxHalf { get { return IslandMap.Active ? 340f : 130f; } }

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
            if (bigRt != null) { bigRt.Release(); Destroy(bigRt); }
            if (rt != null) { rt.Release(); Destroy(rt); }
            if (Instance == this) Instance = null;
        }

        void LateUpdate()
        {
            bool playing = Game.State == GameState.Playing && Game.Player != null;
            cam.enabled = playing;
            if (!playing) return;

            if (IslandMap.Active) IslandMap.Tick(Game.Player.transform.position);

            if (GameInput.Pressed(Act.MapSize))
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
            return ToMap(world, center, w, yaw, half);
        }

        static Vector2 ToMap(Vector3 world, Vector3 center, float w, float yw, float hf)
        {
            float rad = yw * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            float dx = world.x - center.x, dz = world.z - center.z;
            float mx = dx * c - dz * s;
            float my = dx * s + dz * c;
            return new Vector2(w * 0.5f + mx / (2f * hf) * w, w * 0.5f - my / (2f * hf) * w);
        }

        void Dot(Vector2 p, float size, Color c, float w)
        {
            if (p.x < 2f || p.y < 2f || p.x > w - 2f || p.y > w - 2f) return;
            Fill(new Rect(p.x - size * 0.5f, p.y - size * 0.5f, size, size), c);
        }

        GUIStyle northStyle, smallStyle;

        void EnsureStyles()
        {
            if (northStyle != null) return;
            northStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            northStyle.normal.textColor = Color.white;
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            smallStyle.normal.textColor = Color.white;
        }

        static bool IsChest(Interactable it)
        {
            return it.Enabled && it.Prompt != null && it.Prompt.IndexOf("cofre", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Puntos de interés y jugador sobre un recuadro de lado w (se llama dentro de un GUI.BeginGroup).</summary>
        void DrawMarkers(Vector3 center, float w, float yw, float hf, float k, bool fog = false)
        {
            Vector3 pc = Game.Player.transform.position;

            for (int i = 0; i < Refuge.All.Count; i++)
            {
                if (fog && !IslandMap.IsDiscovered(Refuge.All[i].transform.position)) continue;
                Dot(ToMap(Refuge.All[i].transform.position, center, w, yw, hf), 7f * k, new Color(0.45f, 0.95f, 0.65f), w);
            }
            for (int i = 0; i < Interactable.All.Count; i++)
            {
                Interactable it = Interactable.All[i];
                if (!IsChest(it)) continue;
                if (fog && !IslandMap.IsDiscovered(it.transform.position)) continue;
                Dot(ToMap(it.transform.position, center, w, yw, hf), 7f * k, new Color(1f, 0.85f, 0.25f), w);
            }
            for (int i = 0; i < Lockable.All.Count; i++)
            {
                if (!Lockable.All[i].IsEnemy) continue;
                if (fog && (Lockable.All[i].transform.position - pc).sqrMagnitude > 3600f) continue;   // solo los que están cerca
                Dot(ToMap(Lockable.All[i].transform.position, center, w, yw, hf), 6f * k, new Color(0.95f, 0.25f, 0.25f), w);
            }
            if (Game.Partner != null)
                Dot(ToMap(Game.Partner.transform.position, center, w, yw, hf), 8f * k, new Color(1f, 0.55f, 0.72f), w);

            // Jugador: rombo blanco con punta amarilla hacia donde mira
            Vector2 pp = ToMap(pc, center, w, yw, hf);
            if (pp.x > 4f && pp.y > 4f && pp.x < w - 4f && pp.y < w - 4f)
            {
                float ang = Game.Player.transform.eulerAngles.y - yw;
                Matrix4x4 old = GUI.matrix;
                GUIUtility.RotateAroundPivot(ang, pp);
                Fill(new Rect(pp.x - 5f * k, pp.y - 5f * k, 10f * k, 10f * k), Color.white);
                Fill(new Rect(pp.x - 2f * k, pp.y - 11f * k, 4f * k, 7f * k), new Color(1f, 0.85f, 0.2f));
                GUI.matrix = old;
            }
        }

        void DrawNorth(float w, float yw)
        {
            float rad = yw * Mathf.Deg2Rad;
            Vector2 n = new Vector2(-Mathf.Sin(rad), -Mathf.Cos(rad));   // hacia el norte en pantalla (y crece hacia abajo)
            Vector2 np = new Vector2(w * 0.5f, w * 0.5f) + n * (w * 0.5f - 12f);
            Fill(new Rect(np.x - 8, np.y - 8, 16, 16), new Color(0f, 0f, 0f, 0.6f));
            GUI.Label(new Rect(np.x - 8, np.y - 8, 16, 16), "N", northStyle);
        }

        public void Draw(Rect r)
        {
            if (Game.Player == null || rt == null) return;
            if (Event.current != null && Event.current.type != EventType.Repaint) return;
            EnsureStyles();
            float w = r.width;

            // Marco
            Fill(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), new Color(0.95f, 0.78f, 0.4f, 0.95f));
            Fill(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), new Color(0.12f, 0.10f, 0.14f, 1f));

            GUI.BeginGroup(r);
            GUI.DrawTexture(new Rect(0, 0, w, w), rt, ScaleMode.StretchToFill, false);
            DrawMarkers(Game.Player.transform.position, w, yaw, half, 1f);
            DrawNorth(w, yaw);
            GUI.EndGroup();
        }

        // ---------- Mapa grande (tecla M) ----------

        public void OpenLarge()
        {
            if (Game.Player == null) return;
            BigCenter = Game.Player.transform.position;
            BigHalf = Mathf.Clamp(IslandMap.Active ? 90f : 70f, MinHalf, MaxHalf);
        }

        /// <summary>Aleja el mapa para ver toda la zona (la isla entera, o todo el prólogo).</summary>
        public void ShowAll()
        {
            BigHalf = MaxHalf;
            BigCenter = IslandMap.Active ? new Vector3(0f, Game.Player != null ? Game.Player.transform.position.y : 0f, 0f) : BigCenter;
        }

        /// <summary>Aumento actual respecto al máximo alejamiento (x1 = todo el mapa).</summary>
        public float ZoomFactor { get { return MaxHalf / Mathf.Max(0.01f, BigHalf); } }

        void EnsureBig()
        {
            if (bigCam != null) return;
            bigRt = new RenderTexture(1024, 1024, 16, RenderTextureFormat.ARGB32);
            bigRt.name = "BigMapRT";
            bigRt.filterMode = FilterMode.Bilinear;

            GameObject go = new GameObject("BigMapCamera");
            go.transform.SetParent(transform, false);
            bigCam = go.AddComponent<Camera>();
            bigCam.orthographic = true;
            bigCam.targetTexture = bigRt;
            bigCam.clearFlags = CameraClearFlags.SolidColor;
            bigCam.backgroundColor = new Color(0.20f, 0.36f, 0.52f);
            bigCam.nearClipPlane = 1f;
            bigCam.farClipPlane = 700f;
            bigCam.depth = -11f;
            bigCam.allowHDR = false;
            bigCam.allowMSAA = false;
            bigCam.useOcclusionCulling = false;
            bigCam.enabled = false;
        }

        /// <summary>Renderiza el mapa grande (sin niebla). Se llama cada fotograma mientras el panel está abierto.</summary>
        public void RenderLarge()
        {
            if (Game.Player == null || IslandMap.Active) return;
            EnsureBig();
            bigCam.orthographicSize = BigHalf;
            bigCam.transform.position = new Vector3(BigCenter.x, Game.Player.transform.position.y + 220f, BigCenter.z);
            bigCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            bool fog = RenderSettings.fog;
            RenderSettings.fog = false;
            bigCam.Render();
            RenderSettings.fog = fog;
        }

        /// <summary>Dibuja el mapa grande, orientado al norte; arrastrar mueve, la rueda hace zoom.</summary>
        public void DrawLarge(Rect r)
        {
            bool island = IslandMap.Active;
            if (Game.Player == null || (!island && bigRt == null)) return;
            EnsureStyles();
            Event e = Event.current;
            float w = r.width;

            if (e != null && r.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseDrag && e.button == 0)
                {
                    float k = 2f * BigHalf / w;
                    BigCenter += new Vector3(-e.delta.x * k, 0f, e.delta.y * k);
                    if (island) BigCenter = new Vector3(Mathf.Clamp(BigCenter.x, -IslandMap.Half, IslandMap.Half), BigCenter.y, Mathf.Clamp(BigCenter.z, -IslandMap.Half, IslandMap.Half));
                    e.Use();
                }
                else if (e.type == EventType.ScrollWheel)
                {
                    BigHalf = Mathf.Clamp(BigHalf * (1f + e.delta.y * 0.12f), MinHalf, MaxHalf);
                    e.Use();
                }
            }

            if (e == null || e.type != EventType.Repaint) return;

            Fill(new Rect(r.x - 5, r.y - 5, r.width + 10, r.height + 10), new Color(0.95f, 0.78f, 0.4f, 1f));
            Fill(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), new Color(0.12f, 0.10f, 0.14f, 1f));
            GUI.BeginGroup(r);
            if (island) DrawIslandTexture(w);
            else GUI.DrawTexture(new Rect(0, 0, w, w), bigRt, ScaleMode.StretchToFill, false);
            DrawMarkers(BigCenter, w, 0f, BigHalf, 1.5f, island);
            DrawNorth(w, 0f);
            GUI.EndGroup();
        }

        /// <summary>Dibuja la parte descubierta de la isla (el resto queda oscuro).</summary>
        void DrawIslandTexture(float w)
        {
            Fill(new Rect(0, 0, w, w), new Color(0.09f, 0.08f, 0.11f, 1f));
            Texture2D t = IslandMap.Texture;
            if (t == null) return;
            float hf = BigHalf, H = IslandMap.Half;
            float x0 = Mathf.Max(BigCenter.x - hf, -H), x1 = Mathf.Min(BigCenter.x + hf, H);
            float z0 = Mathf.Max(BigCenter.z - hf, -H), z1 = Mathf.Min(BigCenter.z + hf, H);
            if (x1 <= x0 || z1 <= z0) return;
            Vector2 a = ToMap(new Vector3(x0, 0f, z1), BigCenter, w, 0f, hf);   // esquina superior izquierda
            Vector2 b = ToMap(new Vector3(x1, 0f, z0), BigCenter, w, 0f, hf);   // esquina inferior derecha
            Rect uv = new Rect((x0 + H) / (2f * H), (z0 + H) / (2f * H), (x1 - x0) / (2f * H), (z1 - z0) / (2f * H));
            GUI.DrawTextureWithTexCoords(new Rect(a.x, a.y, b.x - a.x, b.y - a.y), t, uv, true);
        }

        /// <summary>Leyenda de colores para el panel del mapa.</summary>
        public void DrawLegend(float x, float y)
        {
            EnsureStyles();
            string[] names = { "Tú", "Pareja", "Enemigo", "Cofre", "Refugio" };
            Color[] cols = { Color.white, new Color(1f, 0.55f, 0.72f), new Color(0.95f, 0.25f, 0.25f), new Color(1f, 0.85f, 0.25f), new Color(0.45f, 0.95f, 0.65f) };
            for (int i = 0; i < names.Length; i++)
            {
                Fill(new Rect(x, y + i * 26f + 5f, 14f, 14f), cols[i]);
                GUI.Label(new Rect(x + 22f, y + i * 26f, 140f, 24f), names[i], smallStyle);
            }
        }
    }
}
