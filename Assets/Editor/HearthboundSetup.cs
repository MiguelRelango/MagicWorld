#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hearthbound.EditorTools
{
    /// <summary>Menú Hearthbound del editor: crea la escena de prólogo y comprueba la configuración.</summary>
    public static class HearthboundSetup
    {
        const string ScenePath = "Assets/Scenes/Prologue.unity";

        [MenuItem("Hearthbound/Crear escena del prólogo")]
        public static void CreateScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            Camera c = cam.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.98f, 0.70f, 0.52f);
            cam.AddComponent<AudioListener>();
            cam.transform.position = new Vector3(0f, 2f, -8f);

            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettingsScene[] old = EditorBuildSettings.scenes;
            bool has = false;
            for (int i = 0; i < old.Length; i++) if (old[i].path == ScenePath) has = true;
            if (!has)
            {
                EditorBuildSettingsScene[] list = new EditorBuildSettingsScene[old.Length + 1];
                System.Array.Copy(old, list, old.Length);
                list[old.Length] = new EditorBuildSettingsScene(ScenePath, true);
                EditorBuildSettings.scenes = list;
            }
            Debug.Log("Hearthbound: escena creada en " + ScenePath + ". Pulsa Play.");
        }

        [MenuItem("Hearthbound/Comprobar configuración")]
        public static void CheckSetup()
        {
            string msg = "Pipeline de render: " + (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null
                ? UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline.name : "Built-in") + "\n";
#if ENABLE_INPUT_SYSTEM
            msg += "Input System (nuevo): activo\n";
#else
            msg += "Input Manager clásico: activo\n";
#endif
            msg += "El juego se inicia solo al pulsar Play en cualquier escena.";
            EditorUtility.DisplayDialog("Hearthbound", msg, "OK");
        }
    }
}
#endif
