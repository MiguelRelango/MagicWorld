using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace Hearthbound
{
    public enum Act
    {
        Attack, Block, Dodge, Lock, Bow, Bomb, Interact, Pause, Partner, Confirm
    }

    /// <summary>
    /// Entrada unificada. Funciona con el nuevo Input System (Unity 6 por defecto)
    /// y con el Input Manager clásico. Teclado, ratón y mando.
    /// </summary>
    public static class GameInput
    {
#if ENABLE_INPUT_SYSTEM
        static readonly List<ButtonControl> buf = new List<ButtonControl>(4);

        static List<ButtonControl> Controls(Act a)
        {
            buf.Clear();
            Keyboard k = Keyboard.current;
            Mouse m = Mouse.current;
            Gamepad g = Gamepad.current;
            switch (a)
            {
                case Act.Attack:
                    if (k != null) buf.Add(k.jKey);
                    if (m != null) buf.Add(m.leftButton);
                    if (g != null) buf.Add(g.buttonWest);
                    break;
                case Act.Block:
                    if (k != null) buf.Add(k.kKey);
                    if (m != null) buf.Add(m.rightButton);
                    if (g != null) buf.Add(g.leftTrigger);
                    break;
                case Act.Dodge:
                    if (k != null) buf.Add(k.spaceKey);
                    if (g != null) buf.Add(g.buttonSouth);
                    break;
                case Act.Lock:
                    if (k != null) buf.Add(k.tabKey);
                    if (g != null) buf.Add(g.rightShoulder);
                    break;
                case Act.Bow:
                    if (k != null) buf.Add(k.fKey);
                    if (g != null) buf.Add(g.buttonNorth);
                    break;
                case Act.Bomb:
                    if (k != null) buf.Add(k.bKey);
                    if (g != null) buf.Add(g.buttonEast);
                    break;
                case Act.Interact:
                    if (k != null) buf.Add(k.eKey);
                    if (g != null) buf.Add(g.leftShoulder);
                    break;
                case Act.Pause:
                    if (k != null) buf.Add(k.escapeKey);
                    if (g != null) buf.Add(g.startButton);
                    break;
                case Act.Partner:
                    if (k != null) buf.Add(k.cKey);
                    if (g != null) buf.Add(g.dpad.down);
                    break;
                case Act.Confirm:
                    if (k != null) { buf.Add(k.enterKey); buf.Add(k.eKey); }
                    if (g != null) buf.Add(g.buttonSouth);
                    break;
            }
            return buf;
        }

        public static bool Pressed(Act a)
        {
            List<ButtonControl> l = Controls(a);
            for (int i = 0; i < l.Count; i++) if (l[i].wasPressedThisFrame) return true;
            return false;
        }

        public static bool Held(Act a)
        {
            List<ButtonControl> l = Controls(a);
            for (int i = 0; i < l.Count; i++) if (l[i].isPressed) return true;
            return false;
        }

        public static bool Released(Act a)
        {
            List<ButtonControl> l = Controls(a);
            for (int i = 0; i < l.Count; i++) if (l[i].wasReleasedThisFrame) return true;
            return false;
        }

        public static Vector2 Move()
        {
            Vector2 v = Vector2.zero;
            Keyboard k = Keyboard.current;
            if (k != null)
            {
                if (k.wKey.isPressed) v.y += 1f;
                if (k.sKey.isPressed) v.y -= 1f;
                if (k.dKey.isPressed) v.x += 1f;
                if (k.aKey.isPressed) v.x -= 1f;
            }
            Gamepad g = Gamepad.current;
            if (g != null)
            {
                Vector2 s = g.leftStick.ReadValue();
                if (s.sqrMagnitude > 0.04f) v += s;
            }
            return Vector2.ClampMagnitude(v, 1f);
        }

        /// <summary>Giro de cámara: flechas izquierda/derecha o stick derecho.</summary>
        public static float CameraAxis()
        {
            float v = 0f;
            Keyboard k = Keyboard.current;
            if (k != null)
            {
                if (k.leftArrowKey.isPressed) v -= 1f;
                if (k.rightArrowKey.isPressed) v += 1f;
            }
            Gamepad g = Gamepad.current;
            if (g != null)
            {
                float s = g.rightStick.ReadValue().x;
                if (Mathf.Abs(s) > 0.2f) v += s;
            }
            return Mathf.Clamp(v, -1f, 1f);
        }

        public static Vector2 PointerDelta()
        {
            return Vector2.zero;
        }
#else
        static KeyCode[] Keys(Act a)
        {
            switch (a)
            {
                case Act.Attack: return new[] { KeyCode.J, KeyCode.Mouse0 };
                case Act.Block: return new[] { KeyCode.K, KeyCode.Mouse1 };
                case Act.Dodge: return new[] { KeyCode.Space };
                case Act.Lock: return new[] { KeyCode.Tab };
                case Act.Bow: return new[] { KeyCode.F };
                case Act.Bomb: return new[] { KeyCode.B };
                case Act.Interact: return new[] { KeyCode.E };
                case Act.Pause: return new[] { KeyCode.Escape };
                case Act.Partner: return new[] { KeyCode.C };
                case Act.Confirm: return new[] { KeyCode.Return, KeyCode.E };
            }
            return new KeyCode[0];
        }

        public static bool Pressed(Act a)
        {
            KeyCode[] ks = Keys(a);
            for (int i = 0; i < ks.Length; i++) if (Input.GetKeyDown(ks[i])) return true;
            return false;
        }

        public static bool Held(Act a)
        {
            KeyCode[] ks = Keys(a);
            for (int i = 0; i < ks.Length; i++) if (Input.GetKey(ks[i])) return true;
            return false;
        }

        public static bool Released(Act a)
        {
            KeyCode[] ks = Keys(a);
            for (int i = 0; i < ks.Length; i++) if (Input.GetKeyUp(ks[i])) return true;
            return false;
        }

        public static Vector2 Move()
        {
            Vector2 v = Vector2.zero;
            if (Input.GetKey(KeyCode.W)) v.y += 1f;
            if (Input.GetKey(KeyCode.S)) v.y -= 1f;
            if (Input.GetKey(KeyCode.D)) v.x += 1f;
            if (Input.GetKey(KeyCode.A)) v.x -= 1f;
            return Vector2.ClampMagnitude(v, 1f);
        }

        public static float CameraAxis()
        {
            float v = 0f;
            if (Input.GetKey(KeyCode.LeftArrow)) v -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) v += 1f;
            return v;
        }

        public static Vector2 PointerDelta()
        {
            return Vector2.zero;
        }
#endif
    }
}
