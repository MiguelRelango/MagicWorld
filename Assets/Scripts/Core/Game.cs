using UnityEngine;

namespace Hearthbound
{
    public enum GameState { Menu, Playing, Paused, Ended }

    /// <summary>Estado global simple del prólogo.</summary>
    public static class Game
    {
        public static GameState State = GameState.Menu;
        public static PlayerController Player;
        public static Partner Partner;
        public static CharacterLook Look = new CharacterLook();

        public static bool HasSword, HasShield, HasBow, HasBombs;
        public static bool HasLeather, HasWand;
        public static string DialogSpeaker = "";
        public static string DialogText = "";
        public static float DialogUntil;
        public static int Bombs, Arrows, ArmorLevel = 1;
        public static int Wood, Gems;
        public static int PauseFrame = -1;
        public static string Objective = "";
        public static string Toast = "";
        public static float ToastUntil;

        public static void Reset()
        {
            HasSword = HasShield = HasBow = HasBombs = false;
            HasLeather = HasWand = false;
            DialogSpeaker = "";
            DialogText = "";
            DialogUntil = 0f;
            Bombs = 0;
            Arrows = 0;
            ArmorLevel = 1;
            Wood = 0;
            Gems = 0;
            Objective = "";
            Toast = "";
            ToastUntil = 0f;
            Player = null;
            Partner = null;
        }

        public static void Dialog(string speaker, string text, float seconds = 5f)
        {
            DialogSpeaker = speaker;
            DialogText = text;
            DialogUntil = Time.unscaledTime + seconds;
        }

        public static void Say(string msg, float seconds = 3.5f)
        {
            Toast = msg;
            ToastUntil = Time.unscaledTime + seconds;
        }
    }
}
