using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Menús con IMGUI sobre la escena 3D: principal, creador de personajes,
    /// multijugador (placeholder), opciones, pausa y pantalla final del prólogo.
    /// </summary>
    public class MenuUI : MonoBehaviour
    {
        public PrologueDirector Director;

        enum Screen_ { Main, Creator, Multi, Options }
        Screen_ screen = Screen_.Main;
        bool fromPause;
        float camT;
        float heroYaw = 160f;

        Texture2D white;
        GUIStyle title, sub, btn, lbl, panelLbl;
        Texture2D btnN, btnH, btnA;

        public static float CameraSpeed = 1f;

        void Awake()
        {
            white = new Texture2D(1, 1);
            white.SetPixel(0, 0, Color.white);
            white.Apply();
            btnN = Solid(new Color(0.96f, 0.80f, 0.45f));
            btnH = Solid(new Color(1f, 0.90f, 0.62f));
            btnA = Solid(new Color(0.85f, 0.65f, 0.30f));
            CameraSpeed = PlayerPrefs.GetFloat("camSpeed", 1f);
            Sfx.Volume = PlayerPrefs.GetFloat("volume", 0.6f);
            GameInput.RightClickCamera = PlayerPrefs.GetInt("dragRight", 0) == 1;
        }

        Texture2D Solid(Color c)
        {
            Texture2D t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        public void ShowMainMenu()
        {
            Time.timeScale = 1f;
            Game.State = GameState.Menu;
            screen = Screen_.Main;
            fromPause = false;
            MenuScene.PreviewArmor = 1;
            MenuScene.Show();
            MenuScene.RefreshHero();
        }

        void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            title.normal.textColor = new Color(1f, 0.96f, 0.82f);
            sub = new GUIStyle(title) { fontSize = 20, fontStyle = FontStyle.Normal };
            btn = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };
            btn.normal.background = btnN;
            btn.hover.background = btnH;
            btn.active.background = btnA;
            btn.normal.textColor = new Color(0.30f, 0.18f, 0.10f);
            btn.hover.textColor = new Color(0.30f, 0.18f, 0.10f);
            btn.active.textColor = new Color(0.30f, 0.18f, 0.10f);
            lbl = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            lbl.normal.textColor = Color.white;
            panelLbl = new GUIStyle(lbl) { fontSize = 16, fontStyle = FontStyle.Normal, wordWrap = true, alignment = TextAnchor.UpperLeft };
        }

        void Fill(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, white);
            GUI.color = old;
        }

        bool Btn(Rect r, string text)
        {
            if (GUI.Button(r, text, btn)) { Sfx.Play("ui"); return true; }
            return false;
        }

        /// <summary>Escala de la interfaz. El menú principal y el creador se dibujan más pequeños (80 %) para dejar sitio al personaje.</summary>
        float UiScale()
        {
            float s = Mathf.Max(0.75f, UnityEngine.Screen.height / 720f);
            if (Game.State == GameState.Menu) s *= 0.8f;
            return s;
        }

        // ---------- Cámara del menú ----------

        void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            if (Game.State == GameState.Menu)
            {
                camT += Time.deltaTime * 0.25f;
                Vector3 o = MenuScene.Origin;
                bool creator = screen == Screen_.Creator;
                float dist = creator ? 4.1f : 8.5f;
                float sway = Mathf.Sin(camT) * (creator ? 0.15f : 0.45f);
                // En el creador el panel ocupa la izquierda: la cámara se desplaza para que el personaje quede
                // centrado en la zona libre de la derecha.
                float shift = 0f;
                if (creator)
                {
                    float halfW = dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * cam.aspect;
                    float panelPx = 460f * UiScale();
                    shift = panelPx / UnityEngine.Screen.width * halfW;
                }
                Vector3 target = o + new Vector3(creator ? -shift : 1.2f, creator ? 1.15f : 1.5f, 0f);
                Vector3 pos = o + new Vector3(sway * dist * 0.6f + (creator ? -shift : 1f), creator ? 1.6f : 2.6f, -dist);
                cam.transform.position = Vector3.Lerp(cam.transform.position, pos, 1f - Mathf.Exp(-3f * Time.unscaledDeltaTime));
                Quaternion rot = Quaternion.LookRotation(target - cam.transform.position);
                cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, rot, 1f - Mathf.Exp(-5f * Time.unscaledDeltaTime));
                if (MenuScene.HeroT != null)
                {
                    heroYaw += Time.unscaledDeltaTime * (creator ? 28f : 0f);
                    float yaw = creator ? heroYaw : 200f + Mathf.Sin(camT * 0.7f) * 8f;
                    MenuScene.HeroT.rotation = Quaternion.Euler(0f, yaw, 0f);
                    if (MenuScene.Hero != null) MenuScene.Hero.Animate(0f, Time.unscaledDeltaTime);
                }
            }
        }

        void Update()
        {
            // Reanudar con Escape cuando está en pausa
            if (Game.State == GameState.Paused && Game.Panel == UiPanel.None && screen == Screen_.Main && Time.frameCount > Game.PauseFrame && GameInput.Pressed(Act.Pause))
                Resume();
            if (Game.State == GameState.Ended && Director != null && Director.Finished) { /* pantalla final en OnGUI */ }
        }

        void Resume()
        {
            Time.timeScale = 1f;
            Game.State = GameState.Playing;
        }

        // ---------- GUI ----------

        void OnGUI()
        {
            Styles();
            float s = UiScale();
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float W = UnityEngine.Screen.width / s, H = UnityEngine.Screen.height / s;

            HandleCapture();
            switch (Game.State)
            {
                case GameState.Menu: DrawMenu(W, H); break;
                case GameState.Paused: if (Game.Panel == UiPanel.None) DrawPause(W, H); break;
                case GameState.Ended: DrawEnd(W, H); break;
            }
            GUI.matrix = old;
        }

        void DrawMenu(float W, float H)
        {
            switch (screen)
            {
                case Screen_.Main: DrawMain(W, H); break;
                case Screen_.Creator: DrawCreator(W, H); break;
                case Screen_.Multi: DrawMulti(W, H); break;
                case Screen_.Options: DrawOptions(W, H); break;
            }
        }

        string seedText = "";
        GUIStyle fieldStyle;

        void DrawMain(float W, float H)
        {
            GUI.Label(new Rect(0, 40, W * 0.55f, 90), "HEARTHBOUND", title);
            GUI.Label(new Rect(0, 118, W * 0.55f, 30), "Prólogo y mundo abierto", sub);

            if (fieldStyle == null)
            {
                fieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            }

            float x = W * 0.275f - 160f, y = H * 0.29f, step = 58f, bh = 50f;
            if (Btn(new Rect(x, y, 320, bh), "Prólogo")) StartGame();
            if (Btn(new Rect(x, y + step, 320, bh), "Isla (mundo abierto)")) StartIsland();

            // Semilla de la isla (vacío = al azar)
            GUI.SetNextControlName("seed");
            seedText = GUI.TextField(new Rect(x + 332, y + step, 130, bh), seedText, 9, fieldStyle);
            if (string.IsNullOrEmpty(seedText) && GUI.GetNameOfFocusedControl() != "seed")
                GUI.Label(new Rect(x + 332, y + step, 130, bh), "semilla", new GUIStyle(sub) { fontSize = 16, alignment = TextAnchor.MiddleCenter });

            if (Btn(new Rect(x, y + step * 2, 320, bh), "Creador de personajes")) { screen = Screen_.Creator; MenuScene.PreviewArmor = 1; MenuScene.RefreshHero(); }
            if (Btn(new Rect(x, y + step * 3, 320, bh), "Multijugador")) screen = Screen_.Multi;
            if (Btn(new Rect(x, y + step * 4, 320, bh), "Opciones")) { screen = Screen_.Options; optTab = OptTab.General; fromPause = false; optScroll = Vector2.zero; }
            if (Btn(new Rect(x, y + step * 5, 320, bh), "Controles")) { screen = Screen_.Options; optTab = OptTab.Controls; fromPause = false; optScroll = Vector2.zero; }
#if !UNITY_WEBGL
            if (Btn(new Rect(x, y + step * 6, 320, bh), "Salir"))
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
#endif
        }

        void DrawCreator(float W, float H)
        {
            Rect p = new Rect(30, 40, 430, H - 80);
            Fill(new Rect(p.x - 4, p.y - 4, p.width + 8, p.height + 8), new Color(0.95f, 0.78f, 0.4f, 0.9f));
            Fill(p, new Color(0.17f, 0.13f, 0.20f, 0.92f));
            GUI.Label(new Rect(p.x + 20, p.y + 10, 390, 40), "Creador de personajes", lbl);

            CharacterLook look = Game.Look;
            int b0 = look.Body, h0 = look.Hair, s0 = look.Skin, hc0 = look.HairColor, c0 = look.Cloth;
            float y = p.y + 64f;
            look.Body = Chooser(p.x + 20, ref y, "Cuerpo", CharacterLook.BodyNames, look.Body);
            look.Hair = Chooser(p.x + 20, ref y, "Peinado", CharacterLook.HairNames, look.Hair);
            look.Skin = Swatches(p.x + 20, ref y, "Piel", CharacterLook.SkinColors, look.Skin);
            look.HairColor = Swatches(p.x + 20, ref y, "Color de cabello", CharacterLook.HairColors, look.HairColor);
            look.Cloth = Swatches(p.x + 20, ref y, "Color de ropa", CharacterLook.ClothColors, look.Cloth);
            if (b0 != look.Body || h0 != look.Hair || s0 != look.Skin || hc0 != look.HairColor || c0 != look.Cloth)
                MenuScene.RefreshHero();

            // Vista previa del sistema de armaduras por niveles
            GUI.Label(new Rect(p.x + 20, y, 390, 26), "Vista previa de armadura: nivel " + MenuScene.PreviewArmor, lbl);
            y += 30f;
            for (int i = 1; i <= 4; i++)
            {
                if (Btn(new Rect(p.x + 20 + (i - 1) * 96, y, 88, 40), "Nivel " + i))
                {
                    MenuScene.PreviewArmor = i;
                    MenuScene.RefreshHero();
                }
            }
            y += 56f;
            GUI.Label(new Rect(p.x + 20, y, 390, 50), "En el prólogo consigues el nivel 2. Los niveles 3 y 4 llegan en las siguientes ruinas.", panelLbl);

            if (Btn(new Rect(p.x + 20, p.yMax - 70, 390, 50), "Listo"))
            {
                MenuScene.PreviewArmor = 1;
                MenuScene.RefreshHero();
                screen = Screen_.Main;
            }
        }

        int Chooser(float x, ref float y, string name, string[] options, int value)
        {
            GUI.Label(new Rect(x, y, 390, 26), name + ": " + options[Mathf.Clamp(value, 0, options.Length - 1)], lbl);
            y += 30f;
            int nv = value;
            if (Btn(new Rect(x, y, 60, 38), "<")) nv = CharacterLook.Cycle(value, options.Length, -1);
            if (Btn(new Rect(x + 330, y, 60, 38), ">")) nv = CharacterLook.Cycle(value, options.Length, 1);
            y += 52f;
            return nv;
        }

        int Swatches(float x, ref float y, string name, Color[] colors, int value)
        {
            GUI.Label(new Rect(x, y, 390, 26), name, lbl);
            y += 30f;
            int nv = value;
            for (int i = 0; i < colors.Length; i++)
            {
                Rect r = new Rect(x + i * 62, y, 52, 38);
                if (i == value) Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), Color.white);
                Fill(r, colors[i]);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { nv = i; Sfx.Play("ui", 1.2f); }
            }
            y += 52f;
            return nv;
        }

        void DrawMulti(float W, float H)
        {
            Rect p = new Rect(30, 40, 520, 360);
            Fill(new Rect(p.x - 4, p.y - 4, p.width + 8, p.height + 8), new Color(0.95f, 0.78f, 0.4f, 0.9f));
            Fill(p, new Color(0.17f, 0.13f, 0.20f, 0.92f));
            GUI.Label(new Rect(p.x + 20, p.y + 12, 480, 40), "Multijugador cooperativo", lbl);
            GUI.Label(new Rect(p.x + 20, p.y + 60, 480, 140),
                "Co-op de 2 a 4 jugadores, local u online, con pareja compartida, " +
                "puzles cooperativos y revivir a compañeros.\n\n" +
                "Esta función todavía no está en el prólogo. Ya está pensada en el diseño (GDD) y el código usa un único punto de entrada de input para poder ampliarla.", panelLbl);
            GUI.enabled = false;
            Btn(new Rect(p.x + 20, p.y + 230, 230, 46), "Crear partida");
            Btn(new Rect(p.x + 270, p.y + 230, 230, 46), "Unirse");
            GUI.enabled = true;
            if (Btn(new Rect(p.x + 20, p.yMax - 66, 480, 46), "Volver")) screen = Screen_.Main;
        }

        enum OptTab { General, Camera, Controls }
        OptTab optTab = OptTab.General;
        Vector2 optScroll;
        bool capturing;
        Act capAct;
        int capSlot;

        void DrawOptions(float W, float H)
        {
            Rect p = new Rect(30, 30, 780, 600);
            Fill(new Rect(p.x - 4, p.y - 4, p.width + 8, p.height + 8), new Color(0.95f, 0.78f, 0.4f, 0.9f));
            Fill(p, new Color(0.17f, 0.13f, 0.20f, 0.95f));
            GUI.Label(new Rect(p.x + 20, p.y + 10, 480, 40), "Opciones", lbl);
            DrawOptionsBody(p);
            if (Btn(new Rect(p.x + 20, p.yMax - 62, 740, 46), "Volver"))
            {
                capturing = false;
                screen = Screen_.Main;
            }
        }

        /// <summary>Opciones con pestañas (menú principal y pausa): General, Cámara y Controles.</summary>
        void DrawOptionsBody(Rect p)
        {
            string[] names = { "General", "Cámara", "Controles" };
            for (int i = 0; i < 3; i++)
            {
                Rect t = new Rect(p.x + 20 + i * 250, p.y + 54, 240, 40);
                if (Btn(t, names[i])) { optTab = (OptTab)i; capturing = false; optScroll = Vector2.zero; }
                if ((int)optTab == i) Fill(new Rect(t.x, t.yMax + 2, t.width, 4), Color.white);
            }

            Rect view = new Rect(p.x + 16, p.y + 108, p.width - 32, p.height - 108 - 74);
            switch (optTab)
            {
                case OptTab.General: DrawGeneralTab(view); break;
                case OptTab.Camera: DrawCameraTab(view); break;
                default: DrawControlsTab(view); break;
            }
        }

        float Slider(Rect view, float y, string text, float value, float min, float max)
        {
            GUI.Label(new Rect(view.x + 4, y, view.width - 8, 28), text, lbl);
            return GUI.HorizontalSlider(new Rect(view.x + 4, y + 32, view.width - 40, 22), value, min, max);
        }

        void DrawGeneralTab(Rect v)
        {
            GUI.BeginGroup(v);
            Rect g = new Rect(0, 0, v.width, v.height);
            float vol = Slider(g, 4, "Volumen de efectos: " + Mathf.RoundToInt(Sfx.Volume * 100f) + "%", Sfx.Volume, 0f, 1f);
            if (!Mathf.Approximately(vol, Sfx.Volume)) { Sfx.Volume = vol; PlayerPrefs.SetFloat("volume", vol); }

            GUI.Label(new Rect(4, 80, g.width - 8, 28), "Modo desarrollador: " + (Settings.Dev ? "ACTIVADO" : "desactivado"), lbl);
            if (Btn(new Rect(4, 114, 360, 42), Settings.Dev ? "Desactivar modo dev" : "Activar modo dev"))
            {
                Settings.Dev = !Settings.Dev;
                Settings.Save();
            }
            GUI.Label(new Rect(4, 164, g.width - 8, 120),
                "Con el modo dev activado, " + GameInput.KeyName(GameInput.Get(Act.Dev, 0)) + " abre un panel durante la partida para " +
                "dar equipo, hacer aparecer enemigos, cofres, enredaderas y rocas, probar efectos y teletransportarte. " +
                "Puedes cambiar esa tecla en Controles.", panelLbl);
            GUI.EndGroup();
        }

        void DrawCameraTab(Rect v)
        {
            float contentH = 520f;
            optScroll = GUI.BeginScrollView(v, optScroll, new Rect(0, 0, v.width - 20, contentH));
            Rect g = new Rect(0, 0, v.width - 20, contentH);
            float y = 4;

            GUI.Label(new Rect(4, y, g.width - 8, 28), "Cámara actual: " + (CameraRig.Mode == CameraMode.Aerial ? "Aérea" : "Sobre el hombro"), lbl);
            if (Btn(new Rect(4, y + 32, 360, 40), "Cambiar cámara (" + GameInput.KeyName(GameInput.Get(Act.CameraMode, 0)) + ")")) CameraRig.Toggle();
            y += 86;

            float c = Slider(g, y, "Velocidad de giro (teclas y mando): " + CameraSpeed.ToString("0.0") + "x", CameraSpeed, 0.4f, 2f);
            if (!Mathf.Approximately(c, CameraSpeed)) { CameraSpeed = c; PlayerPrefs.SetFloat("camSpeed", c); ApplyCameraSpeed(); }
            y += 64;

            float ms = Slider(g, y, "Sensibilidad del ratón: " + Settings.MouseSens.ToString("0.0") + "x", Settings.MouseSens, 0.3f, 3f);
            if (!Mathf.Approximately(ms, Settings.MouseSens)) { Settings.MouseSens = ms; Settings.Save(); }
            y += 64;

            float cf = Slider(g, y, "Seguimiento de la cámara: " + Settings.CamFollow.ToString("0") + "  (bajo = suave, alto = rígida)", Settings.CamFollow, 3f, 20f);
            if (!Mathf.Approximately(cf, Settings.CamFollow)) { Settings.CamFollow = cf; Settings.Save(); }
            y += 64;

            if (Btn(new Rect(4, y, 360, 40), "Invertir horizontal: " + (Settings.InvertX ? "sí" : "no"))) { Settings.InvertX = !Settings.InvertX; Settings.Save(); }
            if (Btn(new Rect(376, y, 360, 40), "Invertir vertical: " + (Settings.InvertY ? "sí" : "no"))) { Settings.InvertY = !Settings.InvertY; Settings.Save(); }
            y += 56;

            GUI.Label(new Rect(4, y, g.width - 8, 28), "Girar con el ratón: mantener " + (GameInput.RightClickCamera ? "botón derecho" : "botón central"), lbl);
            if (Btn(new Rect(4, y + 32, 360, 40), "Cambiar botón de giro"))
            {
                GameInput.RightClickCamera = !GameInput.RightClickCamera;
                PlayerPrefs.SetInt("dragRight", GameInput.RightClickCamera ? 1 : 0);
            }
            if (Btn(new Rect(376, y + 32, 360, 40), "Restablecer cámara")) Settings.ResetCamera();
            if (GameInput.RightClickCamera)
                GUI.Label(new Rect(4, y + 76, g.width - 8, 24), "Con el botón derecho para girar, el escudo queda solo en su tecla.", panelLbl);
            GUI.EndScrollView();
        }

        void DrawControlsTab(Rect v)
        {
            int n = GameInput.Rebindable.Length;
            float rowH = 40f;
            float contentH = 70f + n * rowH + 190f;
            optScroll = GUI.BeginScrollView(v, optScroll, new Rect(0, 0, v.width - 20, contentH));
            float w = v.width - 20;

            GUI.Label(new Rect(4, 2, w - 8, 60), capturing
                ? "Pulsa la nueva tecla o botón del ratón para \"" + GameInput.Label(capAct) + "\"…   (Esc cancela, Supr borra)"
                : "Haz clic en una casilla y pulsa la nueva tecla. Cada acción admite dos. Si repites una tecla, se le quita a la otra acción.", panelLbl);

            for (int i = 0; i < n; i++)
            {
                Act a = GameInput.Rebindable[i];
                float y = 66f + i * rowH;
                if (i % 2 == 0) Fill(new Rect(0, y - 2, w, rowH - 2), new Color(1f, 1f, 1f, 0.05f));
                GUI.Label(new Rect(8, y, 300, 34), GameInput.Label(a), lbl);
                for (int slot = 0; slot < 2; slot++)
                {
                    bool active = capturing && capAct == a && capSlot == slot;
                    string text = active ? "Pulsa una tecla…" : GameInput.KeyName(GameInput.Get(a, slot));
                    if (Btn(new Rect(320 + slot * 210, y, 200, 34), text)) { capturing = true; capAct = a; capSlot = slot; }
                }
            }

            float by = 66f + n * rowH + 8f;
            if (Btn(new Rect(4, by, 360, 40), "Restablecer controles")) { GameInput.ResetDefaults(); capturing = false; }
            GUI.Label(new Rect(4, by + 50, w - 8, 130),
                "Fijas: Esc = pausa, Enter / E = confirmar.\n" +
                "Mando: stick izq. mover, stick der. cámara, A saltar, X espada, LT escudo, RT varita, Y arco, B bomba, " +
                "LB interactuar, RB fijar, cruz derecha hacha, clic stick izq. esquivar, clic stick der. cambiar cámara, " +
                "cruz abajo pareja, arriba mapa grande, izquierda minimapa, Select inventario, Start pausa.", panelLbl);
            GUI.EndScrollView();
        }

        public static void ApplyCameraSpeed()
        {
            if (Camera.main == null) return;
            CameraRig r = Camera.main.GetComponent<CameraRig>();
            if (r != null) r.TurnSpeed = 110f * CameraSpeed;
        }

        /// <summary>Procesa la captura de tecla al reasignar (antes de dibujar, para que el clic no se pierda).</summary>
        void HandleCapture()
        {
            if (!capturing) return;
            Event e = Event.current;
            if (e == null) return;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { capturing = false; e.Use(); return; }
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace))
            {
                GameInput.Set(capAct, capSlot, KeyCode.None);
                capturing = false;
                e.Use();
                return;
            }
            KeyCode kc;
            if (GameInput.TryCapture(e, out kc))
            {
                GameInput.Set(capAct, capSlot, kc);
                Sfx.Play("ui", 1.3f);
                capturing = false;
                e.Use();
            }
        }

        void DrawPause(float W, float H)
        {
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.55f));
            GUI.Label(new Rect(0, 90, W, 80), "Pausa", title);
            float x = W * 0.5f - 160f, y = H * 0.38f;
            if (screen == Screen_.Options)
            {
                DrawOptionsInPause(W, H);
                return;
            }
            if (Btn(new Rect(x, y, 320, 54), "Continuar")) Resume();
            if (Btn(new Rect(x, y + 68, 320, 54), "Opciones")) { screen = Screen_.Options; }
            if (Btn(new Rect(x, y + 136, 320, 54), "Menú principal")) ExitToMenu();
        }

        void DrawOptionsInPause(float W, float H)
        {
            Rect p = new Rect(W * 0.5f - 390, 40, 780, 620);
            Fill(new Rect(p.x - 4, p.y - 4, p.width + 8, p.height + 8), new Color(0.95f, 0.78f, 0.4f, 0.9f));
            Fill(p, new Color(0.17f, 0.13f, 0.20f, 0.97f));
            GUI.Label(new Rect(p.x + 20, p.y + 10, 480, 40), "Opciones", lbl);
            DrawOptionsBody(p);
            if (Btn(new Rect(p.x + 20, p.yMax - 62, 740, 46), "Volver")) { capturing = false; screen = Screen_.Main; }
        }

        void DrawEnd(float W, float H)
        {
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.6f));
            GUI.Label(new Rect(0, 80, W, 80), "Prólogo completado", title);
            GUI.Label(new Rect(W * 0.5f - 300, 190, 600, 200),
                "Conseguiste la armadura de nivel 2, aprendiste a usar espada, escudo, arco y bombas, " +
                "y protegiste a tu pareja de los slimes.\n\n" +
                "Tu varita de fuego ya quema enredaderas; en la próxima ruina descubrirás el hielo y nuevas magias.", panelLbl);
            float x = W * 0.5f - 160f, y = H * 0.62f;
            if (Btn(new Rect(x, y, 320, 54), "Jugar de nuevo")) StartGame();
            if (Btn(new Rect(x, y + 68, 320, 54), "Menú principal")) ExitToMenu();
        }

        // ---------- Acciones ----------

        void StartGame()
        {
            MenuScene.Hide();
            Time.timeScale = 1f;
            screen = Screen_.Main;
            Prim.ClearCache();
            Director.BeginPrologue();
            ApplyCameraSpeed();
        }

        void StartIsland()
        {
            int seed;
            string t = seedText != null ? seedText.Trim() : "";
            if (t.Length == 0) seed = Random.Range(1, 999999);
            else if (!int.TryParse(t, out seed))
            {
                seed = 17;
                for (int i = 0; i < t.Length; i++) seed = seed * 31 + t[i];
            }
            GUI.FocusControl(null);
            MenuScene.Hide();
            Time.timeScale = 1f;
            screen = Screen_.Main;
            Prim.ClearCache();
            Director.BeginIsland(seed);
            ApplyCameraSpeed();
        }

        void ExitToMenu()
        {
            Time.timeScale = 1f;
            Director.Cleanup();
            Prim.ClearCache();
            ShowMainMenu();
        }
    }
}
