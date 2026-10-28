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

        enum Screen_ { Main, Creator, Multi, Options, Controls }
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
                Vector3 target = o + new Vector3(creator ? 0.4f : 1.2f, creator ? 1.15f : 1.5f, 0f);
                Vector3 pos = o + new Vector3(sway * dist * 0.6f + (creator ? 0f : 1f), creator ? 1.6f : 2.6f, -dist);
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
            if (Game.State == GameState.Paused && screen == Screen_.Main && Time.frameCount > Game.PauseFrame && GameInput.Pressed(Act.Pause))
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
            float s = Mathf.Max(0.75f, UnityEngine.Screen.height / 720f);
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float W = UnityEngine.Screen.width / s, H = UnityEngine.Screen.height / s;

            switch (Game.State)
            {
                case GameState.Menu: DrawMenu(W, H); break;
                case GameState.Paused: DrawPause(W, H); break;
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
                case Screen_.Controls: DrawControls(W, H); break;
            }
        }

        void DrawMain(float W, float H)
        {
            GUI.Label(new Rect(0, 40, W * 0.55f, 90), "HEARTHBOUND", title);
            GUI.Label(new Rect(0, 118, W * 0.55f, 30), "Prólogo jugable", sub);

            float x = W * 0.275f - 160f, y = H * 0.40f;
            if (Btn(new Rect(x, y, 320, 54), "Jugar")) StartGame();
            if (Btn(new Rect(x, y + 68, 320, 54), "Creador de personajes")) { screen = Screen_.Creator; MenuScene.PreviewArmor = 1; MenuScene.RefreshHero(); }
            if (Btn(new Rect(x, y + 136, 320, 54), "Multijugador")) screen = Screen_.Multi;
            if (Btn(new Rect(x, y + 204, 320, 54), "Opciones")) { screen = Screen_.Options; fromPause = false; }
            if (Btn(new Rect(x, y + 272, 320, 54), "Controles")) screen = Screen_.Controls;
#if !UNITY_WEBGL
            if (Btn(new Rect(x, y + 340, 320, 54), "Salir"))
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
            float y = p.y + 64f;
            look.Body = Chooser(p.x + 20, ref y, "Cuerpo", CharacterLook.BodyNames, look.Body);
            look.Hair = Chooser(p.x + 20, ref y, "Peinado", CharacterLook.HairNames, look.Hair);
            look.Skin = Swatches(p.x + 20, ref y, "Piel", CharacterLook.SkinColors, look.Skin);
            look.HairColor = Swatches(p.x + 20, ref y, "Color de cabello", CharacterLook.HairColors, look.HairColor);
            look.Cloth = Swatches(p.x + 20, ref y, "Color de ropa", CharacterLook.ClothColors, look.Cloth);

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
            if (nv != value) MenuScene.RefreshHero();
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
            if (nv != value) MenuScene.RefreshHero();
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

        void DrawOptions(float W, float H)
        {
            Rect p = new Rect(30, 40, 520, 340);
            Fill(new Rect(p.x - 4, p.y - 4, p.width + 8, p.height + 8), new Color(0.95f, 0.78f, 0.4f, 0.9f));
            Fill(p, new Color(0.17f, 0.13f, 0.20f, 0.92f));
            GUI.Label(new Rect(p.x + 20, p.y + 12, 480, 40), "Opciones", lbl);

            GUI.Label(new Rect(p.x + 20, p.y + 70, 300, 30), "Volumen de efectos: " + Mathf.RoundToInt(Sfx.Volume * 100f) + "%", lbl);
            float v = GUI.HorizontalSlider(new Rect(p.x + 20, p.y + 108, 480, 24), Sfx.Volume, 0f, 1f);
            if (!Mathf.Approximately(v, Sfx.Volume)) { Sfx.Volume = v; PlayerPrefs.SetFloat("volume", v); }

            GUI.Label(new Rect(p.x + 20, p.y + 150, 400, 30), "Velocidad de giro de cámara: " + CameraSpeed.ToString("0.0") + "x", lbl);
            float c = GUI.HorizontalSlider(new Rect(p.x + 20, p.y + 188, 480, 24), CameraSpeed, 0.4f, 2f);
            if (!Mathf.Approximately(c, CameraSpeed)) { CameraSpeed = c; PlayerPrefs.SetFloat("camSpeed", c); ApplyCameraSpeed(); }

            if (Btn(new Rect(p.x + 20, p.yMax - 66, 480, 46), "Volver"))
            {
                Sfx.Play("ui");
                screen = Screen_.Main;
            }
        }

        public static void ApplyCameraSpeed()
        {
            if (Camera.main == null) return;
            CameraRig r = Camera.main.GetComponent<CameraRig>();
            if (r != null) r.TurnSpeed = 110f * CameraSpeed;
        }

        void DrawControls(float W, float H)
        {
            Rect p = new Rect(30, 40, 560, 470);
            Fill(new Rect(p.x - 4, p.y - 4, p.width + 8, p.height + 8), new Color(0.95f, 0.78f, 0.4f, 0.9f));
            Fill(p, new Color(0.17f, 0.13f, 0.20f, 0.92f));
            GUI.Label(new Rect(p.x + 20, p.y + 12, 500, 40), "Controles", lbl);
            GUI.Label(new Rect(p.x + 20, p.y + 56, 520, 380),
                "TECLADO Y RATÓN\n" +
                "WASD  mover\n" +
                "Flechas izq./der.  girar la cámara\n" +
                "J o clic izquierdo  espada (combo de 3 golpes)\n" +
                "K o clic derecho  escudo (parry si bloqueas justo antes del golpe)\n" +
                "Espacio  esquivar\n" +
                "Tab  fijar / soltar enemigo\n" +
                "F (mantener)  apuntar el arco; soltar para disparar\n" +
                "B  lanzar bomba\n" +
                "E  interactuar\n" +
                "C  ordenar a tu pareja (seguir / esperar / refugio)\n" +
                "Esc  pausa\n\n" +
                "MANDO\n" +
                "Stick izq. mover, stick der. cámara, X espada, LT escudo,\n" +
                "A esquivar, RB fijar, Y arco, B bomba, LB interactuar,\n" +
                "Cruz abajo pareja, Start pausa", panelLbl);
            if (Btn(new Rect(p.x + 20, p.yMax - 66, 520, 46), "Volver")) screen = fromPause ? Screen_.Main : Screen_.Main;
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
            Rect p = new Rect(W * 0.5f - 260, 190, 520, 280);
            Fill(new Rect(p.x - 4, p.y - 4, p.width + 8, p.height + 8), new Color(0.95f, 0.78f, 0.4f, 0.9f));
            Fill(p, new Color(0.17f, 0.13f, 0.20f, 0.95f));
            GUI.Label(new Rect(p.x + 20, p.y + 70 - 56, 300, 30), "Volumen: " + Mathf.RoundToInt(Sfx.Volume * 100f) + "%", lbl);
            float v = GUI.HorizontalSlider(new Rect(p.x + 20, p.y + 48, 480, 24), Sfx.Volume, 0f, 1f);
            if (!Mathf.Approximately(v, Sfx.Volume)) { Sfx.Volume = v; PlayerPrefs.SetFloat("volume", v); }
            GUI.Label(new Rect(p.x + 20, p.y + 90, 400, 30), "Cámara: " + CameraSpeed.ToString("0.0") + "x", lbl);
            float c = GUI.HorizontalSlider(new Rect(p.x + 20, p.y + 128, 480, 24), CameraSpeed, 0.4f, 2f);
            if (!Mathf.Approximately(c, CameraSpeed)) { CameraSpeed = c; PlayerPrefs.SetFloat("camSpeed", c); ApplyCameraSpeed(); }
            if (Btn(new Rect(p.x + 20, p.yMax - 66, 480, 46), "Volver")) screen = Screen_.Main;
        }

        void DrawEnd(float W, float H)
        {
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.6f));
            GUI.Label(new Rect(0, 80, W, 80), "Prólogo completado", title);
            GUI.Label(new Rect(W * 0.5f - 300, 190, 600, 200),
                "Conseguiste la armadura de nivel 2, aprendiste a usar espada, escudo, arco y bombas, " +
                "y protegiste a tu pareja de los slimes.\n\n" +
                "La varita que encontraste abrirá el hielo y el fuego en la próxima ruina.", panelLbl);
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

        void ExitToMenu()
        {
            Time.timeScale = 1f;
            Director.Cleanup();
            Prim.ClearCache();
            ShowMainMenu();
        }
    }
}
