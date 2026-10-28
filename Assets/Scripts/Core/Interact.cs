using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hearthbound
{
    /// <summary>Objetivo fijable (enemigos, campana, interruptores a distancia).</summary>
    public class Lockable : MonoBehaviour
    {
        public static readonly List<Lockable> All = new List<Lockable>();
        public float AimHeight = 0.9f;
        public bool IsEnemy;

        public Vector3 AimPoint { get { return transform.position + Vector3.up * AimHeight; } }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
    }

    /// <summary>Objeto con el que se interactúa pulsando el botón de acción.</summary>
    public class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();
        public string Prompt = "Interactuar";
        public float Range = 2.2f;
        public bool Enabled = true;
        public Action OnUse;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public static Interactable Nearest(Vector3 pos)
        {
            Interactable best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < All.Count; i++)
            {
                Interactable it = All[i];
                if (!it.Enabled) continue;
                float d = Vector3.Distance(Nav.Flat(it.transform.position), Nav.Flat(pos));
                if (d <= it.Range && d < bestD) { best = it; bestD = d; }
            }
            return best;
        }
    }
}
