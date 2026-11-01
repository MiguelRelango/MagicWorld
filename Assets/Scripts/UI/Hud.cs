using UnityEngine;

namespace Hearthbound
{
    /// <summary>HUD en pantalla con IMGUI: vida, objetos, objetivo, diálogos, avisos y marca de fijado.</summary>
    public class Hud : MonoBehaviour
    {
        public PrologueDirector Director;

        Texture2D white;
        GUIStyle label, big, small, box;

        void Awake()
        {
            white = new Texture2D(1, 1);
            white.SetPixel(0, 0, Color.white);
            white.Apply();
        }

        void Styles()
        {
            if (label != null) return;
            label = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, wordWrap = true };
            label.normal.textColor = Color.white;
            big = new GUIStyle(label) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
            small = new GUIStyle(label) { fontSize = 13, fontStyle = FontStyle.Normal };
            box = new GUIStyle(label) { fontSize = 17, alignment = TextAnchor.UpperLeft };
        }

        void Fill(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, white);
            GUI.color = old;
        }

        void OnGUI()
        {
            if (Game.State != GameState.Playing && Game.State != GameState.Paused) return;
            if (Game.Player == null) return;
            Styles();
            float s = Mathf.Max(0.75f, Screen.height / 720f);
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float W = Screen.width / s, H = Screen.height / s;

            DrawHealth();
            DrawMana();
            DrawItems(W);
            DrawObjective(W);
            DrawPartner();
            DrawBow(W, H);
            DrawPrompt(W, H);
            DrawDialog(W, H);
            DrawToast(W, H);
            DrawHint(W, H);
            DrawTreeInfo(W, H);
            DrawBuild(W, H);
            DrawDevBadge(H);
            DrawLockMarker(s);
            DrawCrosshair(W, H);
            if (Minimap.Instance != null) Minimap.Instance.Draw(new Rect(W - 192f, 18f, 172f, 172f));
            DrawFlash(W, H);

            if (Director != null && Director.Dead)
            {
                Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.55f));
                GUI.Label(new Rect(0, H * 0.4f, W, 60), "Te desmayaste...", big);
            }
            GUI.matrix = old;
        }

        void DrawHealth()
        {
            Health hp = Game.Player.GetComponent<Health>();
            if (hp == null) return;
            int max = Mathf.CeilToInt(hp.Max);
            for (int i = 0; i < max; i++)
            {
                Rect r = new Rect(18 + i * 30, 18, 24, 24);
                Fill(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), new Color(0.2f, 0.05f, 0.08f, 0.8f));
                Fill(r, new Color(0.35f, 0.18f, 0.22f));
                float fill = Mathf.Clamp01(hp.Hp - i);
                if (fill > 0f) Fill(new Rect(r.x, r.y, r.width * fill, r.height), new Color(0.95f, 0.30f, 0.38f));
            }
        }

        void DrawMana()
        {
            if (!Game.HasWand) return;
            float f = Mathf.Clamp01(Game.Mana / Mathf.Max(1f, Game.MaxMana));
            Fill(new Rect(16, 46, 204, 14), new Color(0f, 0.05f, 0.15f, 0.7f));
            Fill(new Rect(18, 48, 200f * f, 10), f < 0.2f ? new Color(0.45f, 0.55f, 0.9f) : new Color(0.30f, 0.60f, 1f));
            GUI.Label(new Rect(226, 42, 120, 20), "Maná", small);
        }

        void DrawCrosshair(float W, float H)
        {
            PlayerCombat c = Game.Player.GetComponent<PlayerCombat>();
            if (c == null || CameraRig.Mode != CameraMode.Shoulder || !(c.Aiming || Game.HasWand)) return;
            float cx = W * 0.5f, cy = H * 0.5f;
            Color col = new Color(1f, 0.95f, 0.6f, 0.95f);
            Fill(new Rect(cx - 1, cy - 12, 2, 8), col);
            Fill(new Rect(cx - 1, cy + 4, 2, 8), col);
            Fill(new Rect(cx - 12, cy - 1, 8, 2), col);
            Fill(new Rect(cx + 4, cy - 1, 8, 2), col);
        }

        /// <summary>Aviso del brote de árbol más cercano con el tiempo que le falta.</summary>
        void DrawTreeInfo(float W, float H)
        {
            if (Game.Building || Game.Player == null) return;
            TreeNode n = TreeNode.NearestGrowing(Game.Player.transform.position, 3.5f);
            if (n == null) return;
            string t = (n.Planted ? "Brote plantado" : "Brote de árbol") + " · crece en " + TreeNode.FormatTime(n.SecondsLeft);
            Fill(new Rect(W * 0.5f - 170f, H * 0.66f, 340f, 28f), new Color(0f, 0f, 0f, 0.5f));
            GUI.Label(new Rect(W * 0.5f - 164f, H * 0.66f + 2f, 330f, 26f), t, new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
        }

        /// <summary>Barra de piezas del modo construcción.</summary>
        void DrawBuild(float W, float H)
        {
            Builder b = Builder.Instance;
            if (!Game.Building || b == null) return;
            float sw = 116f, gap = 8f;
            float total = 5f * sw + 4f * gap;
            float x0 = (W - total) * 0.5f, y = H - 98f;

            string info = "CONSTRUIR  Nivel " + b.Level + " (rueda)  Madera " + Game.Wood + "  Clic izq.: colocar  Clic der. o Supr: quitar  X: salir";
            Fill(new Rect(x0 - 6f, y - 62f, total + 12f, 26f), new Color(0f, 0f, 0f, 0.55f));
            GUI.Label(new Rect(x0, y - 62f, total, 26f), info, new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
            if (!string.IsNullOrEmpty(b.Hint))
            {
                Fill(new Rect(x0 - 6f, y - 34f, total + 12f, 26f), new Color(0.45f, 0.08f, 0.08f, 0.7f));
                GUI.Label(new Rect(x0, y - 34f, total, 26f), b.Hint, new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
            }

            for (int i = 0; i < 5; i++)
            {
                Rect r = new Rect(x0 + i * (sw + gap), y, sw, 62f);
                Fill(new Rect(r.x - 3f, r.y - 3f, r.width + 6f, r.height + 6f), i == b.Sel ? new Color(1f, 0.9f, 0.45f) : new Color(0.3f, 0.26f, 0.32f));
                Fill(r, new Color(0.15f, 0.12f, 0.18f, 0.95f));
                GUIStyle c = new GUIStyle(small) { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold };
                GUI.Label(new Rect(r.x, r.y + 4f, r.width, 24f), (i + 1) + "  " + Builder.Names[i], c);
                bool ok = Game.Wood >= Builder.Cost[i];
                GUIStyle c2 = new GUIStyle(small) { alignment = TextAnchor.UpperCenter };
                c2.normal.textColor = ok ? new Color(0.85f, 0.95f, 0.8f) : new Color(1f, 0.55f, 0.5f);
                GUI.Label(new Rect(r.x, r.y + 30f, r.width, 24f), Builder.Cost[i] + " madera", c2);
            }
        }

        void DrawDevBadge(float H)
        {
            if (!Settings.Dev) return;
            string t = "DEV (" + GameInput.KeyName(GameInput.Get(Act.Dev, 0)) + ")";
            if (Game.GodMode) t += "  invulnerable";
            if (Game.InfMana) t += "  maná inf.";
            if (Game.InfAmmo) t += "  munición inf.";
            Fill(new Rect(14, H - 62f, 320, 22), new Color(0.35f, 0.05f, 0.45f, 0.75f));
            GUI.Label(new Rect(20, H - 64f, 320, 24), t, small);
        }

        void DrawFlash(float W, float H)
        {
            float left = Game.FlashUntil - Time.unscaledTime;
            if (left <= 0f) return;
            float a = Mathf.Clamp01(left / Game.FlashDuration);
            Fill(new Rect(0, 0, W, H), new Color(1f, 0.97f, 0.85f, a * a * 0.9f));
        }

        void DrawItems(float W)
        {
            string t = "";
            if (Game.HasBow) t += "Flechas " + Game.Arrows + "   ";
            if (Game.HasBombs) t += "Bombas " + Game.Bombs + "   ";
            t += "Gemas " + Game.Gems + "   Armadura " + Game.ArmorLevel;
            if (Game.HasAxe || Game.Wood > 0) t += "   Madera " + Game.Wood;
            if (Game.Seeds > 0) t += "   Semillas " + Game.Seeds;
            Fill(new Rect(14, 66, 560, 28), new Color(0f, 0f, 0f, 0.35f));
            GUI.Label(new Rect(20, 69, 560, 26), t, small);
        }

        void DrawObjective(float W)
        {
            if (string.IsNullOrEmpty(Game.Objective)) return;
            float ox = W - 592f;   // a la izquierda del minimapa
            Fill(new Rect(ox, 14, 380, 52), new Color(0f, 0f, 0f, 0.45f));
            GUI.Label(new Rect(ox + 8, 16, 360, 20), "OBJETIVO", small);
            GUI.Label(new Rect(ox + 8, 34, 364, 30), Game.Objective, small);
        }

        void DrawPartner()
        {
            Partner p = Game.Partner;
            if (p == null) return;
            Fill(new Rect(14, 100, 160, 14), new Color(0f, 0f, 0f, 0.4f));
            Fill(new Rect(16, 102, 156f * Mathf.Clamp01(p.Morale), 10), p.Downed ? new Color(0.6f, 0.6f, 0.7f) : new Color(0.98f, 0.62f, 0.72f));
            GUI.Label(new Rect(180, 96, 200, 20), p.Downed ? "Pareja: desplomada" : "Pareja: " + (p.Order == PartnerOrder.Follow ? "te sigue" : p.Order == PartnerOrder.Wait ? "espera" : "refugio"), small);
        }

        void DrawBow(float W, float H)
        {
            PlayerCombat c = Game.Player.GetComponent<PlayerCombat>();
            if (c == null || !c.Aiming) return;
            float cx = W * 0.5f, cy = H * 0.5f;
            Fill(new Rect(cx - 60, cy + 120, 120, 8), new Color(0f, 0f, 0f, 0.5f));
            Fill(new Rect(cx - 59, cy + 121, 118f * c.BowCharge, 6), new Color(1f, 0.85f, 0.35f));
        }

        void DrawPrompt(float W, float H)
        {
            Interactable it = Interactable.Nearest(Game.Player.transform.position);
            if (it == null) return;
            Fill(new Rect(W * 0.5f - 160, H - 150, 320, 34), new Color(0f, 0f, 0f, 0.6f));
            GUI.Label(new Rect(W * 0.5f - 150, H - 146, 300, 30), "[E]  " + it.Prompt, label);
        }

        void DrawDialog(float W, float H)
        {
            if (Time.unscaledTime > Game.DialogUntil || string.IsNullOrEmpty(Game.DialogText)) return;
            Rect panel = new Rect(W * 0.5f - 340, H - 130, 680, 100);
            Fill(new Rect(panel.x - 3, panel.y - 3, panel.width + 6, panel.height + 6), new Color(0.95f, 0.78f, 0.4f, 0.9f));
            Fill(panel, new Color(0.18f, 0.14f, 0.2f, 0.92f));
            GUI.Label(new Rect(panel.x + 14, panel.y + 6, 300, 24), Game.DialogSpeaker, label);
            GUI.Label(new Rect(panel.x + 14, panel.y + 30, panel.width - 28, 66), Game.DialogText, box);
        }

        void DrawToast(float W, float H)
        {
            if (Time.unscaledTime > Game.ToastUntil || string.IsNullOrEmpty(Game.Toast)) return;
            Fill(new Rect(W * 0.5f - 260, 90, 520, 36), new Color(0f, 0f, 0f, 0.55f));
            GUI.Label(new Rect(W * 0.5f - 250, 94, 500, 30), Game.Toast, new GUIStyle(label) { alignment = TextAnchor.MiddleCenter });
        }

        void DrawHint(float W, float H)
        {
            if (Director == null) return;
            string h = Director.CurrentHint();
            if (string.IsNullOrEmpty(h)) return;
            GUI.Label(new Rect(18, H - 34, W - 36, 24), h, small);
        }

        void DrawLockMarker(float s)
        {
            PlayerController pc = Game.Player;
            if (pc == null || pc.LockTarget == null || Camera.main == null) return;
            Vector3 sp = Camera.main.WorldToScreenPoint(pc.LockTarget.AimPoint + Vector3.up * 0.7f);
            if (sp.z <= 0f) return;
            float x = sp.x / s, y = (Screen.height - sp.y) / s;
            Fill(new Rect(x - 10, y - 10, 20, 20), new Color(0.25f, 0.15f, 0.05f, 0.9f));
            Fill(new Rect(x - 7, y - 7, 14, 14), new Color(1f, 0.85f, 0.25f, 0.95f));
        }
    }
}
