using System;
using UnityEngine;

namespace Hearthbound
{
    /// <summary>Vida con invulnerabilidad corta y filtro de bloqueo (escudo).</summary>
    public class Health : MonoBehaviour
    {
        public float Max = 3f;
        public float Hp = 3f;
        public float InvulTime = 0.5f;
        float invulUntil;

        /// <summary>Si devuelve true, el daño se anula (escudo). Recibe la posición del atacante.</summary>
        public Func<Vector3, bool> Block;
        public Action<float, Vector3> OnDamaged;
        public Action OnDied;

        public bool Dead { get { return Hp <= 0f; } }
        public float Fraction { get { return Max <= 0f ? 0f : Hp / Max; } }

        public void Init(float max)
        {
            Max = max;
            Hp = max;
        }

        public bool Damage(float amount, Vector3 from)
        {
            if (Dead) return false;
            if (Time.time < invulUntil) return false;
            if (Block != null && Block(from)) return false;
            Hp = Mathf.Max(0f, Hp - amount);
            invulUntil = Time.time + InvulTime;
            if (OnDamaged != null) OnDamaged(amount, from);
            if (Hp <= 0f && OnDied != null) OnDied();
            return true;
        }

        public void Heal(float amount)
        {
            if (Dead) return;
            Hp = Mathf.Min(Max, Hp + amount);
        }

        public void Revive(float fraction)
        {
            Hp = Mathf.Max(1f, Max * fraction);
            invulUntil = Time.time + 1.0f;
        }
    }
}
