using System.Collections.Generic;
using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Paneles a pantalla completa que pausan el juego: mapa grande (M), inventario (I) y panel de modo dev (F1).
    /// Pulsar de nuevo la misma tecla (o Esc) los cierra.
    /// </summary>
    public class Panels : MonoBehaviour
    {
        public static Panels Instance;

        Texture2D white, circle, btnN, btnH, btnA, btnOff;
        GUIStyle title, lbl, small, desc, btn, btnSelStyle, countStyle;
        PrologueDirector director;
        Transform spawnRoot;
        int sel;
        Vector2 devScroll;
        string devMsg = "";

        void Awake()
        {
            Instance = this;
            director = GetComponent<PrologueDirector>();
            white = Solid(Color.white);
            btnN = Solid(new Color(0.96f, 0.80f, 0.45f));
            btnH = Solid(new Color(1f, 0.90f, 0.62f));
            btnA = Solid(new Color(0.85f, 0.65f, 0.30f));
            btnOff = Solid(new Color(0.45f, 0.40f, 0.38f));

            circle = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32f, 32f));
                    circle.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(32f - d)));
                }
            circle.Apply();
        }

        static Texture2D Solid(Color c)
        {
            Texture2D t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnApplicationQuit() { IslandMap.Save(); }

        void OnApplicationPause(bool paused) { if (paused) IslandMap.Save(); }

        // ---------- Apertura y cierre ----------

        void Open(UiPanel p)
        {
            Game.Panel = p;
            Game.State = GameState.Paused;
            Game.PauseFrame = Time.frameCount;
            Time.timeScale = 0f;
            if (p == UiPanel.Map && Minimap.Instance != null) Minimap.Instance.OpenLarge();
            if (p == UiPanel.Inventory) sel = Mathf.Clamp(sel, 0, 11);
            devMsg = "";
            Sfx.Play("ui", 1.1f, 0.6f);
        }

        void Close()
        {
            Game.Panel = UiPanel.None;
            Game.State = GameState.Playing;
            Time.timeScale = 1f;
            Sfx.Play("ui", 0.9f, 0.6f);
        }

        public void ClearSpawned()
        {
            if (spawnRoot != null) Destroy(spawnRoot.gameObject);
            spawnRoot = null;
        }

        void Update()
        {
            if (Game.State == GameState.Menu || Game.State == GameState.Ended)
            {
                Game.Panel = UiPanel.None;
                if (Game.State == GameState.Menu) ClearSpawned();
                return;
            }
            if (Game.Player == null) return;

            bool playing = Game.State == GameState.Playing;
            bool open = Game.State == GameState.Paused && Game.Panel != UiPanel.None;
            if (!playing && !open) return;

            UiPanel want = UiPanel.None;
            if (GameInput.Pressed(Act.Map)) want = UiPanel.Map;
            else if (GameInput.Pressed(Act.Inventory)) want = UiPanel.Inventory;
            else if (Settings.Dev && GameInput.Pressed(Act.Dev)) want = UiPanel.Dev;
            if (want == UiPanel.None) return;

            if (open && Game.Panel == want) Close();
            else Open(want);
        }

        void LateUpdate()
        {
            if (Game.State != GameState.Paused || Game.Panel == UiPanel.None) return;
            // Esc cierra (en LateUpdate para que el director no lo tome como "pausa" en el mismo fotograma).
            if (Time.frameCount > Game.PauseFrame && GameInput.Pressed(Act.Pause)) { Close(); return; }
            if (Game.Panel == UiPanel.Map && Minimap.Instance != null) Minimap.Instance.RenderLarge();
        }

        // ---------- Estilos ----------

        void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            title.normal.textColor = new Color(1f, 0.96f, 0.82f);
            lbl = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            lbl.normal.textColor = Color.white;
            small = new GUIStyle(lbl) { fontSize = 14, fontStyle = FontStyle.Normal };
            desc = new GUIStyle(lbl) { fontSize = 16, fontStyle = FontStyle.Normal, wordWrap = true, alignment = TextAnchor.UpperLeft };
            countStyle = new GUIStyle(lbl) { fontSize = 15, alignment = TextAnchor.LowerRight };
            btn = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
            btn.normal.background = btnN;
            btn.hover.background = btnH;
            btn.active.background = btnA;
            btn.normal.textColor = btn.hover.textColor = btn.active.textColor = new Color(0.30f, 0.18f, 0.10f);
            btnSelStyle = new GUIStyle(btn);
            btnSelStyle.normal.background = btnOff;
            btnSelStyle.hover.background = btnOff;
            btnSelStyle.normal.textColor = btnSelStyle.hover.textColor = new Color(0.80f, 0.76f, 0.72f);
        }

        void Fill(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, white);
            GUI.color = old;
        }

        void Disc(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, circle);
            GUI.color = old;
        }

        void RotFill(Rect r, float ang, Vector2 pivot, Color c)
        {
            Matrix4x4 m = GUI.matrix;
            GUIUtility.RotateAroundPivot(ang, pivot);
            Fill(r, c);
            GUI.matrix = m;
        }

        bool Btn(Rect r, string text, bool enabled = true)
        {
            bool old = GUI.enabled;
            GUI.enabled = enabled;
            bool hit = GUI.Button(r, text, enabled ? btn : btnSelStyle);
            GUI.enabled = old;
            if (hit && enabled) Sfx.Play("ui", 1.2f, 0.6f);
            return hit && enabled;
        }

        void Frame(Rect p)
        {
            Fill(new Rect(p.x - 4, p.y - 4, p.width + 8, p.height + 8), new Color(0.95f, 0.78f, 0.4f, 0.95f));
            Fill(p, new Color(0.15f, 0.12f, 0.18f, 0.97f));
        }

        // ---------- GUI ----------

        void OnGUI()
        {
            if (Game.State != GameState.Paused || Game.Panel == UiPanel.None) return;
            Styles();
            GUI.depth = -100;
            float s = Mathf.Max(0.75f, Screen.height / 720f);
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float W = Screen.width / s, H = Screen.height / s;

            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.72f));
            switch (Game.Panel)
            {
                case UiPanel.Map: DrawMap(W, H); break;
                case UiPanel.Inventory: DrawInventory(W, H); break;
                case UiPanel.Dev: DrawDev(W, H); break;
            }
            GUI.matrix = old;
        }

        // ---------- Mapa grande ----------

        void DrawMap(float W, float H)
        {
            Minimap mm = Minimap.Instance;
            float side = Mathf.Min(H - 100f, W - 380f);
            Rect map = new Rect(40f, (H - side) * 0.5f + 10f, side, side);
            GUI.Label(new Rect(40f, 12f, 400f, 40f), "Mapa", title);
            if (mm != null) mm.DrawLarge(map);

            float x = map.xMax + 40f;
            float y = map.y + 4f;
            GUI.Label(new Rect(x, y, 280f, 30f), "Leyenda", lbl);
            if (mm != null) mm.DrawLegend(x, y + 36f);
            y += 36f + 5f * 26f + 10f;

            bool island = IslandMap.Active;
            if (mm != null) GUI.Label(new Rect(x, y, 300f, 28f), "Zoom: x" + mm.ZoomFactor.ToString("0.0"), small);
            if (island) GUI.Label(new Rect(x, y + 24f, 300f, 28f), "Descubierto: " + IslandMap.Percent.ToString("0.0") + " % de la isla", small);
            y += 56f;
            GUI.Label(new Rect(x, y, 310f, 150f),
                "Arrastra con el ratón para mover el mapa.\nRueda del ratón: acercar / alejar (hasta x50).\n" +
                (island ? "Solo se dibuja lo que has recorrido; se guarda para esta semilla." : "Muestra el terreno de la zona."), desc);
            y += 150f;
            if (mm != null && Btn(new Rect(x, y, 260f, 40f), "Centrar en ti")) mm.OpenLarge();
            if (mm != null && Btn(new Rect(x, y + 48f, 260f, 40f), island ? "Ver toda la isla" : "Ver todo")) mm.ShowAll();
            if (Btn(new Rect(x, y + 96f, 260f, 40f), "Cerrar (" + Keys(Act.Map) + ")")) Close();
        }

        // ---------- Inventario ----------

        class Item
        {
            public string Name, Desc, Count;
            public bool Owned;
            public int Icon;
        }

        string Keys(Act a)
        {
            string k0 = GameInput.KeyName(GameInput.Get(a, 0));
            KeyCode k1c = GameInput.Get(a, 1);
            return k1c == KeyCode.None ? k0 : k0 + " / " + GameInput.KeyName(k1c);
        }

        List<Item> BuildItems()
        {
            List<Item> l = new List<Item>();
            l.Add(new Item { Name = "Espada", Icon = 0, Owned = Game.HasSword, Count = "",
                Desc = "Combo de 3 golpes; el tercero es el más fuerte. Controles: " + Keys(Act.Attack) + ". Cada golpe que conecta recarga un poco de maná." });
            l.Add(new Item { Name = "Escudo", Icon = 1, Owned = Game.HasShield, Count = "",
                Desc = "Mantén " + Keys(Act.Block) + " para bloquear. Si bloqueas justo antes del golpe haces un parry: aturde a los enemigos cercanos y recarga maná." });
            l.Add(new Item { Name = "Arco", Icon = 2, Owned = Game.HasBow, Count = Game.HasBow ? Game.Arrows.ToString() : "",
                Desc = "Mantén " + Keys(Act.Bow) + " para apuntar y suelta para disparar. Cuanto más cargas, más lejos y más fuerte va la flecha. Activa interruptores a distancia." });
            l.Add(new Item { Name = "Bombas", Icon = 3, Owned = Game.HasBombs, Count = Game.HasBombs ? Game.Bombs.ToString() : "",
                Desc = "Se lanzan con " + Keys(Act.Bomb) + ". Rompen muros y rocas agrietadas y dañan a los enemigos, pero también a ti: mantén la distancia." });
            l.Add(new Item { Name = "Varita de fuego", Icon = 4, Owned = Game.HasWand, Count = Game.HasWand ? Mathf.RoundToInt(Game.Mana) + "/" + Mathf.RoundToInt(Game.MaxMana) : "",
                Desc = "Lanza bolas de fuego con " + Keys(Act.Wand) + " (18 de maná). Daña slimes y quema las enredaderas; el fuego se contagia entre las matas. El maná se recupera solo y al golpear o hacer parry." });
            l.Add(new Item { Name = "Armadura", Icon = 5, Owned = true, Count = "Nivel " + Game.ArmorLevel,
                Desc = "Más nivel, más vida máxima (nivel " + Game.ArmorLevel + ": " + (5 + 2 * (Game.ArmorLevel - 1)) + " puntos). Elige abajo cuál llevar puesta; los niveles se desbloquean en la mesa de trabajo." });
            l.Add(new Item { Name = "Cuero reforzado", Icon = 6, Owned = Game.HasLeather, Count = "",
                Desc = "Material del cofre de las ruinas. Con él se crea la armadura de nivel 2 en la mesa de trabajo de casa." });
            l.Add(new Item { Name = "Gemas", Icon = 7, Owned = true, Count = Game.Gems.ToString(),
                Desc = "Moneda del mundo: la sueltan los enemigos, las rocas agrietadas y los cofres. Servirán para mejorar el equipo." });
            l.Add(new Item { Name = "Madera", Icon = 8, Owned = true, Count = Game.Wood.ToString(),
                Desc = "Material de construcción y mejoras de armadura." });
            return l;
        }

        void DrawInventory(float W, float H)
        {
            List<Item> items = BuildItems();
            Rect p = new Rect((W - 960f) * 0.5f, (H - 560f) * 0.5f, 960f, 560f);
            Frame(p);
            GUI.Label(new Rect(p.x + 24f, p.y + 10f, 400f, 44f), "Inventario", title);

            // Teclado: flechas / WASD mueven la selección.
            Event e = Event.current;
            if (e != null && e.type == EventType.KeyDown)
            {
                int dx = 0, dy = 0;
                if (e.keyCode == KeyCode.LeftArrow || e.keyCode == KeyCode.A) dx = -1;
                else if (e.keyCode == KeyCode.RightArrow || e.keyCode == KeyCode.D) dx = 1;
                else if (e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.W) dy = -1;
                else if (e.keyCode == KeyCode.DownArrow || e.keyCode == KeyCode.S) dy = 1;
                if (dx != 0 || dy != 0)
                {
                    sel = Mathf.Clamp(sel + dx + dy * 4, 0, 11);
                    Sfx.Play("ui", 1.3f, 0.4f);
                    e.Use();
                }
            }

            // Casillas (4 x 3)
            float gx = p.x + 24f, gy = p.y + 70f, cell = 100f, gap = 12f;
            for (int i = 0; i < 12; i++)
            {
                int cx = i % 4, cy = i / 4;
                Rect r = new Rect(gx + cx * (cell + gap), gy + cy * (cell + gap), cell, cell);
                bool has = i < items.Count;
                bool owned = has && items[i].Owned;
                Fill(new Rect(r.x - 3f, r.y - 3f, r.width + 6f, r.height + 6f), i == sel ? new Color(1f, 0.92f, 0.5f) : new Color(0.35f, 0.30f, 0.38f));
                Fill(r, owned ? new Color(0.27f, 0.22f, 0.32f) : new Color(0.17f, 0.15f, 0.20f));
                if (has) DrawIcon(items[i].Icon, r, owned);
                if (has && owned && !string.IsNullOrEmpty(items[i].Count)) GUI.Label(new Rect(r.x, r.y, r.width - 6f, r.height - 4f), items[i].Count, countStyle);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { sel = i; Sfx.Play("ui", 1.3f, 0.4f); }
            }

            // Estadísticas bajo la cuadrícula
            float sy = gy + 3 * (cell + gap) + 6f;
            Health hp = Game.Player != null ? Game.Player.GetComponent<Health>() : null;
            string stats = "Vida " + (hp != null ? Mathf.CeilToInt(hp.Hp) + "/" + Mathf.CeilToInt(hp.Max) : "-") +
                           "     Enemigos derrotados: " + Game.Kills;
            GUI.Label(new Rect(gx, sy, 470f, 30f), stats, small);

            // Detalle
            Rect d = new Rect(p.x + 520f, p.y + 70f, 416f, 400f);
            Fill(d, new Color(0.10f, 0.08f, 0.13f, 0.9f));
            Item it = sel < items.Count ? items[sel] : null;
            if (it == null)
            {
                GUI.Label(new Rect(d.x + 16f, d.y + 16f, d.width - 32f, 40f), "Casilla vacía", lbl);
            }
            else
            {
                Rect big = new Rect(d.x + 16f, d.y + 16f, 84f, 84f);
                Fill(big, new Color(0.27f, 0.22f, 0.32f));
                DrawIcon(it.Icon, big, it.Owned);
                GUI.Label(new Rect(d.x + 112f, d.y + 20f, d.width - 124f, 34f), it.Owned ? it.Name : "???", lbl);
                if (it.Owned && !string.IsNullOrEmpty(it.Count)) GUI.Label(new Rect(d.x + 112f, d.y + 54f, d.width - 124f, 30f), it.Count, small);
                GUI.Label(new Rect(d.x + 16f, d.y + 112f, d.width - 32f, 170f), it.Owned ? it.Desc : "Aún no has encontrado este objeto.", desc);

                if (it.Icon == 5 && director != null)
                {
                    GUI.Label(new Rect(d.x + 16f, d.y + 292f, d.width - 32f, 26f), "Armadura puesta:", small);
                    for (int n = 1; n <= 4; n++)
                    {
                        bool unlocked = n <= Game.ArmorMax;
                        string t = n == Game.ArmorLevel ? "Nivel " + n + " (puesta)" : unlocked ? "Nivel " + n : "Nivel " + n + " (bloq.)";
                        Rect b = new Rect(d.x + 16f + ((n - 1) % 2) * 196f, d.y + 322f + ((n - 1) / 2) * 46f, 188f, 38f);
                        if (Btn(b, t, unlocked && n != Game.ArmorLevel)) director.SetArmor(n, false);
                    }
                }
            }

            GUI.Label(new Rect(p.x + 24f, p.yMax - 44f, 600f, 30f), "Clic o flechas: elegir objeto.   " + Keys(Act.Inventory) + " o Esc: cerrar", small);
            if (Btn(new Rect(p.xMax - 190f, p.yMax - 54f, 166f, 40f), "Cerrar")) Close();
        }

        void DrawIcon(int kind, Rect r, bool owned)
        {
            float s = r.width / 96f;
            float cx = r.x + r.width * 0.5f, cy = r.y + r.height * 0.5f;
            Vector2 c = new Vector2(cx, cy);
            float a = owned ? 1f : 0.28f;
            switch (kind)
            {
                case 0: // espada
                    RotFill(new Rect(cx - 4f * s, cy - 36f * s, 8f * s, 54f * s), 45f, c, new Color(0.85f, 0.88f, 0.94f, a));
                    RotFill(new Rect(cx - 16f * s, cy + 16f * s, 32f * s, 6f * s), 45f, c, new Color(0.85f, 0.70f, 0.30f, a));
                    RotFill(new Rect(cx - 3f * s, cy + 22f * s, 6f * s, 16f * s), 45f, c, new Color(0.50f, 0.32f, 0.18f, a));
                    break;
                case 1: // escudo
                    Fill(new Rect(cx - 24f * s, cy - 30f * s, 48f * s, 56f * s), new Color(0.88f, 0.75f, 0.30f, a));
                    Fill(new Rect(cx - 20f * s, cy - 26f * s, 40f * s, 48f * s), new Color(0.30f, 0.45f, 0.75f, a));
                    Disc(new Rect(cx - 9f * s, cy - 11f * s, 18f * s, 18f * s), new Color(0.90f, 0.75f, 0.30f, a));
                    break;
                case 2: // arco
                    RotFill(new Rect(cx - 3f * s, cy - 34f * s, 6f * s, 26f * s), -22f, new Vector2(cx, cy - 21f * s), new Color(0.60f, 0.40f, 0.22f, a));
                    Fill(new Rect(cx - 3f * s, cy - 20f * s, 6f * s, 40f * s), new Color(0.60f, 0.40f, 0.22f, a));
                    RotFill(new Rect(cx - 3f * s, cy + 8f * s, 6f * s, 26f * s), 22f, new Vector2(cx, cy + 21f * s), new Color(0.60f, 0.40f, 0.22f, a));
                    Fill(new Rect(cx + 14f * s, cy - 32f * s, 2f * s, 64f * s), new Color(0.92f, 0.92f, 0.85f, a));
                    break;
                case 3: // bomba
                    Disc(new Rect(cx - 24f * s, cy - 18f * s, 48f * s, 48f * s), new Color(0.15f, 0.15f, 0.18f, a));
                    RotFill(new Rect(cx + 8f * s, cy - 32f * s, 5f * s, 18f * s), 30f, new Vector2(cx + 10f * s, cy - 16f * s), new Color(0.65f, 0.50f, 0.30f, a));
                    Disc(new Rect(cx + 14f * s, cy - 40f * s, 14f * s, 14f * s), new Color(1f, 0.6f, 0.15f, a));
                    break;
                case 4: // varita
                    RotFill(new Rect(cx - 3f * s, cy - 24f * s, 6f * s, 62f * s), 40f, c, new Color(0.50f, 0.32f, 0.18f, a));
                    Disc(new Rect(cx - 30f * s, cy - 36f * s, 26f * s, 26f * s), new Color(1f, 0.5f, 0.1f, a));
                    Disc(new Rect(cx - 24f * s, cy - 30f * s, 14f * s, 14f * s), new Color(1f, 0.9f, 0.5f, a));
                    break;
                case 5: // armadura
                {
                    Color[] cols = { new Color(0.62f, 0.50f, 0.38f), new Color(0.45f, 0.70f, 0.45f), new Color(0.65f, 0.72f, 0.82f), new Color(0.95f, 0.80f, 0.30f) };
                    Color ac = cols[Mathf.Clamp(Game.ArmorLevel - 1, 0, 3)];
                    ac.a = a;
                    Fill(new Rect(cx - 20f * s, cy - 24f * s, 40f * s, 52f * s), ac);
                    Fill(new Rect(cx - 34f * s, cy - 24f * s, 14f * s, 30f * s), ac * 0.85f);
                    Fill(new Rect(cx + 20f * s, cy - 24f * s, 14f * s, 30f * s), ac * 0.85f);
                    Fill(new Rect(cx - 8f * s, cy - 24f * s, 16f * s, 8f * s), new Color(0.15f, 0.12f, 0.18f, a));
                    break;
                }
                case 6: // cuero
                    Fill(new Rect(cx - 30f * s, cy - 20f * s, 60f * s, 40f * s), new Color(0.55f, 0.36f, 0.20f, a));
                    for (int i = 0; i < 5; i++) Fill(new Rect(cx - 24f * s + i * 11f * s, cy - 2f * s, 6f * s, 2f * s), new Color(0.88f, 0.75f, 0.5f, a));
                    break;
                case 7: // gema
                    RotFill(new Rect(cx - 20f * s, cy - 20f * s, 40f * s, 40f * s), 45f, c, new Color(0.30f, 0.85f, 0.95f, a));
                    RotFill(new Rect(cx - 8f * s, cy - 8f * s, 16f * s, 16f * s), 45f, c, new Color(0.85f, 1f, 1f, a));
                    break;
                case 8: // madera
                    Fill(new Rect(cx - 30f * s, cy - 24f * s, 60f * s, 16f * s), new Color(0.55f, 0.38f, 0.22f, a));
                    Fill(new Rect(cx - 30f * s, cy - 6f * s, 60f * s, 16f * s), new Color(0.62f, 0.44f, 0.26f, a));
                    Fill(new Rect(cx - 30f * s, cy + 12f * s, 60f * s, 16f * s), new Color(0.50f, 0.34f, 0.20f, a));
                    break;
            }
        }

        // ---------- Modo dev ----------

        Vector3 FrontPoint(float dist)
        {
            Transform t = Game.Player.transform;
            Vector3 p = t.position + t.forward * dist;
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 8f, Vector3.down, out hit, 40f, ~0, QueryTriggerInteraction.Ignore)) p.y = hit.point.y;
            return p;
        }

        Transform SpawnRoot()
        {
            if (spawnRoot == null) spawnRoot = new GameObject("DevSpawned").transform;
            return spawnRoot;
        }

        void Teleport(Vector3 pos)
        {
            if (director != null && director.IslandMode && IslandWorld.Instance != null) IslandWorld.Instance.EnsureAround(pos, 1);
            Game.Player.Teleport(pos);
            Game.Player.ClearLock();
            if (Game.Player.Rig != null) Game.Player.Rig.Snap();
            if (Game.Partner != null) Game.Partner.Teleport(pos + Game.Player.transform.right * 1.5f);
            devMsg = "Teletransportado";
        }

        void GiveAll()
        {
            Game.HasSword = Game.HasShield = Game.HasBow = Game.HasBombs = Game.HasWand = Game.HasLeather = true;
            Game.Arrows = Mathf.Max(Game.Arrows, 30);
            Game.Bombs = Mathf.Max(Game.Bombs, 20);
            Game.Gems += 50;
            Game.Wood += 20;
            Game.Mana = Game.MaxMana;
            if (director != null) { director.SetArmor(4, true); director.RefreshModel(); }
            devMsg = "Equipo completo entregado";
        }

        void HealAll()
        {
            Health hp = Game.Player.GetComponent<Health>();
            if (hp != null) hp.Heal(999f);
            Game.Mana = Game.MaxMana;
            if (Game.Partner != null) Game.Partner.Recover();
            devMsg = "Vida, maná y pareja restaurados";
        }

        void KillAll()
        {
            int n = 0;
            Lockable[] snap = Lockable.All.ToArray();
            for (int i = 0; i < snap.Length; i++)
            {
                if (snap[i] == null || !snap[i].IsEnemy) continue;
                EnemySlime e = snap[i].GetComponent<EnemySlime>();
                if (e != null) { e.Hurt(999f, Game.Player.transform.position, 0f); n++; }
            }
            devMsg = n + " enemigos eliminados";
        }

        void DrawDev(float W, float H)
        {
            Rect p = new Rect((W - 1000f) * 0.5f, (H - 620f) * 0.5f, 1000f, 620f);
            Frame(p);
            GUI.Label(new Rect(p.x + 24f, p.y + 8f, 600f, 44f), "Modo desarrollador", title);
            GUI.Label(new Rect(p.x + 24f, p.y + 50f, 950f, 26f),
                "El juego está en pausa. Los objetos aparecen 3 m delante de ti. " + Keys(Act.Dev) + " o Esc: cerrar.", small);
            if (!string.IsNullOrEmpty(devMsg)) GUI.Label(new Rect(p.x + 24f, p.yMax - 40f, 700f, 28f), devMsg, lbl);

            float colW = 300f, top = p.y + 84f, h = p.height - 140f;
            bool island = director != null && director.IslandMode;

            // Columna 1: trucos
            GUILayout.BeginArea(new Rect(p.x + 20f, top, colW, h));
            GUILayout.Label("TRUCOS", lbl);
            if (GUILayout.Button((Game.GodMode ? "[X] " : "[  ] ") + "Invulnerable", btn, GUILayout.Height(34f))) Game.GodMode = !Game.GodMode;
            if (GUILayout.Button((Game.InfMana ? "[X] " : "[  ] ") + "Maná infinito", btn, GUILayout.Height(34f))) Game.InfMana = !Game.InfMana;
            if (GUILayout.Button((Game.InfAmmo ? "[X] " : "[  ] ") + "Flechas y bombas infinitas", btn, GUILayout.Height(34f))) Game.InfAmmo = !Game.InfAmmo;
            GUILayout.Space(6f);
            if (GUILayout.Button("Dar todo el equipo", btn, GUILayout.Height(34f))) GiveAll();
            if (GUILayout.Button("Curar todo (vida, maná, pareja)", btn, GUILayout.Height(34f))) HealAll();
            if (GUILayout.Button("Eliminar todos los enemigos", btn, GUILayout.Height(34f))) KillAll();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+10 gemas", btn, GUILayout.Height(32f))) { Game.Gems += 10; devMsg = "+10 gemas"; }
            if (GUILayout.Button("+10 madera", btn, GUILayout.Height(32f))) { Game.Wood += 10; devMsg = "+10 madera"; }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+5 bombas", btn, GUILayout.Height(32f))) { Game.HasBombs = true; Game.Bombs += 5; devMsg = "+5 bombas"; }
            if (GUILayout.Button("+10 flechas", btn, GUILayout.Height(32f))) { Game.HasBow = true; Game.Arrows += 10; devMsg = "+10 flechas"; }
            GUILayout.EndHorizontal();
            GUILayout.Label("Armadura", small);
            GUILayout.BeginHorizontal();
            for (int n = 1; n <= 4; n++)
                if (GUILayout.Button("N" + n, btn, GUILayout.Height(32f)) && director != null) { director.SetArmor(n, true); devMsg = "Armadura nivel " + n; }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // Columna 2: objetos
            GUILayout.BeginArea(new Rect(p.x + 20f + colW + 20f, top, colW + 40f, h));
            devScroll = GUILayout.BeginScrollView(devScroll);
            GUILayout.Label("APARECER OBJETOS", lbl);
            Transform root = null;
            bool spawnClick = false;
            GUILayout.Label("Enemigos y recogibles", small);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Slime", btn, GUILayout.Height(32f))) { EnemySlime.Spawn(FrontPoint(3f) + Vector3.up * 0.1f, SpawnRoot()); spawnClick = true; devMsg = "Slime"; }
            if (GUILayout.Button("3 slimes", btn, GUILayout.Height(32f)))
            {
                Vector3 f = FrontPoint(4f);
                for (int i = 0; i < 3; i++) EnemySlime.Spawn(f + new Vector3((i - 1) * 1.6f, 0.1f, 0f), SpawnRoot());
                spawnClick = true; devMsg = "3 slimes";
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bombas", btn, GUILayout.Height(30f))) { Pickup.Spawn(FrontPoint(2.5f) + Vector3.up * 0.5f, PickupKind.Bombs, 3); devMsg = "Bolsa de bombas"; }
            if (GUILayout.Button("Flechas", btn, GUILayout.Height(30f))) { Pickup.Spawn(FrontPoint(2.5f) + Vector3.up * 0.5f, PickupKind.Arrows, 5); devMsg = "Flechas"; }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Gema", btn, GUILayout.Height(30f))) { Pickup.Spawn(FrontPoint(2.5f) + Vector3.up * 0.5f, PickupKind.Gem, 3); devMsg = "Gemas"; }
            if (GUILayout.Button("Corazón", btn, GUILayout.Height(30f))) { Pickup.Spawn(FrontPoint(2.5f) + Vector3.up * 0.5f, PickupKind.Heart, 1); devMsg = "Corazón"; }
            if (GUILayout.Button("Madera", btn, GUILayout.Height(30f))) { Pickup.Spawn(FrontPoint(2.5f) + Vector3.up * 0.5f, PickupKind.Wood, 2); devMsg = "Madera"; }
            GUILayout.EndHorizontal();

            GUILayout.Label("Puzles y mecánicas", small);
            if (GUILayout.Button("Enredadera suelta", btn, GUILayout.Height(32f)))
            {
                Vine.Create(SpawnRoot(), FrontPoint(3f), Game.Player.transform.eulerAngles.y + 90f, 1f);
                devMsg = "Enredadera (quémala con la varita)";
            }
            if (GUILayout.Button("Cofre rodeado de enredaderas", btn, GUILayout.Height(32f)))
            {
                Vector3 c = FrontPoint(4.5f);
                root = SpawnRoot();
                Chest ch = Chest.Create(root, c, Game.Player.transform.eulerAngles.y + 180f);
                ch.OnOpened = () => { Game.Gems += 5; Sfx.Play("pickup"); Game.Say("Cofre: +5 gemas", 3f); };
                for (int i = 0; i < 7; i++)
                {
                    float a = i * Mathf.PI * 2f / 7f;
                    Vector3 vp = c + new Vector3(Mathf.Cos(a) * 1.9f, 0f, Mathf.Sin(a) * 1.9f);
                    vp.y = c.y;
                    Vine.Create(root, vp, -a * Mathf.Rad2Deg + 90f, 1f);
                }
                devMsg = "Cofre con enredaderas";
            }
            if (GUILayout.Button("Cofre", btn, GUILayout.Height(32f)))
            {
                Chest ch = Chest.Create(SpawnRoot(), FrontPoint(3f), Game.Player.transform.eulerAngles.y + 180f);
                ch.OnOpened = () => { Game.Gems += 5; Sfx.Play("pickup"); Game.Say("Cofre: +5 gemas", 3f); };
                devMsg = "Cofre";
            }
            if (GUILayout.Button("Roca agrietada (usa bomba)", btn, GUILayout.Height(32f)))
            {
                Vector3 c = FrontPoint(4f);
                BreakableWall w = BreakableWall.Create(SpawnRoot(), c + Vector3.up, new Vector3(2.2f, 2f, 2.2f), new Color(0.55f, 0.52f, 0.50f));
                w.BurstSize = 0.35f;
                w.OnBroken = () => { Pickup.Spawn(c + new Vector3(0.7f, 0.5f, 0f), PickupKind.Bombs, 2); Pickup.Spawn(c + new Vector3(-0.7f, 0.5f, 0.3f), PickupKind.Gem, 2); };
                devMsg = "Roca agrietada";
            }
            if (GUILayout.Button("Muro agrietado grande (destello)", btn, GUILayout.Height(32f)))
            {
                Vector3 c = FrontPoint(5f);
                BreakableWall w = BreakableWall.Create(SpawnRoot(), c + Vector3.up * 1.7f, new Vector3(1f, 3.4f, 4f), new Color(0.52f, 0.48f, 0.46f));
                w.BurstSize = 1.5f;
                w.transform.rotation = Quaternion.Euler(0f, Game.Player.transform.eulerAngles.y + 90f, 0f);
                devMsg = "Muro agrietado";
            }

            GUILayout.Label("Efectos", small);
            if (GUILayout.Button("Bomba encendida", btn, GUILayout.Height(30f))) { Bomb.Spawn(FrontPoint(3f) + Vector3.up * 0.8f, Vector3.zero); devMsg = "Bomba"; }
            if (GUILayout.Button("Destello de luz", btn, GUILayout.Height(30f))) { LightBurst.Spawn(FrontPoint(3f) + Vector3.up * 1.2f, 1.2f, true); devMsg = "Destello"; }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            if (spawnClick) Sfx.Play("ui", 1.3f, 0.5f);

            // Columna 3: viaje
            GUILayout.BeginArea(new Rect(p.x + 20f + (colW + 20f) * 2f + 40f, top, colW - 60f, h));
            GUILayout.Label("VIAJE", lbl);
            if (!island)
            {
                if (GUILayout.Button("Casa", btn, GUILayout.Height(32f))) Teleport(PrologueWorld.PlayerStart);
                if (GUILayout.Button("Huerto", btn, GUILayout.Height(32f))) Teleport(PrologueWorld.GardenCenter + new Vector3(8f, 0.2f, 0f));
                if (GUILayout.Button("Campana", btn, GUILayout.Height(32f))) Teleport(PrologueWorld.BellPos + new Vector3(-3f, 0.2f, 0f));
                if (GUILayout.Button("Cofre con enredaderas", btn, GUILayout.Height(32f))) Teleport(new Vector3(13f, 0.2f, -4.5f));
                if (GUILayout.Button("Cueva: sala A", btn, GUILayout.Height(32f))) Teleport(new Vector3(41f, 0.2f, 30f));
                if (GUILayout.Button("Cueva: sala B", btn, GUILayout.Height(32f))) Teleport(new Vector3(63f, 0.2f, 30f));
                if (GUILayout.Button("Cueva: roca final", btn, GUILayout.Height(32f))) Teleport(new Vector3(80.5f, 0.2f, 30f));
            }
            else
            {
                if (GUILayout.Button("Campamento", btn, GUILayout.Height(32f))) Teleport(IslandTerrain.Camp + Vector3.up * 0.4f);
                if (GUILayout.Button("Cima de la montaña", btn, GUILayout.Height(32f))) Teleport(new Vector3(0f, IslandTerrain.Height(0f, 0f) + 0.6f, 0f));
                if (GUILayout.Button("Costa norte", btn, GUILayout.Height(32f))) { Vector3 q = new Vector3(0f, 0f, 150f); q.y = IslandTerrain.Height(q.x, q.z) + 0.6f; Teleport(q); }
                if (GUILayout.Button("Revelar todo el mapa", btn, GUILayout.Height(32f))) { IslandMap.RevealAll(); devMsg = "Mapa revelado"; }
                if (GUILayout.Button("Olvidar el mapa", btn, GUILayout.Height(32f))) { IslandMap.Forget(); devMsg = "Mapa borrado"; }
            }
            if (director != null && GUILayout.Button("Punto seguro", btn, GUILayout.Height(32f))) Teleport(director.Checkpoint);
            if (Game.Partner != null && GUILayout.Button("Pareja a mi lado", btn, GUILayout.Height(32f)))
            {
                Game.Partner.Teleport(Game.Player.transform.position + Game.Player.transform.right * 1.5f);
                devMsg = "Pareja a tu lado";
            }
            GUILayout.EndArea();

            if (Btn(new Rect(p.xMax - 190f, p.yMax - 54f, 166f, 40f), "Cerrar")) Close();
        }
    }
}
