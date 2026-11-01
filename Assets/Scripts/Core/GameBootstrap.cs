using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Arranque automático: al pulsar Play en CUALQUIER escena crea la cámara, el menú 3D,
    /// el HUD y el director del prólogo. No hace falta configurar nada en la escena.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (Object.FindFirstObjectByType<PrologueDirector>() != null) return;

            // Desactiva luces previas de la escena (la plantilla trae una luz direccional).
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++) lights[i].gameObject.SetActive(false);

            GameInput.Load();
            Settings.Load();
            EnsureCamera();

            Application.targetFrameRate = 60;
            Time.timeScale = 1f;

            GameObject root = new GameObject("Hearthbound");
            PrologueDirector dir = root.AddComponent<PrologueDirector>();
            Hud hud = root.AddComponent<Hud>();
            hud.Director = dir;
            root.AddComponent<Minimap>();
            root.AddComponent<Panels>();
            MenuUI ui = root.AddComponent<MenuUI>();
            ui.Director = dir;

            ui.ShowMainMenu();
            MenuUI.ApplyCameraSpeed();
        }

        static void EnsureCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.98f, 0.70f, 0.52f);
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 250f;
            if (cam.GetComponent<CameraRig>() == null) cam.gameObject.AddComponent<CameraRig>();
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
        }
    }
}
