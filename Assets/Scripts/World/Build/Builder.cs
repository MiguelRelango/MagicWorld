using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Hearthbound
{
    public enum PieceKind { Floor, Wall, Door, Roof, Campfire }

    /// <summary>Pieza construida. Guarda su clave en la cuadrícula para evitar duplicados y poder quitarla.</summary>
    public class BuildPiece : MonoBehaviour
    {
        public PieceKind Kind;
        public string Key;
        public int Gx, Gz, Edge, Level;
        public float Yaw;
    }

    /// <summary>Hoguera con luz parpadeante.</summary>
    public class Campfire : MonoBehaviour
    {
        Light lamp;
        Transform flame;

        void Start()
        {
            GameObject lg = new GameObject("FireLight");
            lg.transform.SetParent(transform, false);
            lg.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            lamp = lg.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = new Color(1f, 0.62f, 0.28f);
            lamp.range = 9f;
            lamp.intensity = 1.8f;
            lamp.shadows = LightShadows.None;
            flame = transform.Find("Flame");
        }

        void Update()
        {
            float f = 0.85f + Mathf.Sin(Time.time * 13f) * 0.12f + Mathf.Sin(Time.time * 29f) * 0.06f;
            if (lamp != null) lamp.intensity = 1.8f * f;
            if (flame != null) flame.localScale = new Vector3(0.38f, 0.6f * f, 0.38f);
        }
    }

    /// <summary>
    /// Sistema de construcción (tecla X): suelos, paredes, puertas, techos y fogatas sobre una cuadrícula de 2 m.
    /// El puntero del ratón elige dónde, la rueda cambia el nivel (altura), 1-5 eligen la pieza,
    /// clic izquierdo coloca (gasta madera) y clic derecho quita (devuelve la madera).
    /// Las construcciones se guardan en disco por mundo (isla = semilla).
    /// </summary>
    public class Builder : MonoBehaviour
    {
        public static Builder Instance;

        public const float Cell = 2f;
        public const float WallH = 2.4f;
        public const int MaxLevel = 3;
        const float Reach = 11f;

        public static readonly string[] Names = { "Suelo", "Pared", "Puerta", "Techo", "Fogata" };
        public static readonly int[] Cost = { 2, 2, 3, 3, 4 };

        public int Sel;                 // pieza elegida (0..4)
        public int Level;               // nivel de altura
        public string Hint = "";        // mensaje de estado para el HUD
        public bool HintOk = true;

        Transform root;
        GameObject ghost;
        int ghostSel = -1;
        bool ghostOk;
        string worldId = "";
        bool dirty;
        float nextSave;
        readonly Dictionary<string, BuildPiece> pieces = new Dictionary<string, BuildPiece>();

        static readonly Color Plank = new Color(0.68f, 0.48f, 0.28f);
        static readonly Color PlankDark = new Color(0.52f, 0.36f, 0.21f);
        static readonly Color RoofCol = new Color(0.66f, 0.32f, 0.26f);

        class Plan
        {
            public bool Ok;
            public string Why = "";
            public string Key;
            public PieceKind Kind;
            public Vector3 Pos;
            public float Yaw;
            public int Gx, Gz, Edge, Level;
        }

        void Awake() { Instance = this; }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnApplicationQuit() { Save(); }

        void OnApplicationPause(bool paused) { if (paused) Save(); }

        // ---------- Mundo ----------

        Transform Root()
        {
            if (root == null) root = new GameObject("Construcciones").transform;
            return root;
        }

        /// <summary>Empieza un mundo (carga las construcciones guardadas de ese mundo).</summary>
        public void BeginWorld(string id, bool fresh = false)
        {
            EndWorld();
            worldId = id;
            Level = 0;
            if (fresh)
            {
                try { if (File.Exists(PathFor())) File.Delete(PathFor()); } catch (System.Exception) { }
            }
            else Load();
        }

        /// <summary>Guarda y retira todas las construcciones (al salir de la partida).</summary>
        public void EndWorld()
        {
            if (worldId.Length > 0) Save();
            worldId = "";
            Game.Building = false;
            HideGhost();
            pieces.Clear();
            if (root != null) Destroy(root.gameObject);
            root = null;
        }

        public int Count { get { return pieces.Count; } }

        // ---------- Bucle ----------

        void Update()
        {
            if (Game.State == GameState.Menu || Game.State == GameState.Ended)
            {
                if (Game.Building) Game.Building = false;
                HideGhost();
                return;
            }
            if (Game.State != GameState.Playing || Game.Player == null) { HideGhost(); return; }

            if (CraftingSystem.IsWorking) { HideGhost(); return; }

            if (dirty && Time.unscaledTime >= nextSave) { Save(); nextSave = Time.unscaledTime + 20f; }

            if (GameInput.Pressed(Act.Build) && worldId.Length > 0)
            {
                Game.Building = !Game.Building;
                Game.Say(Game.Building ? "Modo construcción" : "Modo construcción desactivado", 1.8f);
                Sfx.Play("ui", Game.Building ? 1.2f : 0.9f, 0.6f);
                if (Game.Building) Game.Player.ClearLock();
            }
            if (!Game.Building) { HideGhost(); return; }

            // Elegir pieza (1-5) y nivel (rueda).
            for (int i = 0; i < 5; i++)
                if (GameInput.RawPressed(KeyCode.Alpha1 + i)) { Sel = i; Sfx.Play("ui", 1.4f, 0.4f); }
            float scroll = GameInput.Scroll();
            if (Mathf.Abs(scroll) > 0.01f) Level = Mathf.Clamp(Level + (scroll > 0f ? 1 : -1), 0, MaxLevel);

            Plan plan = Compute();
            UpdateGhost(plan);
            Hint = plan.Ok ? "" : plan.Why;
            HintOk = plan.Ok;

            if (GameInput.Pressed(Act.Attack)) TryPlace(plan);
            if (GameInput.Pressed(Act.Block) || GameInput.RawPressed(KeyCode.Delete) || GameInput.RawPressed(KeyCode.Backspace)) TryRemove();
        }

        // ---------- Cálculo de la colocación ----------

        static bool IsIgnored(Collider c)
        {
            return c.GetComponentInParent<PlayerController>() != null
                || c.GetComponentInParent<Partner>() != null
                || c.GetComponentInParent<EnemySlime>() != null
                || c.GetComponentInParent<Pickup>() != null;
        }

        static RaycastHit[] Sorted(Ray ray, float dist)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, dist, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            return hits;
        }

        bool PointerHit(out RaycastHit hit)
        {
            hit = default(RaycastHit);
            Camera cam = Camera.main;
            if (cam == null) return false;
            Ray ray = cam.ScreenPointToRay(GameInput.PointerPos());
            RaycastHit[] hits = Sorted(ray, 80f);
            for (int i = 0; i < hits.Length; i++)
            {
                if (IsIgnored(hits[i].collider)) continue;
                if (ghost != null && hits[i].collider.transform.IsChildOf(ghost.transform)) continue;
                hit = hits[i];
                return true;
            }
            return false;
        }

        /// <summary>Suelo bajo un punto, ignorando construcciones y árboles. Devuelve false si no es tierra construible.</summary>
        bool GroundAt(float x, float z, float refY, out float y)
        {
            y = 0f;
            Ray ray = new Ray(new Vector3(x, refY + 5f, z), Vector3.down);
            RaycastHit[] hits = Sorted(ray, 60f);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider c = hits[i].collider;
                if (IsIgnored(c) || c.GetComponentInParent<BuildPiece>() != null || c.GetComponentInParent<TreeNode>() != null) continue;
                string n = c.name;
                if (n != "Ground" && !n.StartsWith("Chunk")) return false;
                if (IslandMap.Active && hits[i].point.y < 0.5f) return false;   // agua
                y = hits[i].point.y;
                return true;
            }
            return false;
        }

        Plan Compute()
        {
            Plan p = new Plan();
            p.Kind = (PieceKind)Sel;
            p.Level = Level;
            RaycastHit hit;
            if (!PointerHit(out hit)) { p.Why = "Apunta al suelo con el ratón"; return p; }

            Vector3 pt = hit.point;
            int gx = Mathf.RoundToInt(pt.x / Cell), gz = Mathf.RoundToInt(pt.z / Cell);
            float cx = gx * Cell, cz = gz * Cell;
            p.Gx = gx; p.Gz = gz;

            float g;
            if (!GroundAt(cx, cz, pt.y, out g)) { p.Why = "Solo se construye en tierra firme"; return p; }
            g = Mathf.Round(g * 2f) * 0.5f;

            float baseY = g + p.Level * WallH;
            switch (p.Kind)
            {
                case PieceKind.Floor:
                case PieceKind.Roof:
                {
                    // Un techo del nivel L es la losa del nivel L+1: comparten hueco en la cuadrícula.
                    int slab = p.Kind == PieceKind.Roof ? p.Level + 1 : p.Level;
                    p.Key = "S" + slab + ":" + gx + "," + gz;
                    p.Pos = new Vector3(cx, g + slab * WallH, cz);
                    p.Yaw = 0f;
                    break;
                }
                case PieceKind.Wall:
                case PieceKind.Door:
                {
                    float dx = pt.x - cx, dz = pt.z - cz;
                    int wx2 = 2 * gx, wz2 = 2 * gz;
                    if (Mathf.Abs(dx) > Mathf.Abs(dz)) { wx2 += dx >= 0f ? 1 : -1; p.Edge = 2; p.Yaw = 90f; }   // a lo largo de Z
                    else { wz2 += dz >= 0f ? 1 : -1; p.Edge = 1; p.Yaw = 0f; }                                   // a lo largo de X
                    p.Key = "W" + p.Level + ":" + wx2 + "," + wz2;
                    p.Pos = new Vector3(wx2 * Cell * 0.5f, baseY, wz2 * Cell * 0.5f);
                    break;
                }
                default:   // fogata
                    p.Key = "C:" + gx + "," + gz;
                    p.Pos = new Vector3(cx, g, cz);
                    break;
            }

            // Validaciones
            Vector3 pl = Game.Player.transform.position;
            if (Vector3.Distance(Nav.Flat(pl), Nav.Flat(p.Pos)) > Reach) { p.Why = "Demasiado lejos"; return p; }
            if (Game.Wood < Cost[Sel]) { p.Why = "Necesitas " + Cost[Sel] + " de madera (tienes " + Game.Wood + ")"; return p; }
            if (pieces.ContainsKey(p.Key)) { p.Why = "Ya hay una pieza aquí"; return p; }
            if (p.Kind != PieceKind.Floor && p.Kind != PieceKind.Roof)
            {
                // No encerrar al jugador ni a la pareja dentro de una pared.
                if (Near(pl, p, 0.75f) || (Game.Partner != null && Near(Game.Partner.transform.position, p, 0.7f))) { p.Why = "Hay alguien en el sitio"; return p; }
            }
            Collider[] near = Physics.OverlapSphere(p.Pos + Vector3.up * 1.1f, 1.0f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < near.Length; i++)
                if (near[i].GetComponentInParent<TreeNode>() != null) { p.Why = "Hay un árbol en el sitio"; return p; }
            for (int i = 0; i < TreeNode.All.Count; i++)
                if (Vector3.Distance(Nav.Flat(TreeNode.All[i].transform.position), Nav.Flat(p.Pos)) < 1.1f) { p.Why = "Hay un brote de árbol en el sitio"; return p; }

            p.Ok = true;
            return p;
        }

        static bool Near(Vector3 who, Plan p, float r)
        {
            if (who.y > p.Pos.y + WallH + 0.5f || who.y < p.Pos.y - 1.5f) return false;
            if (p.Kind == PieceKind.Campfire) return Vector3.Distance(Nav.Flat(who), Nav.Flat(p.Pos)) < r + 0.3f;
            // Distancia al segmento de la pared (largo 2 m).
            Vector3 a = p.Edge == 1 ? Vector3.right : Vector3.forward;
            Vector3 d = Nav.Flat(who - p.Pos);
            float along = Mathf.Clamp(Vector3.Dot(d, a), -Cell * 0.5f, Cell * 0.5f);
            return (d - a * along).magnitude < r;
        }

        // ---------- Fantasma (vista previa) ----------

        void HideGhost()
        {
            if (ghost != null) ghost.SetActive(false);
        }

        void UpdateGhost(Plan p)
        {
            if (ghost == null || ghostSel != Sel)
            {
                if (ghost != null) Destroy(ghost);
                ghost = Geometry(p.Kind, true);
                ghostSel = Sel;
                ghostOk = !p.Ok;   // fuerza actualizar color
            }
            if (!p.Ok && p.Pos == Vector3.zero) { ghost.SetActive(false); return; }
            ghost.SetActive(true);
            ghost.transform.position = p.Pos;
            ghost.transform.rotation = Quaternion.Euler(0f, p.Yaw, 0f);
            if (p.Ok != ghostOk)
            {
                ghostOk = p.Ok;
                Material m = Prim.Mat(p.Ok ? new Color(0.40f, 1f, 0.55f) : new Color(1f, 0.35f, 0.35f), true);
                Renderer[] rs = ghost.GetComponentsInChildren<Renderer>();
                for (int i = 0; i < rs.Length; i++) rs[i].sharedMaterial = m;
            }
        }

        // ---------- Colocar y quitar ----------

        void TryPlace(Plan p)
        {
            if (!p.Ok)
            {
                if (p.Why.Length > 0) Game.Say(p.Why, 1.5f);
                Sfx.Play("ui", 0.6f, 0.5f);
                return;
            }
            Game.Wood -= Cost[Sel];
            Spawn(p.Kind, p.Key, p.Pos, p.Yaw, p.Gx, p.Gz, p.Edge, p.Level);
            Sfx.Play("build", 0.9f + Random.value * 0.2f);
            dirty = true;
        }

        BuildPiece Spawn(PieceKind kind, string key, Vector3 pos, float yaw, int gx, int gz, int edge, int level)
        {
            GameObject g = Geometry(kind, false);
            g.transform.SetParent(Root(), false);
            g.transform.position = pos;
            g.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            BuildPiece bp = g.AddComponent<BuildPiece>();
            bp.Kind = kind; bp.Key = key; bp.Gx = gx; bp.Gz = gz; bp.Edge = edge; bp.Level = level; bp.Yaw = yaw;
            if (kind == PieceKind.Campfire) g.AddComponent<Campfire>();
            pieces[key] = bp;
            return bp;
        }

        void TryRemove()
        {
            RaycastHit hit;
            if (!PointerHit(out hit)) return;
            BuildPiece bp = hit.collider.GetComponentInParent<BuildPiece>();
            if (bp == null) return;
            if (Vector3.Distance(Nav.Flat(Game.Player.transform.position), Nav.Flat(bp.transform.position)) > Reach) { Game.Say("Demasiado lejos", 1.5f); return; }
            Game.Wood += Cost[(int)bp.Kind];
            pieces.Remove(bp.Key);
            Destroy(bp.gameObject);
            Sfx.Play("chop", 1.3f, 0.8f);
            Game.Say("Pieza retirada: +" + Cost[(int)bp.Kind] + " madera", 1.5f);
            dirty = true;
        }

        // ---------- Geometría ----------

        static GameObject Geometry(PieceKind kind, bool ghost)
        {
            GameObject g = new GameObject(ghost ? "Fantasma" : "Pieza_" + kind);
            Transform t = g.transform;
            bool col = !ghost;
            switch (kind)
            {
                case PieceKind.Floor:
                    Prim.Box(t, new Vector3(0f, 0.1f, 0f), new Vector3(Cell, 0.2f, Cell), Plank, col, "Slab");
                    if (!ghost)
                        for (int i = 0; i < 3; i++)
                            Prim.Box(t, new Vector3(0f, 0.205f, -0.67f + i * 0.67f), new Vector3(Cell - 0.04f, 0.012f, 0.03f), PlankDark, false, "Seam");
                    break;
                case PieceKind.Roof:
                    Prim.Box(t, new Vector3(0f, 0.1f, 0f), new Vector3(Cell + 0.2f, 0.2f, Cell + 0.2f), RoofCol, col, "Slab");
                    if (!ghost)
                        Prim.Box(t, new Vector3(0f, 0.22f, 0f), new Vector3(Cell, 0.06f, Cell), RoofCol * 0.85f, false, "Tiles");
                    break;
                case PieceKind.Wall:
                    Prim.Box(t, new Vector3(0f, (WallH - 1f) * 0.5f, 0f), new Vector3(Cell, WallH + 1f, 0.2f), Plank, col, "Wall");
                    if (!ghost)
                    {
                        Prim.Box(t, new Vector3(0f, WallH - 0.08f, 0f), new Vector3(Cell + 0.04f, 0.16f, 0.26f), PlankDark, false, "Beam");
                        Prim.Box(t, new Vector3(-0.98f, (WallH - 1f) * 0.5f, 0f), new Vector3(0.08f, WallH + 1f, 0.26f), PlankDark, false, "PostL");
                        Prim.Box(t, new Vector3(0.98f, (WallH - 1f) * 0.5f, 0f), new Vector3(0.08f, WallH + 1f, 0.26f), PlankDark, false, "PostR");
                    }
                    break;
                case PieceKind.Door:
                    // Marco con hueco de 1,1 m de ancho y 1,9 m de alto.
                    Prim.Box(t, new Vector3(-0.775f, (WallH - 1f) * 0.5f, 0f), new Vector3(0.45f, WallH + 1f, 0.2f), Plank, col, "SideL");
                    Prim.Box(t, new Vector3(0.775f, (WallH - 1f) * 0.5f, 0f), new Vector3(0.45f, WallH + 1f, 0.2f), Plank, col, "SideR");
                    Prim.Box(t, new Vector3(0f, (WallH + 1.9f) * 0.5f, 0f), new Vector3(1.1f, WallH - 1.9f, 0.2f), Plank, col, "Lintel");
                    if (!ghost)
                    {
                        Prim.Box(t, new Vector3(0f, WallH - 0.08f, 0f), new Vector3(Cell + 0.04f, 0.16f, 0.26f), PlankDark, false, "Beam");
                        Prim.Box(t, new Vector3(-0.58f, 0.95f, 0f), new Vector3(0.07f, 1.9f, 0.26f), PlankDark, false, "FrameL");
                        Prim.Box(t, new Vector3(0.58f, 0.95f, 0f), new Vector3(0.07f, 1.9f, 0.26f), PlankDark, false, "FrameR");
                    }
                    break;
                default: // fogata
                    for (int i = 0; i < 7; i++)
                    {
                        float a = i * Mathf.PI * 2f / 7f;
                        Prim.Box(t, new Vector3(Mathf.Cos(a) * 0.5f, 0.1f, Mathf.Sin(a) * 0.5f), new Vector3(0.22f, 0.2f, 0.22f), new Color(0.55f, 0.54f, 0.52f), false, "Stone");
                    }
                    if (col)
                    {
                        CapsuleCollider cc = g.AddComponent<CapsuleCollider>();
                        cc.center = new Vector3(0f, 0.4f, 0f);
                        cc.radius = 0.55f;
                        cc.height = 0.8f;
                    }
                    Prim.Box(t, new Vector3(0f, 0.16f, 0f), new Vector3(0.7f, 0.1f, 0.1f), new Color(0.45f, 0.30f, 0.18f), false, "Log1").transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
                    Prim.Box(t, new Vector3(0f, 0.16f, 0f), new Vector3(0.7f, 0.1f, 0.1f), new Color(0.40f, 0.27f, 0.16f), false, "Log2").transform.localRotation = Quaternion.Euler(0f, -35f, 0f);
                    if (!ghost) Prim.Make(PrimitiveType.Sphere, t, new Vector3(0f, 0.5f, 0f), new Vector3(0.38f, 0.6f, 0.38f), new Color(1f, 0.55f, 0.15f), false, "Flame", true);
                    break;
            }
            return g;
        }

        // ---------- Guardado ----------

        string PathFor()
        {
            return Path.Combine(Application.persistentDataPath, "hearthbound_build_" + worldId + ".txt");
        }

        public void Save()
        {
            if (worldId.Length == 0) return;
            try
            {
                List<string> lines = new List<string>();
                foreach (KeyValuePair<string, BuildPiece> kv in pieces)
                {
                    BuildPiece b = kv.Value;
                    if (b == null) continue;
                    Vector3 p = b.transform.position;
                    lines.Add(string.Join("|", new[]
                    {
                        ((int)b.Kind).ToString(), b.Key, b.Gx.ToString(), b.Gz.ToString(), b.Edge.ToString(), b.Level.ToString(),
                        p.x.ToString("R", CultureInfo.InvariantCulture), p.y.ToString("R", CultureInfo.InvariantCulture),
                        p.z.ToString("R", CultureInfo.InvariantCulture), b.Yaw.ToString("R", CultureInfo.InvariantCulture)
                    }));
                }
                File.WriteAllLines(PathFor(), lines.ToArray());
                dirty = false;
            }
            catch (System.Exception) { /* sin permisos: las construcciones valen solo para esta sesión */ }
        }

        void Load()
        {
            try
            {
                string path = PathFor();
                if (!File.Exists(path)) return;
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    try
                    {
                    string[] f = lines[i].Split('|');
                    if (f.Length < 10) continue;
                    int kind = int.Parse(f[0], CultureInfo.InvariantCulture);
                    if (kind < 0 || kind > 4) continue;
                    PieceKind k = (PieceKind)kind;
                    Vector3 pos = new Vector3(
                        float.Parse(f[6], CultureInfo.InvariantCulture),
                        float.Parse(f[7], CultureInfo.InvariantCulture),
                        float.Parse(f[8], CultureInfo.InvariantCulture));
                    Spawn(k, f[1], pos, float.Parse(f[9], CultureInfo.InvariantCulture),
                        int.Parse(f[2], CultureInfo.InvariantCulture), int.Parse(f[3], CultureInfo.InvariantCulture),
                        int.Parse(f[4], CultureInfo.InvariantCulture), int.Parse(f[5], CultureInfo.InvariantCulture));
                    }
                    catch (System.Exception) { /* línea dañada: se ignora */ }
                }
            }
            catch (System.Exception) { }
        }
    }
}
