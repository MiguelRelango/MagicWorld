using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hearthbound
{
    /// <summary>Una entrada de mazmorra en la montaña. Los datos son deterministas según la semilla.</summary>
    public class DungeonSite
    {
        public int Id;
        public Vector3 Pos;        // centro de la boca de la cueva, sobre el terreno
        public Vector3 Fwd;        // hacia fuera de la montaña (horizontal, unitario)
        public Vector2Int Chunk;
        public bool Opened;        // el sello de roca ya se rompió
        public bool Rescued;       // el aldeano ya está a salvo fuera
        public Transform Entrance; // raíz visual de la entrada (null si su chunk no está cargado)
        public DungeonInstance Maze;

        /// <summary>Esquina de la mazmorra en el mundo: muy lejos de la isla para que no se mezclen.</summary>
        public Vector3 Origin { get { return new Vector3(3000f + Id * 200f, 0f, 3000f); } }
        public Vector3 Right { get { return new Vector3(Fwd.z, 0f, -Fwd.x); } }
        /// <summary>Suelo de la plataforma de piedra delante de la entrada.</summary>
        public Vector3 Ground { get { return Pos + Fwd * 3.3f + Vector3.up * 0.08f; } }
    }

    /// <summary>
    /// Mazmorras del mundo abierto: una cueva rocosa en la montaña, sellada con roca. Al acercarte se rompe
    /// con un destello y deja una columna de luz; al cruzarla llegas a un laberinto con antorchas, goblins,
    /// tesoros y un aldeano prisionero al que rescatar.
    /// </summary>
    public static class Dungeon
    {
        public const string Name = "Cripta de los Goblins";

        public static readonly List<DungeonSite> Sites = new List<DungeonSite>();
        public static bool Active;
        public static DungeonSite Current;

        static DungeonRunner runner;
        static bool busy;
        static float sealWarnAt;
        static float cooldownUntil;

        // Ambiente guardado al entrar
        static Color savedAmbient, savedFog, savedBg;
        static float savedSun, savedFogStart, savedFogEnd;
        static bool envSaved;

        // ---------- Colocación de entradas ----------

        public static void Init(int seed)
        {
            Clear();
            // Las bocas de las cuevas están en las montañas de lava del mapa (IslandLayout.DungeonSpots).
            for (int k = 0; k < IslandLayout.DungeonSpots.Length; k++)
            {
                float x = IslandLayout.DungeonSpots[k].x, z = IslandLayout.DungeonSpots[k].y;
                float h = IslandTerrain.Height(x, z);
                Vector3 p = new Vector3(x, h, z);

                // Orientación cuesta abajo (hacia fuera de la montaña).
                float gx = IslandTerrain.Height(x + 1.5f, z) - IslandTerrain.Height(x - 1.5f, z);
                float gz = IslandTerrain.Height(x, z + 1.5f) - IslandTerrain.Height(x, z - 1.5f);
                Vector3 fwd = new Vector3(-gx, 0f, -gz);
                if (fwd.sqrMagnitude < 0.0001f) fwd = new Vector3(0f, 0f, -1f);
                fwd.Normalize();

                DungeonSite s = new DungeonSite();
                s.Id = Sites.Count;
                s.Pos = p;
                s.Fwd = fwd;
                s.Chunk = new Vector2Int(Mathf.FloorToInt(x / IslandWorld.ChunkSize), Mathf.FloorToInt(z / IslandWorld.ChunkSize));
                Sites.Add(s);
            }

            GameObject go = new GameObject("DungeonRunner");
            runner = go.AddComponent<DungeonRunner>();
        }

        public static void Clear()
        {
            if (Active) RestoreEnvironment();
            for (int i = 0; i < Sites.Count; i++)
                if (Sites[i].Maze != null && Sites[i].Maze.Root != null) Object.Destroy(Sites[i].Maze.Root.gameObject);
            Sites.Clear();
            Active = false;
            Current = null;
            busy = false;
            cooldownUntil = 0f;
            envSaved = false;
            if (runner != null) Object.Destroy(runner.gameObject);
            runner = null;
        }

        // ---------- Entrada en la montaña ----------

        public static void PopulateChunk(Transform chunk, int cx, int cz)
        {
            for (int i = 0; i < Sites.Count; i++)
            {
                DungeonSite s = Sites[i];
                if (s.Chunk.x == cx && s.Chunk.y == cz) BuildEntrance(s, chunk);
            }
        }

        static readonly Color Rocky = new Color(0.55f, 0.52f, 0.50f);

        static GameObject Stone(Transform parent, Vector3 pos, Vector3 size, string name, bool collider = true, string tex = "Rock", float tile = 2f)
        {
            GameObject g = Prim.Box(parent, pos, size, Rocky, collider, name);
            return WorldLook.Surface(g, tex, tile);
        }

        static void BuildEntrance(DungeonSite s, Transform chunk)
        {
            GameObject rootGo = new GameObject("DungeonEntrance " + s.Id);
            rootGo.transform.SetParent(chunk, false);
            rootGo.transform.position = s.Pos;
            rootGo.transform.rotation = Quaternion.LookRotation(s.Fwd, Vector3.up);
            Transform t = rootGo.transform;
            s.Entrance = t;

            // Plataforma de piedra a la entrada (profunda, para que se asiente en la pendiente).
            Stone(t, new Vector3(0f, -1.2f, 1.6f), new Vector3(9.5f, 2.5f, 7.0f), "Platform", true, "Masonry", 3f);

            // Arco: dos pilares y un dintel.
            Stone(t, new Vector3(-2.5f, 2.4f, -0.2f), new Vector3(1.6f, 5.0f, 2.2f), "PillarL");
            Stone(t, new Vector3(2.5f, 2.4f, -0.2f), new Vector3(1.6f, 5.0f, 2.2f), "PillarR");
            Stone(t, new Vector3(0f, 5.4f, -0.2f), new Vector3(7.0f, 1.4f, 2.4f), "Lintel");

            // Macizo rocoso alrededor y encima: la cueva parece excavada en la montaña.
            Stone(t, new Vector3(-5.2f, 2.6f, -2.6f), new Vector3(5.5f, 6.0f, 5.0f), "RockMassL", false);
            Stone(t, new Vector3(5.2f, 2.6f, -2.6f), new Vector3(5.5f, 6.0f, 5.0f), "RockMassR", false);
            Stone(t, new Vector3(0f, 7.2f, -3.0f), new Vector3(10.5f, 4.0f, 5.5f), "RockMassTop", false);
            Stone(t, new Vector3(0f, 2.0f, -4.0f), new Vector3(8.0f, 6.0f, 4.0f), "RockMassBack", false);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 1.1f + s.Id;
                Vector3 rp = new Vector3(Mathf.Sin(a) * 4.4f, 0.25f, 3.6f + Mathf.Cos(a * 1.7f) * 0.9f);
                GameObject rub = Stone(t, rp, new Vector3(0.7f, 0.45f, 0.6f) * (0.7f + (i % 3) * 0.3f), "Rubble", false);
                rub.transform.localRotation = Quaternion.Euler(0f, i * 53f, 0f);
            }

            // Fondo oscuro de la cueva (cierra el paso al terreno de detrás).
            GameObject dark = Prim.Box(t, new Vector3(0f, 2.3f, -1.4f), new Vector3(3.6f, 4.6f, 0.6f), new Color(0.02f, 0.02f, 0.03f), true, "CaveDark");
            dark.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Antorchas en los pilares.
            Torch.Create(t, t.TransformPoint(new Vector3(-2.5f, 2.3f, 0.92f)), s.Fwd, true);
            Torch.Create(t, t.TransformPoint(new Vector3(2.5f, 2.3f, 0.92f)), s.Fwd, true);

            Vector3 portalWorld = t.TransformPoint(new Vector3(0f, 0.1f, 0.2f));

            if (!s.Opened)
            {
                // Sello de roca con grietas que brillan. Se rompe cuando el jugador se acerca.
                GameObject seal = Stone(t, new Vector3(0f, 2.2f, 0.25f), new Vector3(3.3f, 4.4f, 1.0f), "Seal", true);
                for (int i = 0; i < 4; i++)
                {
                    GameObject crack = Prim.Make(PrimitiveType.Cube, seal.transform, new Vector3(-0.28f + i * 0.19f, -0.15f + (i % 2) * 0.12f, 0.52f),
                        new Vector3(0.018f, 0.5f - (i % 2) * 0.12f, 0.06f), new Color(1f, 0.85f, 0.45f), false, "Crack", true);
                    crack.transform.localRotation = Quaternion.Euler(0f, 0f, -14f + i * 9f);
                }
                Prim.Make(PrimitiveType.Cube, seal.transform, new Vector3(0f, 0.05f, 0.52f), new Vector3(0.012f, 0.9f, 0.06f), new Color(1f, 0.9f, 0.55f), false, "CrackMain", true);
                Zone.Create(t, s.Pos + s.Fwd * 3.0f, 7.5f, () => OpenSeal(s, seal), false);
            }
            else
            {
                PortalBeam.Create(t, portalWorld, 1.1f, 4.4f, true);
                Zone.Create(t, portalWorld, 1.5f, () => TryEnter(s), false);
            }

            if (s.Rescued) SpawnVillagerOutside(s);
        }

        static void OpenSeal(DungeonSite s, GameObject seal)
        {
            if (s.Opened || seal == null) return;
            if (!Quests.GuildSeal)
            {
                // Sin el emblema del gremio la roca no cede.
                if (Time.time > sealWarnAt)
                {
                    sealWarnAt = Time.time + 8f;
                    Game.Say("La roca está sellada con un emblema antiguo... quizá en el pueblo sepan algo", 5f);
                }
                return;
            }
            s.Opened = true;
            Transform t = s.Entrance;
            Vector3 at = seal.transform.position + s.Fwd * 0.6f;
            LightBurst.Spawn(at, 1.35f, true);
            Object.Destroy(seal);
            Game.Say("Una luz se abre paso entre la roca...", 4f);

            Vector3 portalWorld = t.TransformPoint(new Vector3(0f, 0.1f, 0.2f));
            PortalBeam.Create(t, portalWorld, 1.1f, 4.4f, true);
            Zone.Create(t, portalWorld, 1.5f, () => TryEnter(s), false);
        }

        // ---------- Aldeano rescatado ----------

        public static void SpawnVillagerOutside(DungeonSite s)
        {
            if (s.Entrance == null) return;
            Vector3 pos = s.Ground + s.Right * 3.2f;
            GameObject npc = Npc.Create(s.Entrance, pos, "Aldeano rescatado", new Color(0.35f, 0.55f, 0.75f), new Color(0.35f, 0.22f, 0.12f), true,
                Mathf.Atan2(-s.Right.x, -s.Right.z) * Mathf.Rad2Deg + 25f);
            Interactable it = npc.AddComponent<Interactable>();
            it.Prompt = "Hablar con el aldeano";
            it.Range = 3.0f;
            string[] lines =
            {
                "¡Gracias otra vez! Dicen que en estas montañas hay más cuevas selladas. Cuidado con los goblins.",
                "Los goblins se llevaron mi leña... pero sigo vivo gracias a ti.",
                "Si ves otra columna de luz en la montaña, entra: dentro suele haber tesoros."
            };
            int n = 0;
            it.OnUse = () => { Game.Dialog("Aldeano", lines[n % lines.Length], 5f); n++; };
        }

        // ---------- Entrar y salir ----------

        public static void TryEnter(DungeonSite s)
        {
            if (busy || Active || runner == null) return;
            if (Time.time < cooldownUntil) return;
            if (Game.Player == null || Game.State != GameState.Playing) return;
            busy = true;
            runner.StartCoroutine(EnterRoutine(s));
        }

        /// <summary>Entra sin comprobar el cooldown (modo dev).</summary>
        public static void DevEnter(int index)
        {
            if (runner == null || index < 0 || index >= Sites.Count || Game.Player == null) return;
            if (Active) ForceExit();
            busy = true;
            runner.StartCoroutine(EnterRoutine(Sites[index]));
        }

        static IEnumerator EnterRoutine(DungeonSite s)
        {
            Game.Flash(1.3f);
            Sfx.Play("flash", 0.8f, 0.9f);
            yield return new WaitForSecondsRealtime(0.18f);

            if (s.Maze == null) s.Maze = DungeonInstance.Build(s);
            Physics.SyncTransforms();
            yield return null;

            SaveEnvironment();
            ApplyEnvironment();
            Active = true;
            Current = s;
            Teleport(s.Maze.Spawn, 0f);
            s.Maze.OnEntered();
            Game.Say(Name, 4f);
            cooldownUntil = Time.time + 1.5f;
            busy = false;
        }

        public static void Exit()
        {
            if (busy || !Active || Current == null || runner == null) return;
            busy = true;
            runner.StartCoroutine(ExitRoutine());
        }

        static IEnumerator ExitRoutine()
        {
            DungeonSite s = Current;
            Game.Flash(1.3f);
            Sfx.Play("flash", 0.8f, 0.9f);
            yield return new WaitForSecondsRealtime(0.18f);

            Vector3 outPos = s.Ground + Vector3.up * 0.5f;
            if (IslandWorld.Instance != null) IslandWorld.Instance.EnsureAround(outPos, 1);
            Physics.SyncTransforms();
            yield return null;

            RestoreEnvironment();
            Active = false;
            Current = null;
            float yaw = Mathf.Atan2(s.Fwd.x, s.Fwd.z) * Mathf.Rad2Deg;
            Teleport(outPos, yaw);
            bool rescued = s.Maze != null && s.Maze.CollectVillager();
            if (rescued)
            {
                s.Rescued = true;
                Quests.OnVillagerRescued();
                SpawnVillagerOutside(s);
                Reward();
            }
            cooldownUntil = Time.time + 1.5f;
            busy = false;
        }

        /// <summary>Sale al instante sin animación (teletransportes del modo dev).</summary>
        public static void ForceExit()
        {
            if (!Active) return;
            RestoreEnvironment();
            Active = false;
            Current = null;
            busy = false;
        }

        static void Teleport(Vector3 pos, float yaw)
        {
            if (Game.Player == null) return;
            Game.Player.Teleport(pos);
            Game.Player.ClearLock();
            if (Game.Player.Rig != null) { Game.Player.Rig.Yaw = yaw; Game.Player.Rig.Snap(); }
            if (Game.Partner != null) Game.Partner.Teleport(pos + Game.Player.transform.right * 1.5f);
        }

        static void Reward()
        {
            Game.Gems += 30;
            Game.HasBombs = true;
            Game.Bombs = Mathf.Min(20, Game.Bombs + 5);
            Health h = Game.Player != null ? Game.Player.GetComponent<Health>() : null;
            if (h != null) h.Heal(99f);
            Sfx.Play("pickup");
            Game.Dialog("Aldeano", "¡Me salvaste! Toma esto como agradecimiento: gemas y bombas. ¡Y que descanses!", 7f);
            Game.Say("Aldeano rescatado: +30 gemas, +5 bombas", 5f);
        }

        // ---------- Ambiente ----------

        static void SaveEnvironment()
        {
            if (envSaved) return;
            envSaved = true;
            savedAmbient = RenderSettings.ambientLight;
            savedFog = RenderSettings.fogColor;
            savedFogStart = RenderSettings.fogStartDistance;
            savedFogEnd = RenderSettings.fogEndDistance;
            savedSun = RenderSettings.sun != null ? RenderSettings.sun.intensity : 1f;
            Camera cam = Camera.main;
            savedBg = cam != null ? cam.backgroundColor : Color.black;
        }

        static void ApplyEnvironment()
        {
            Color dark = new Color(0.05f, 0.04f, 0.07f);
            RenderSettings.ambientLight = new Color(0.34f, 0.31f, 0.40f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = dark;
            RenderSettings.fogStartDistance = 18f;
            RenderSettings.fogEndDistance = 80f;
            if (RenderSettings.sun != null) RenderSettings.sun.intensity = 0f;
            Camera cam = Camera.main;
            if (cam != null) cam.backgroundColor = dark;
        }

        static void RestoreEnvironment()
        {
            if (!envSaved) return;
            envSaved = false;
            RenderSettings.ambientLight = savedAmbient;
            RenderSettings.fogColor = savedFog;
            RenderSettings.fogStartDistance = savedFogStart;
            RenderSettings.fogEndDistance = savedFogEnd;
            if (RenderSettings.sun != null) RenderSettings.sun.intensity = savedSun;
            Camera cam = Camera.main;
            if (cam != null) cam.backgroundColor = savedBg;
        }

        // ---------- Consultas ----------

        public static Vector3 RespawnPoint
        {
            get { return Current != null && Current.Maze != null ? Current.Maze.Spawn : Vector3.zero; }
        }

        public static string ObjectiveText
        {
            get { return Current != null && Current.Maze != null ? Current.Maze.ObjectiveText : ""; }
        }

        /// <summary>Punto fuera de la entrada, para teletransportes del modo dev.</summary>
        public static Vector3 OutsidePoint(int index)
        {
            DungeonSite s = Sites[index];
            return s.Ground + Vector3.up * 0.5f + s.Fwd * 1.2f;
        }
    }

    /// <summary>Soporte para corrutinas de la mazmorra (la clase Dungeon es estática).</summary>
    public class DungeonRunner : MonoBehaviour
    {
    }
}
