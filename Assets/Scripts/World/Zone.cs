using System;
using UnityEngine;

namespace Hearthbound
{
    /// <summary>Zona de activación por distancia (sin físicas). Dispara una sola vez.</summary>
    public class Zone : MonoBehaviour
    {
        public float Radius = 6f;
        public Action OnEnter;
        public bool Once = true;
        bool fired;

        /// <summary>Si once es false, llama a onEnter cada fotograma mientras el jugador esté dentro.</summary>
        public static Zone Create(Transform parent, Vector3 pos, float radius, Action onEnter, bool once = false)
        {
            GameObject go = new GameObject("Zone");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            Zone z = go.AddComponent<Zone>();
            z.Radius = radius;
            z.OnEnter = onEnter;
            z.Once = once;
            return z;
        }

        void Update()
        {
            if (fired && Once) return;
            if (Game.State != GameState.Playing || Game.Player == null) return;
            float d = Vector3.Distance(Nav.Flat(transform.position), Nav.Flat(Game.Player.transform.position));
            if (d <= Radius)
            {
                fired = true;
                if (OnEnter != null) OnEnter();
            }
        }
    }

    /// <summary>NPC sencillo hecho con primitivas, con interacción de diálogo.</summary>
    public static class Npc
    {
        public static GameObject Create(Transform parent, Vector3 pos, string name, Color outfit, Color hair, bool beard, float yaw)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Color skin = new Color(0.90f, 0.72f, 0.58f);
            Prim.Cyl(go.transform, new Vector3(-0.14f, 0.28f, 0f), new Vector3(0.18f, 0.28f, 0.18f), outfit * 0.7f, false, "LegL");
            Prim.Cyl(go.transform, new Vector3(0.14f, 0.28f, 0f), new Vector3(0.18f, 0.28f, 0.18f), outfit * 0.7f, false, "LegR");
            Prim.Cyl(go.transform, new Vector3(0f, 0.78f, 0f), new Vector3(0.62f, 0.38f, 0.46f), outfit, false, "Torso");
            Prim.Cyl(go.transform, new Vector3(-0.40f, 0.82f, 0f), new Vector3(0.15f, 0.25f, 0.15f), outfit, false, "ArmL");
            Prim.Cyl(go.transform, new Vector3(0.40f, 0.82f, 0f), new Vector3(0.15f, 0.25f, 0.15f), outfit, false, "ArmR");
            Prim.Ball(go.transform, new Vector3(0f, 1.42f, 0f), new Vector3(0.50f, 0.52f, 0.50f), skin, false, "Head");
            Prim.Ball(go.transform, new Vector3(-0.10f, 1.45f, 0.21f), Vector3.one * 0.07f, new Color(0.1f, 0.1f, 0.12f), false, "EyeL");
            Prim.Ball(go.transform, new Vector3(0.10f, 1.45f, 0.21f), Vector3.one * 0.07f, new Color(0.1f, 0.1f, 0.12f), false, "EyeR");
            Prim.Ball(go.transform, new Vector3(0f, 1.58f, -0.03f), new Vector3(0.54f, 0.32f, 0.54f), hair, false, "Hair");
            if (beard) Prim.Ball(go.transform, new Vector3(0f, 1.20f, 0.14f), new Vector3(0.38f, 0.34f, 0.28f), hair, false, "Beard");
            CapsuleCollider cap = go.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0f, 0.9f, 0f);
            cap.height = 1.8f;
            cap.radius = 0.4f;
            return go;
        }
    }
}
