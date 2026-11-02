using System.Collections.Generic;
using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// El interior de una mazmorra: laberinto generado con semilla, antorchas en las paredes, goblins,
    /// cofres en los callejones sin salida y, al fondo, una jaula con un aldeano al que rescatar.
    /// Se construye muy lejos de la isla (ver DungeonSite.Origin) y se conserva mientras dure la partida.
    /// </summary>
    public class DungeonInstance
    {
        const int W = 9, H = 9;
        const float C = 6f;          // tamaño de cada celda
        const float T = 0.8f;        // grosor de los muros
        const float WallH = 3.8f;

        public DungeonSite Site;
        public Transform Root;
        public Vector3 Spawn;
        public Vector3 PadPos;
        public Vector3 PrisonPos;
        public bool Freed, Collected;
        public int ChestsTotal, ChestsOpened;

        readonly List<EnemySlime> goblins = new List<EnemySlime>();
        readonly List<GameObject> doorBars = new List<GameObject>();
        GameObject cage, villagerGo;
        DungeonVillager villager;
        bool greeted;
        float warnAt;

        readonly bool[,] hw = new bool[W, H + 1];   // muro en el lado sur de la celda (i, j)
        readonly bool[,] vw = new bool[W + 1, H];   // muro en el lado oeste de la celda (i, j)
        readonly int[,] dist = new int[W, H];
        System.Random rng;
        Vector3 origin;
        int prisonI, prisonJ;
        Vector3 prisonOpen = Vector3.forward;

        // ---------- Construcción ----------

        public static DungeonInstance Build(DungeonSite site)
        {
            DungeonInstance d = new DungeonInstance();
            d.Site = site;
            d.origin = site.Origin;
            d.rng = new System.Random(IslandTerrain.Seed * 31 + site.Id * 7919 + 17);
            GameObject root = new GameObject("Dungeon " + site.Id);
            d.Root = root.transform;

            d.Generate();
            d.BuildShell();
            d.BuildExitPad();
            d.BuildPrison();
            d.PlaceChests();
            d.PlaceGoblins();
            d.PlaceTorches();
            d.PlaceLoot();
            return d;
        }

        Vector3 Cell(int i, int j)
        {
            return origin + new Vector3((i - 4) * C, 0f, j * C + C * 0.5f);
        }

        bool OpenS(int i, int j) { return !hw[i, j]; }
        bool OpenN(int i, int j) { return !hw[i, j + 1]; }
        bool OpenW(int i, int j) { return !vw[i, j]; }
        bool OpenE(int i, int j) { return !vw[i + 1, j]; }

        int Openings(int i, int j)
        {
            int n = 0;
            if (OpenS(i, j)) n++;
            if (OpenN(i, j)) n++;
            if (OpenW(i, j)) n++;
            if (OpenE(i, j)) n++;
            return n;
        }

        /// <summary>Dirección (horizontal, unitaria) de la primera salida de la celda.</summary>
        Vector3 OpenDir(int i, int j)
        {
            if (OpenN(i, j)) return Vector3.forward;
            if (OpenE(i, j)) return Vector3.right;
            if (OpenS(i, j)) return Vector3.back;
            return Vector3.left;
        }

        void Carve(Vector2Int a, Vector2Int b)
        {
            if (b.x == a.x + 1) vw[a.x + 1, a.y] = false;
            else if (b.x == a.x - 1) vw[a.x, a.y] = false;
            else if (b.y == a.y + 1) hw[a.x, a.y + 1] = false;
            else if (b.y == a.y - 1) hw[a.x, a.y] = false;
        }

        void OpenRoom(int i0, int j0)
        {
            if (i0 < 0 || j0 < 0 || i0 + 1 >= W || j0 + 1 >= H) return;
            vw[i0 + 1, j0] = false;
            vw[i0 + 1, j0 + 1] = false;
            hw[i0, j0 + 1] = false;
            hw[i0 + 1, j0 + 1] = false;
        }

        void Shuffle<TItem>(List<TItem> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int k = rng.Next(i + 1);
                TItem tmp = list[i]; list[i] = list[k]; list[k] = tmp;
            }
        }

        void Generate()
        {
            for (int i = 0; i < W; i++) for (int j = 0; j <= H; j++) hw[i, j] = true;
            for (int i = 0; i <= W; i++) for (int j = 0; j < H; j++) vw[i, j] = true;

            // Laberinto perfecto por búsqueda en profundidad desde la celda de entrada.
            Vector2Int[] dirs = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
            bool[,] seen = new bool[W, H];
            Stack<Vector2Int> stack = new Stack<Vector2Int>();
            Vector2Int start = new Vector2Int(4, 0);
            seen[start.x, start.y] = true;
            stack.Push(start);
            while (stack.Count > 0)
            {
                Vector2Int cur = stack.Peek();
                List<Vector2Int> opts = new List<Vector2Int>();
                for (int k = 0; k < 4; k++)
                {
                    Vector2Int n = new Vector2Int(cur.x + dirs[k].x, cur.y + dirs[k].y);
                    if (n.x < 0 || n.y < 0 || n.x >= W || n.y >= H || seen[n.x, n.y]) continue;
                    opts.Add(n);
                }
                if (opts.Count == 0) { stack.Pop(); continue; }
                Vector2Int nx = opts[rng.Next(opts.Count)];
                Carve(cur, nx);
                seen[nx.x, nx.y] = true;
                stack.Push(nx);
            }

            // Algunos atajos para que haya vueltas y no solo un camino.
            for (int i = 1; i < W; i++)
                for (int j = 0; j < H; j++)
                    if (vw[i, j] && rng.NextDouble() < 0.07) vw[i, j] = false;
            for (int i = 0; i < W; i++)
                for (int j = 1; j < H; j++)
                    if (hw[i, j] && rng.NextDouble() < 0.07) hw[i, j] = false;

            // Dos salas abiertas de 2x2 para los combates más grandes.
            OpenRoom(1 + rng.Next(W - 3), 3 + rng.Next(H - 5));
            OpenRoom(1 + rng.Next(W - 3), 3 + rng.Next(H - 5));

            // Distancias desde la entrada (por los pasillos abiertos).
            for (int i = 0; i < W; i++) for (int j = 0; j < H; j++) dist[i, j] = -1;
            Queue<Vector2Int> q = new Queue<Vector2Int>();
            dist[start.x, start.y] = 0;
            q.Enqueue(start);
            while (q.Count > 0)
            {
                Vector2Int c = q.Dequeue();
                int d = dist[c.x, c.y] + 1;
                if (OpenN(c.x, c.y) && dist[c.x, c.y + 1] < 0) { dist[c.x, c.y + 1] = d; q.Enqueue(new Vector2Int(c.x, c.y + 1)); }
                if (OpenS(c.x, c.y) && dist[c.x, c.y - 1] < 0) { dist[c.x, c.y - 1] = d; q.Enqueue(new Vector2Int(c.x, c.y - 1)); }
                if (OpenE(c.x, c.y) && dist[c.x + 1, c.y] < 0) { dist[c.x + 1, c.y] = d; q.Enqueue(new Vector2Int(c.x + 1, c.y)); }
                if (OpenW(c.x, c.y) && dist[c.x - 1, c.y] < 0) { dist[c.x - 1, c.y] = d; q.Enqueue(new Vector2Int(c.x - 1, c.y)); }
            }

            // La prisión: el callejón sin salida más lejano.
            int best = -1;
            prisonI = 4; prisonJ = H - 1;
            for (int i = 0; i < W; i++)
                for (int j = 0; j < H; j++)
                {
                    if (i == 4 && j == 0) continue;
                    bool dead = Openings(i, j) == 1;
                    int score = dist[i, j] + (dead ? 1000 : 0);
                    if (dist[i, j] >= 0 && score > best) { best = score; prisonI = i; prisonJ = j; }
                }
            prisonOpen = OpenDir(prisonI, prisonJ);
        }

        void BuildShell()
        {
            Color stone = new Color(0.5f, 0.5f, 0.52f);
            float cx = origin.x, cz = origin.z + H * C * 0.5f;
            GameObject floor = Prim.Box(Root, new Vector3(cx, -0.5f, cz), new Vector3(W * C + 4f, 1f, H * C + 4f), stone, true, "Floor");
            WorldLook.Surface(floor, "Rock", 3f);

            // Muros horizontales (a lo largo de X), fusionando tramos contiguos en una sola pieza.
            for (int j = 0; j <= H; j++)
            {
                int i = 0;
                while (i < W)
                {
                    if (!hw[i, j]) { i++; continue; }
                    int i0 = i;
                    while (i < W && hw[i, j]) i++;
                    float x0 = origin.x + (i0 - 4.5f) * C - T * 0.5f;
                    float x1 = origin.x + (i - 4.5f) * C + T * 0.5f;
                    GameObject w = Prim.Box(Root, new Vector3((x0 + x1) * 0.5f, WallH * 0.5f, origin.z + j * C), new Vector3(x1 - x0, WallH, T), stone, true, "Wall");
                    WorldLook.Surface(w, "Masonry", 2f);
                }
            }
            // Muros verticales (a lo largo de Z), un poco más altos para no parpadear al solaparse con los horizontales.
            for (int i = 0; i <= W; i++)
            {
                int j = 0;
                while (j < H)
                {
                    if (!vw[i, j]) { j++; continue; }
                    int j0 = j;
                    while (j < H && vw[i, j]) j++;
                    float z0 = origin.z + j0 * C - T * 0.5f;
                    float z1 = origin.z + j * C + T * 0.5f;
                    GameObject w = Prim.Box(Root, new Vector3(origin.x + (i - 4.5f) * C, (WallH + 0.02f) * 0.5f, (z0 + z1) * 0.5f), new Vector3(T, WallH + 0.02f, z1 - z0), stone, true, "Wall");
                    WorldLook.Surface(w, "Masonry", 2f);
                }
            }
        }

        void BuildExitPad()
        {
            Vector3 c = Cell(4, 0);
            PadPos = c + new Vector3(0f, 0f, -1.5f);
            Spawn = c + new Vector3(0f, 0.15f, 1.2f);
            PortalBeam.Create(Root, PadPos, 0.9f, 3.4f, false);
            Zone.Create(Root, PadPos, 1.4f, TryExit, false);
            float zWall = c.z - (C * 0.5f - T * 0.5f);
            Torch.Create(Root, new Vector3(c.x - 1.9f, 2.1f, zWall), Vector3.forward, true);
            Torch.Create(Root, new Vector3(c.x + 1.9f, 2.1f, zWall), Vector3.forward, true);
        }

        void TryExit()
        {
            if (Freed && !Collected && villagerGo != null && Game.Player != null)
            {
                float d = Vector3.Distance(Nav.Flat(villagerGo.transform.position), Nav.Flat(Game.Player.transform.position));
                if (d > 9f)
                {
                    if (Time.time > warnAt) { Game.Say("Espera al aldeano: aún viene detrás de ti", 2.5f); warnAt = Time.time + 3f; }
                    return;
                }
            }
            Dungeon.Exit();
        }

        // ---------- Prisión ----------

        void BuildPrison()
        {
            Vector3 c = Cell(prisonI, prisonJ);
            PrisonPos = c;
            cage = new GameObject("Cage");
            cage.transform.SetParent(Root, false);
            cage.transform.position = c;
            Transform ct = cage.transform;

            Color iron = new Color(0.26f, 0.26f, 0.31f);
            float half = 1.2f;
            float[] ps = { -1.2f, -0.6f, 0f, 0.6f, 1.2f };
            Vector3[] sideDirs = { Vector3.back, Vector3.forward, Vector3.left, Vector3.right };
            for (int side = 0; side < 4; side++)
            {
                for (int k = 0; k < 5; k++)
                {
                    if (side >= 2 && (k == 0 || k == 4)) continue;   // las esquinas ya las puso el lado norte/sur
                    Vector3 lp;
                    if (side == 0) lp = new Vector3(ps[k], 1.3f, -half);
                    else if (side == 1) lp = new Vector3(ps[k], 1.3f, half);
                    else if (side == 2) lp = new Vector3(-half, 1.3f, ps[k]);
                    else lp = new Vector3(half, 1.3f, ps[k]);
                    bool door = Vector3.Dot(sideDirs[side], prisonOpen) > 0.5f && k >= 1 && k <= 3;
                    GameObject bar = Prim.Cyl(ct, lp, new Vector3(0.07f, 1.3f, 0.07f), iron, true, door ? "DoorBar" : "Bar");
                    if (door) doorBars.Add(bar);
                }
            }
            for (int s = 0; s < 2; s++)
            {
                float y = s == 0 ? 0.05f : 2.6f;
                float sg = s == 0 ? -1f : 1f;
                Prim.Box(ct, new Vector3(0f, y, -half), new Vector3(2.5f, 0.1f, 0.1f), iron, false, "FrameS");
                Prim.Box(ct, new Vector3(0f, y, half), new Vector3(2.5f, 0.1f, 0.1f), iron, false, "FrameN");
                Prim.Box(ct, new Vector3(-half, y, 0f), new Vector3(0.1f, 0.1f, 2.5f), iron, false, "FrameW");
                Prim.Box(ct, new Vector3(half, y, 0f), new Vector3(0.1f, 0.1f, 2.5f), iron, false, "FrameE");
                if (sg > 0f) Prim.Box(ct, new Vector3(0f, 2.66f, 0f), new Vector3(2.5f, 0.06f, 2.5f), iron * 0.8f, false, "Lid");
            }

            Interactable it = cage.AddComponent<Interactable>();
            it.Prompt = "Abrir la jaula";
            it.Range = 3.6f;
            it.OnUse = Free;

            float yaw = Mathf.Atan2(prisonOpen.x, prisonOpen.z) * Mathf.Rad2Deg;
            villagerGo = Npc.Create(Root, c + new Vector3(0f, 0.02f, 0f), "Aldeano prisionero", new Color(0.35f, 0.55f, 0.75f), new Color(0.35f, 0.22f, 0.12f), true, yaw);
            villager = DungeonVillager.Attach(villagerGo);

            // Antorchas junto a la jaula.
            Vector3 perp = new Vector3(prisonOpen.z, 0f, -prisonOpen.x);
            TorchOnWall(c, -prisonOpen, perp * 1.6f);
            TorchOnWall(c, -prisonOpen, -perp * 1.6f);
        }

        void Free()
        {
            if (Freed) return;
            Freed = true;
            for (int i = 0; i < doorBars.Count; i++) if (doorBars[i] != null) Object.Destroy(doorBars[i]);
            doorBars.Clear();
            Interactable it = cage != null ? cage.GetComponent<Interactable>() : null;
            if (it != null) it.Enabled = false;
            Sfx.Play("door", 1.1f, 1f);
            LightBurst.Spawn(PrisonPos + Vector3.up * 1.4f, 0.5f, false);
            if (villager != null) villager.Following = true;
            Game.Dialog("Aldeano", "¡Por fin! Los goblins me encerraron aquí. Sácame de este lugar: te sigo. La salida es la luz del principio.", 7f);
        }

        /// <summary>Recoge al aldeano al salir. Devuelve true si estaba liberado y lo traes contigo.</summary>
        public bool CollectVillager()
        {
            if (!Freed || Collected || villagerGo == null) return false;
            Collected = true;
            Object.Destroy(villagerGo);
            return true;
        }

        // ---------- Contenido ----------

        List<Vector2Int> DeadEnds()
        {
            List<Vector2Int> list = new List<Vector2Int>();
            for (int i = 0; i < W; i++)
                for (int j = 0; j < H; j++)
                {
                    if (i == 4 && j == 0) continue;
                    if (i == prisonI && j == prisonJ) continue;
                    if (dist[i, j] >= 0 && Openings(i, j) == 1) list.Add(new Vector2Int(i, j));
                }
            return list;
        }

        void PlaceChests()
        {
            // El callejón más lejano guarda el tesoro principal; el resto se reparte al azar.
            List<Vector2Int> all = DeadEnds();
            all.Sort((a, b) => dist[b.x, b.y].CompareTo(dist[a.x, a.y]));
            List<Vector2Int> chosen = new List<Vector2Int>();
            if (all.Count > 0) chosen.Add(all[0]);
            List<Vector2Int> others = all.Count > 1 ? all.GetRange(1, all.Count - 1) : new List<Vector2Int>();
            Shuffle(others);
            for (int i = 0; i < others.Count && chosen.Count < 4; i++) chosen.Add(others[i]);

            // Si faltan callejones, se completa con celdas lejanas cualquiera.
            int guard = 0;
            while (chosen.Count < 3 && guard++ < 200)
            {
                int i = rng.Next(W), j = 2 + rng.Next(H - 2);
                Vector2Int v = new Vector2Int(i, j);
                if (dist[i, j] < 4 || (i == prisonI && j == prisonJ) || chosen.Contains(v)) continue;
                chosen.Add(v);
            }

            for (int n = 0; n < chosen.Count; n++)
            {
                Vector2Int v = chosen[n];
                Vector3 c = Cell(v.x, v.y);
                Vector3 open = Openings(v.x, v.y) == 1 ? OpenDir(v.x, v.y) : Vector3.forward;
                Vector3 pos = c - open * 1.7f;
                float yaw = Mathf.Atan2(open.x, open.z) * Mathf.Rad2Deg;
                Chest chest = Chest.Create(Root, pos, yaw);
                bool grand = n == 0;
                chest.OnOpened = () => { ChestsOpened++; ChestReward(grand); };
                ChestsTotal++;
                TorchOnWall(c, -open, Vector3.zero);
            }
        }

        void ChestReward(bool grand)
        {
            Sfx.Play("pickup");
            Health h = Game.Player != null ? Game.Player.GetComponent<Health>() : null;
            if (grand)
            {
                Game.Gems += 40;
                Game.HasBombs = true;
                Game.Bombs = Mathf.Min(20, Game.Bombs + 6);
                Game.Arrows = Mathf.Min(30, Game.Arrows + 12);
                Game.Mana = Game.MaxMana;
                if (h != null) h.Heal(99f);
                Game.Say("¡Tesoro de la mazmorra! +40 gemas, bombas, flechas y vida restaurada", 5f);
                return;
            }
            Game.Gems += 12 + rng.Next(10);
            int r = rng.Next(4);
            if (r == 0) { Game.HasBombs = true; Game.Bombs = Mathf.Min(20, Game.Bombs + 4); Game.Say("Cofre: gemas y +4 bombas", 3f); }
            else if (r == 1) { Game.Arrows = Mathf.Min(30, Game.Arrows + 10); Game.Say("Cofre: gemas y +10 flechas", 3f); }
            else if (r == 2) { if (h != null) h.Heal(99f); Game.Say("Cofre: gemas y corazones restaurados", 3f); }
            else { Game.Mana = Game.MaxMana; Game.Say("Cofre: gemas y maná al máximo", 3f); }
        }

        void PlaceGoblins()
        {
            Vector3 pc = Cell(prisonI, prisonJ);
            // Guardias de la prisión: dos delante de la jaula y uno detrás.
            Vector3 perp = new Vector3(prisonOpen.z, 0f, -prisonOpen.x);
            int ni = prisonI + Mathf.RoundToInt(prisonOpen.x), nj = prisonJ + Mathf.RoundToInt(prisonOpen.z);
            if (ni >= 0 && nj >= 0 && ni < W && nj < H)
            {
                Vector3 nc = Cell(ni, nj);
                AddGoblin(nc + perp * 1.3f);
                AddGoblin(nc - perp * 1.3f);
            }
            AddGoblin(pc - prisonOpen * 2.0f);

            List<Vector2Int> cells = new List<Vector2Int>();
            for (int i = 0; i < W; i++)
                for (int j = 0; j < H; j++)
                {
                    if (dist[i, j] < 3) continue;
                    if (i == prisonI && j == prisonJ) continue;
                    if (ni == i && nj == j) continue;
                    cells.Add(new Vector2Int(i, j));
                }
            Shuffle(cells);
            int count = Mathf.Min(cells.Count, 11);
            for (int n = 0; n < count; n++)
            {
                Vector2Int v = cells[n];
                Vector3 c = Cell(v.x, v.y);
                AddGoblin(c + new Vector3((float)(rng.NextDouble() - 0.5) * 2.4f, 0f, (float)(rng.NextDouble() - 0.5) * 2.4f));
                if (dist[v.x, v.y] >= 8 && rng.NextDouble() < 0.45)
                    AddGoblin(c + new Vector3((float)(rng.NextDouble() - 0.5) * 3.2f, 0f, (float)(rng.NextDouble() - 0.5) * 3.2f));
            }
        }

        void AddGoblin(Vector3 pos)
        {
            EnemySlime g = EnemySlime.SpawnGoblin(pos + Vector3.up * 0.1f, Root);
            g.transform.rotation = Quaternion.Euler(0f, rng.Next(360), 0f);
            goblins.Add(g);
        }

        public int AliveGoblins()
        {
            int n = 0;
            for (int i = 0; i < goblins.Count; i++) if (goblins[i] != null && !goblins[i].Hp.Dead) n++;
            return n;
        }

        /// <summary>Antorcha en la pared de la celda indicada (wallDir apunta hacia la pared).</summary>
        void TorchOnWall(Vector3 cellCenter, Vector3 wallDir, Vector3 lateral)
        {
            Vector3 p = cellCenter + lateral + wallDir * (C * 0.5f - T * 0.5f);
            p.y = 2.1f;
            Torch.Create(Root, p, -wallDir, true);
        }

        void PlaceTorches()
        {
            for (int i = 0; i < W; i++)
                for (int j = 0; j < H; j++)
                {
                    if (i == 4 && j == 0) continue;
                    if (i == prisonI && j == prisonJ) continue;
                    if (rng.NextDouble() > 0.34) continue;
                    List<Vector3> walls = new List<Vector3>();
                    if (hw[i, j]) walls.Add(Vector3.back);
                    if (hw[i, j + 1]) walls.Add(Vector3.forward);
                    if (vw[i, j]) walls.Add(Vector3.left);
                    if (vw[i + 1, j]) walls.Add(Vector3.right);
                    if (walls.Count == 0) continue;
                    TorchOnWall(Cell(i, j), walls[rng.Next(walls.Count)], Vector3.zero);
                }
        }

        void PlaceLoot()
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            for (int i = 0; i < W; i++)
                for (int j = 0; j < H; j++)
                    if (dist[i, j] >= 2 && !(i == prisonI && j == prisonJ)) cells.Add(new Vector2Int(i, j));
            Shuffle(cells);
            for (int n = 0; n < cells.Count && n < 12; n++)
            {
                Vector3 c = Cell(cells[n].x, cells[n].y) + new Vector3((float)(rng.NextDouble() - 0.5) * 2f, 0.5f, (float)(rng.NextDouble() - 0.5) * 2f);
                Pickup p;
                if (n < 7) p = Pickup.Spawn(c, PickupKind.Gem, 1 + rng.Next(2));
                else if (n < 10) p = Pickup.Spawn(c, PickupKind.Heart);
                else p = Pickup.Spawn(c, PickupKind.Arrows, 4);
                if (p != null) p.transform.SetParent(Root, true);
            }
        }

        // ---------- Estado ----------

        public void OnEntered()
        {
            if (greeted) return;
            greeted = true;
            Game.Dialog("Pareja", "Qué lugar más sombrío... Hay goblins por todas partes. Sigue las antorchas y busca al aldeano prisionero.", 7f);
        }

        public string ObjectiveText
        {
            get
            {
                if (Collected) return Dungeon.Name + " · aldeano a salvo";
                if (Freed) return Dungeon.Name + " · escolta al aldeano hasta la luz de la salida";
                return Dungeon.Name + " · rescata al aldeano · goblins: " + AliveGoblins() + " · cofres: " + ChestsOpened + "/" + ChestsTotal;
            }
        }
    }
}
