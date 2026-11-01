using System;
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
        Attack, Block, Dodge, Jump, Lock, Bow, Bomb, Wand, Axe, Plant, Build, Interact, Partner,
        CameraMode, Map, MapSize, Inventory, Dev,
        MoveForward, MoveBack, MoveLeft, MoveRight, CamLeft, CamRight, CamUp, CamDown,
        Pause, Confirm, Sprint
    }

    /// <summary>
    /// Entrada unificada. Funciona con el nuevo Input System (Unity 6 por defecto)
    /// y con el Input Manager clásico. Teclado, ratón y mando.
    /// Las teclas de teclado/ratón son reasignables (2 por acción) y se guardan en PlayerPrefs.
    /// </summary>
    public static class GameInput
    {
        /// <summary>Si es true, el giro de cámara con el ratón usa el botón derecho (y el escudo queda solo en K / LT).</summary>
        public static bool RightClickCamera;

        // ---------- Asignación de teclas ----------

        /// <summary>Acciones que el jugador puede reasignar, en el orden en que se muestran.</summary>
        public static readonly Act[] Rebindable =
        {
            Act.MoveForward, Act.MoveBack, Act.MoveLeft, Act.MoveRight,
            Act.CamLeft, Act.CamRight, Act.CamUp, Act.CamDown,
            Act.Attack, Act.Block, Act.Dodge, Act.Sprint, Act.Jump, Act.Lock, Act.Bow, Act.Bomb, Act.Wand, Act.Axe, Act.Plant, Act.Build,
            Act.Interact, Act.Partner, Act.CameraMode, Act.Map, Act.MapSize, Act.Inventory, Act.Dev
        };

        static readonly Dictionary<Act, KeyCode[]> binds = MakeDefaults();

        static Dictionary<Act, KeyCode[]> MakeDefaults()
        {
            Dictionary<Act, KeyCode[]> d = new Dictionary<Act, KeyCode[]>();
            d[Act.Attack] = new[] { KeyCode.J, KeyCode.Mouse0 };
            d[Act.Block] = new[] { KeyCode.K, KeyCode.Mouse1 };
            d[Act.Dodge] = new[] { KeyCode.LeftControl, KeyCode.RightControl };
            d[Act.Sprint] = new[] { KeyCode.LeftShift, KeyCode.RightShift };
            d[Act.Jump] = new[] { KeyCode.Space, KeyCode.None };
            d[Act.Lock] = new[] { KeyCode.Tab, KeyCode.None };
            d[Act.Bow] = new[] { KeyCode.F, KeyCode.None };
            d[Act.Bomb] = new[] { KeyCode.B, KeyCode.None };
            d[Act.Wand] = new[] { KeyCode.R, KeyCode.None };
            d[Act.Axe] = new[] { KeyCode.T, KeyCode.None };
            d[Act.Plant] = new[] { KeyCode.G, KeyCode.None };
            d[Act.Build] = new[] { KeyCode.X, KeyCode.None };
            d[Act.Interact] = new[] { KeyCode.E, KeyCode.None };
            d[Act.Partner] = new[] { KeyCode.C, KeyCode.None };
            d[Act.CameraMode] = new[] { KeyCode.V, KeyCode.None };
            d[Act.Map] = new[] { KeyCode.M, KeyCode.None };
            d[Act.MapSize] = new[] { KeyCode.N, KeyCode.None };
            d[Act.Inventory] = new[] { KeyCode.I, KeyCode.None };
            d[Act.Dev] = new[] { KeyCode.F1, KeyCode.None };
            d[Act.MoveForward] = new[] { KeyCode.W, KeyCode.None };
            d[Act.MoveBack] = new[] { KeyCode.S, KeyCode.None };
            d[Act.MoveLeft] = new[] { KeyCode.A, KeyCode.None };
            d[Act.MoveRight] = new[] { KeyCode.D, KeyCode.None };
            d[Act.CamLeft] = new[] { KeyCode.LeftArrow, KeyCode.None };
            d[Act.CamRight] = new[] { KeyCode.RightArrow, KeyCode.None };
            d[Act.CamUp] = new[] { KeyCode.UpArrow, KeyCode.None };
            d[Act.CamDown] = new[] { KeyCode.DownArrow, KeyCode.None };
            // Fijas (no reasignables)
            d[Act.Pause] = new[] { KeyCode.Escape, KeyCode.None };
            d[Act.Confirm] = new[] { KeyCode.Return, KeyCode.E };
            return d;
        }

        static readonly Dictionary<Act, KeyCode[]> defaults = MakeDefaults();

        public static KeyCode Get(Act a, int slot)
        {
            KeyCode[] k;
            return binds.TryGetValue(a, out k) && slot >= 0 && slot < k.Length ? k[slot] : KeyCode.None;
        }

        /// <summary>Asigna una tecla. Si otra acción la usaba, se la quita.</summary>
        public static void Set(Act a, int slot, KeyCode kc)
        {
            if (kc != KeyCode.None)
            {
                for (int i = 0; i < Rebindable.Length; i++)
                {
                    KeyCode[] k = binds[Rebindable[i]];
                    for (int j = 0; j < k.Length; j++)
                        if (k[j] == kc && !(Rebindable[i] == a && j == slot)) k[j] = KeyCode.None;
                }
            }
            binds[a][slot] = kc;
            Save();
        }

        public static void ResetDefaults()
        {
            for (int i = 0; i < Rebindable.Length; i++)
            {
                KeyCode[] d = defaults[Rebindable[i]];
                binds[Rebindable[i]] = new[] { d[0], d[1] };
            }
            Save();
        }

        public static void Load()
        {
            for (int i = 0; i < Rebindable.Length; i++)
                for (int j = 0; j < 2; j++)
                {
                    string key = "bind_" + Rebindable[i] + "_" + j;
                    if (PlayerPrefs.HasKey(key)) binds[Rebindable[i]][j] = (KeyCode)PlayerPrefs.GetInt(key);
                }
            if (PlayerPrefs.GetInt("controls_sprint_roll_v1", 0) == 0)
            {
                Set(Act.Dodge, 0, KeyCode.LeftControl);
                Set(Act.Dodge, 1, KeyCode.RightControl);
                Set(Act.Sprint, 0, KeyCode.LeftShift);
                Set(Act.Sprint, 1, KeyCode.RightShift);
                PlayerPrefs.SetInt("controls_sprint_roll_v1", 1);
                Save();
            }
        }

        static void Save()
        {
            for (int i = 0; i < Rebindable.Length; i++)
                for (int j = 0; j < 2; j++)
                    PlayerPrefs.SetInt("bind_" + Rebindable[i] + "_" + j, (int)binds[Rebindable[i]][j]);
            PlayerPrefs.Save();
        }

        public static string Label(Act a)
        {
            switch (a)
            {
                case Act.MoveForward: return "Mover adelante";
                case Act.MoveBack: return "Mover atrás";
                case Act.MoveLeft: return "Mover izquierda";
                case Act.MoveRight: return "Mover derecha";
                case Act.CamLeft: return "Cámara izquierda";
                case Act.CamRight: return "Cámara derecha";
                case Act.CamUp: return "Cámara arriba";
                case Act.CamDown: return "Cámara abajo";
                case Act.Attack: return "Espada";
                case Act.Block: return "Escudo / parry";
                case Act.Dodge: return "Voltereta";
                case Act.Sprint: return "Correr (mantener)";
                case Act.Jump: return "Saltar";
                case Act.Lock: return "Fijar enemigo";
                case Act.Bow: return "Arco";
                case Act.Bomb: return "Bomba";
                case Act.Wand: return "Varita de fuego";
                case Act.Axe: return "Hacha (talar)";
                case Act.Plant: return "Plantar semilla";
                case Act.Build: return "Modo construcción";
                case Act.Interact: return "Interactuar";
                case Act.Partner: return "Ordenar a la pareja";
                case Act.CameraMode: return "Cambiar cámara";
                case Act.Map: return "Mapa grande";
                case Act.MapSize: return "Tamaño del minimapa";
                case Act.Inventory: return "Inventario";
                case Act.Dev: return "Panel dev";
                case Act.Pause: return "Pausa";
            }
            return a.ToString();
        }

        public static string KeyName(KeyCode kc)
        {
            switch (kc)
            {
                case KeyCode.None: return "—";
                case KeyCode.Mouse0: return "Clic izq.";
                case KeyCode.Mouse1: return "Clic der.";
                case KeyCode.Mouse2: return "Clic central";
                case KeyCode.Mouse3: return "Ratón 4";
                case KeyCode.Mouse4: return "Ratón 5";
                case KeyCode.Space: return "Espacio";
                case KeyCode.Return: return "Enter";
                case KeyCode.LeftShift: return "Shift izq.";
                case KeyCode.RightShift: return "Shift der.";
                case KeyCode.LeftControl: return "Ctrl izq.";
                case KeyCode.RightControl: return "Ctrl der.";
                case KeyCode.LeftAlt: return "Alt izq.";
                case KeyCode.RightAlt: return "Alt der.";
                case KeyCode.UpArrow: return "Flecha arriba";
                case KeyCode.DownArrow: return "Flecha abajo";
                case KeyCode.LeftArrow: return "Flecha izq.";
                case KeyCode.RightArrow: return "Flecha der.";
                case KeyCode.Escape: return "Esc";
                case KeyCode.CapsLock: return "Bloq Mayús";
                case KeyCode.BackQuote: return "`";
            }
            string n = kc.ToString();
            if (n.StartsWith("Alpha")) return n.Substring(5);
            if (n.StartsWith("Keypad")) return "Num " + n.Substring(6);
            return n;
        }

        /// <summary>Lee la tecla o botón del ratón pulsado a partir de un evento de IMGUI (para reasignar).</summary>
        public static bool TryCapture(Event e, out KeyCode kc)
        {
            kc = KeyCode.None;
            if (e == null) return false;
            if (e.type == EventType.KeyDown && e.keyCode != KeyCode.None) { kc = e.keyCode; return true; }
            if (e.type == EventType.MouseDown && e.button >= 0 && e.button <= 4) { kc = (KeyCode)((int)KeyCode.Mouse0 + e.button); return true; }
            return false;
        }

        // ---------- Lectura ----------

        static bool Skip(Act a, KeyCode kc)
        {
            return kc == KeyCode.None || (a == Act.Block && RightClickCamera && kc == KeyCode.Mouse1);
        }

#if ENABLE_INPUT_SYSTEM
        static readonly List<ButtonControl> buf = new List<ButtonControl>(8);
        static readonly Dictionary<KeyCode, Key> keyCache = new Dictionary<KeyCode, Key>();

        static Key ToKey(KeyCode kc)
        {
            Key key;
            if (keyCache.TryGetValue(kc, out key)) return key;
            string n = kc.ToString();
            if (n.StartsWith("Alpha")) n = "Digit" + n.Substring(5);
            else if (n.StartsWith("Keypad")) n = "Numpad" + n.Substring(6);
            else if (n == "Return") n = "Enter";
            else if (n == "LeftControl") n = "LeftCtrl";
            else if (n == "RightControl") n = "RightCtrl";
            else if (n == "BackQuote") n = "Backquote";
            if (!Enum.TryParse(n, out key)) key = Key.None;
            keyCache[kc] = key;
            return key;
        }

        static ButtonControl Ctl(KeyCode kc)
        {
            if (kc >= KeyCode.Mouse0 && kc <= KeyCode.Mouse4)
            {
                Mouse m = Mouse.current;
                if (m == null) return null;
                switch (kc)
                {
                    case KeyCode.Mouse0: return m.leftButton;
                    case KeyCode.Mouse1: return m.rightButton;
                    case KeyCode.Mouse2: return m.middleButton;
                    case KeyCode.Mouse3: return m.backButton;
                    default: return m.forwardButton;
                }
            }
            Keyboard k = Keyboard.current;
            if (k == null) return null;
            Key key = ToKey(kc);
            return key == Key.None ? null : k[key];
        }

        static List<ButtonControl> Controls(Act a)
        {
            buf.Clear();
            KeyCode[] ks;
            if (binds.TryGetValue(a, out ks))
            {
                for (int i = 0; i < ks.Length; i++)
                {
                    if (Skip(a, ks[i])) continue;
                    ButtonControl c = Ctl(ks[i]);
                    if (c != null) buf.Add(c);
                }
            }
            Gamepad g = Gamepad.current;
            if (g != null)
            {
                switch (a)
                {
                    case Act.Attack: buf.Add(g.buttonWest); break;
                    case Act.Block: buf.Add(g.leftTrigger); break;
                    case Act.Dodge: buf.Add(g.leftStickButton); break;
                    case Act.Jump: buf.Add(g.buttonSouth); break;
                    case Act.Lock: buf.Add(g.rightShoulder); break;
                    case Act.Bow: buf.Add(g.buttonNorth); break;
                    case Act.Bomb: buf.Add(g.buttonEast); break;
                    case Act.Interact: buf.Add(g.leftShoulder); break;
                    case Act.Pause: buf.Add(g.startButton); break;
                    case Act.Partner: buf.Add(g.dpad.down); break;
                    case Act.Confirm: buf.Add(g.buttonSouth); break;
                    case Act.CameraMode: buf.Add(g.rightStickButton); break;
                    case Act.Map: buf.Add(g.dpad.up); break;
                    case Act.MapSize: buf.Add(g.dpad.left); break;
                    case Act.Inventory: buf.Add(g.selectButton); break;
                    case Act.Wand: buf.Add(g.rightTrigger); break;
                    case Act.Axe: buf.Add(g.dpad.right); break;
                }
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

        /// <summary>Pulsación de una tecla concreta (no reasignable), p. ej. los números de la barra de construcción.</summary>
        public static bool RawPressed(KeyCode kc)
        {
            ButtonControl c = Ctl(kc);
            return c != null && c.wasPressedThisFrame;
        }

        /// <summary>Posición del puntero en píxeles (origen abajo a la izquierda).</summary>
        public static Vector2 PointerPos()
        {
            Mouse m = Mouse.current;
            return m != null ? m.position.ReadValue() : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        public static Vector2 Move()
        {
            Vector2 v = Vector2.zero;
            if (Held(Act.MoveForward)) v.y += 1f;
            if (Held(Act.MoveBack)) v.y -= 1f;
            if (Held(Act.MoveRight)) v.x += 1f;
            if (Held(Act.MoveLeft)) v.x -= 1f;
            Gamepad g = Gamepad.current;
            if (g != null)
            {
                Vector2 s = g.leftStick.ReadValue();
                if (s.sqrMagnitude > 0.04f) v += s;
            }
            return Vector2.ClampMagnitude(v, 1f);
        }

        /// <summary>Giro de cámara: teclas de cámara izquierda/derecha o stick derecho.</summary>
        public static float CameraAxis()
        {
            float v = 0f;
            if (Held(Act.CamLeft)) v -= 1f;
            if (Held(Act.CamRight)) v += 1f;
            Gamepad g = Gamepad.current;
            if (g != null)
            {
                float s = g.rightStick.ReadValue().x;
                if (Mathf.Abs(s) > 0.2f) v += s;
            }
            return Mathf.Clamp(v, -1f, 1f);
        }

        /// <summary>Inclinación de cámara con las teclas arriba/abajo o el stick derecho vertical.</summary>
        public static float CameraPitchAxis()
        {
            float v = 0f;
            if (Held(Act.CamUp)) v += 1f;
            if (Held(Act.CamDown)) v -= 1f;
            Gamepad g = Gamepad.current;
            if (g != null)
            {
                float s = g.rightStick.ReadValue().y;
                if (Mathf.Abs(s) > 0.2f) v += s;
            }
            return Mathf.Clamp(v, -1f, 1f);
        }

        /// <summary>true mientras se mantiene pulsado el botón de giro de cámara del ratón.</summary>
        public static bool DragHeld()
        {
            Mouse m = Mouse.current;
            if (m == null) return false;
            return RightClickCamera ? m.rightButton.isPressed : m.middleButton.isPressed;
        }

        /// <summary>Movimiento del ratón en píxeles desde el fotograma anterior.</summary>
        public static Vector2 PointerDelta()
        {
            Mouse m = Mouse.current;
            return m != null ? m.delta.ReadValue() : Vector2.zero;
        }

        /// <summary>Rueda del ratón: positivo = hacia delante.</summary>
        public static float Scroll()
        {
            Mouse m = Mouse.current;
            if (m == null) return 0f;
            float y = m.scroll.ReadValue().y;
            if (Mathf.Abs(y) > 5f) y /= 120f;
            return y;
        }
#else
        public static bool Pressed(Act a)
        {
            KeyCode[] ks;
            if (!binds.TryGetValue(a, out ks)) return false;
            for (int i = 0; i < ks.Length; i++) if (!Skip(a, ks[i]) && Input.GetKeyDown(ks[i])) return true;
            return false;
        }

        public static bool Held(Act a)
        {
            KeyCode[] ks;
            if (!binds.TryGetValue(a, out ks)) return false;
            for (int i = 0; i < ks.Length; i++) if (!Skip(a, ks[i]) && Input.GetKey(ks[i])) return true;
            return false;
        }

        public static bool Released(Act a)
        {
            KeyCode[] ks;
            if (!binds.TryGetValue(a, out ks)) return false;
            for (int i = 0; i < ks.Length; i++) if (!Skip(a, ks[i]) && Input.GetKeyUp(ks[i])) return true;
            return false;
        }

        public static bool RawPressed(KeyCode kc)
        {
            return Input.GetKeyDown(kc);
        }

        public static Vector2 PointerPos()
        {
            return Input.mousePosition;
        }

        public static Vector2 Move()
        {
            Vector2 v = Vector2.zero;
            if (Held(Act.MoveForward)) v.y += 1f;
            if (Held(Act.MoveBack)) v.y -= 1f;
            if (Held(Act.MoveRight)) v.x += 1f;
            if (Held(Act.MoveLeft)) v.x -= 1f;
            return Vector2.ClampMagnitude(v, 1f);
        }

        public static float CameraAxis()
        {
            float v = 0f;
            if (Held(Act.CamLeft)) v -= 1f;
            if (Held(Act.CamRight)) v += 1f;
            return v;
        }

        public static float CameraPitchAxis()
        {
            float v = 0f;
            if (Held(Act.CamUp)) v += 1f;
            if (Held(Act.CamDown)) v -= 1f;
            return v;
        }

        public static bool DragHeld()
        {
            return Input.GetMouseButton(RightClickCamera ? 1 : 2);
        }

        public static Vector2 PointerDelta()
        {
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 10f;
        }

        public static float Scroll()
        {
            return Input.mouseScrollDelta.y;
        }
#endif
    }
}
