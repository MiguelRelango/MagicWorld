using UnityEngine;

namespace Hearthbound
{
    /// <summary>Ajustes del jugador guardados en PlayerPrefs (cámara, modo dev...).</summary>
    public static class Settings
    {
        public static float MouseSens = 1f;     // sensibilidad del ratón al arrastrar
        public static bool InvertX, InvertY;    // inversión de los ejes de cámara
        public static float CamFollow = 8f;     // rapidez con la que la cámara sigue al personaje (suavizado)
        public static bool Dev;                 // modo desarrollador

        static bool loaded;

        public static void Load()
        {
            if (loaded) return;
            loaded = true;
            MouseSens = PlayerPrefs.GetFloat("mouseSens", 1f);
            InvertX = PlayerPrefs.GetInt("invX", 0) == 1;
            InvertY = PlayerPrefs.GetInt("invY", 0) == 1;
            CamFollow = PlayerPrefs.GetFloat("camFollow", 8f);
            Dev = PlayerPrefs.GetInt("dev", 0) == 1;
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat("mouseSens", MouseSens);
            PlayerPrefs.SetInt("invX", InvertX ? 1 : 0);
            PlayerPrefs.SetInt("invY", InvertY ? 1 : 0);
            PlayerPrefs.SetFloat("camFollow", CamFollow);
            PlayerPrefs.SetInt("dev", Dev ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void ResetCamera()
        {
            MouseSens = 1f;
            InvertX = InvertY = false;
            CamFollow = 8f;
            MenuUI.CameraSpeed = 1f;
            PlayerPrefs.SetFloat("camSpeed", 1f);
            Save();
            MenuUI.ApplyCameraSpeed();
        }
    }
}
