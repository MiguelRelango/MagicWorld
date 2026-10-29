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
            DrawItems(W);
            DrawObjective(W);
            DrawPartner();
            DrawBow(W, H);
            DrawPrompt(W, H);
            DrawDialog(W, H);
            DrawToast(W, H);
            DrawHint(W, H);
            DrawLockMarker(s);

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

        void DrawItems(float W)
        {
            string t = "";
            if (Game.HasBow) t += "Flechas " + Game.Arrows + "   ";
            if (Game.HasBombs) t += "Bombas " + Game.Bombs + "   ";
            t += "Gemas " + Game.Gems + "   Armadura " + Game.ArmorLevel;
            Fill(new Rect(14, 50, 360, 28), new Color(0f, 0f, 0f, 0.35f));
            GUI.Label(new Rect(20, 53, 360, 26), t, small);
        }

        void DrawObjective(float W)
        {
            if (string.IsNullOrEmpty(Game.Objective)) return;
            Fill(new Rect(W - 380, 14, 366, 52), new Color(0f, 0f, 0f, 0.45f));
            GUI.Label(new Rect(W - 372, 16, 350, 20), "OBJETIVO", small);
            GUI.Label(new Rect(W - 372, 34, 352, 30), Game.Objective, small);
        }

        void DrawPartner()
        {
            Partner p = Game.Partner;
            if (p == null) return;
            Fill(new Rect(14, 84, 160, 14), new Color(0f, 0f, 0f, 0.4f));
            Fill(new Rect(16, 86, 156f * Mathf.Clamp01(p.Morale), 10), p.Downed ? new Color(0.6f, 0.6f, 0.7f) : new Color(0.98f, 0.62f, 0.72f));
            GUI.Label(new Rect(180, 80, 200, 20), p.Downed ? "Pareja: desplomada" : "Pareja: " + (p.Order == PartnerOrder.Follow ? "te sigue" : p.Order == PartnerOrder.Wait ? "espera" : "refugio"), small);
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
