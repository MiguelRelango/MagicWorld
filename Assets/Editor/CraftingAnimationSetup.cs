#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace Hearthbound.EditorTools
{
    public class CraftingAnimationSetup : EditorWindow
    {
        AnimationClip clip;
        [MenuItem("Hearthbound/Configurar animación de crafteo")]
        static void Open(){GetWindow<CraftingAnimationSetup>("Animación de crafteo");}
        void OnGUI()
        {
            EditorGUILayout.HelpBox("Importa Human Crafting Animations FREE desde My Assets. Elige un clip Humanoid de trabajo de pie compatible con un banco de trabajo; usa una variante in-place.",MessageType.Info);
            clip=(AnimationClip)EditorGUILayout.ObjectField("Animación",clip,typeof(AnimationClip),false);
            GUI.enabled=clip != null;
            if(GUILayout.Button("Asignar al banco de trabajo"))
            {
                if(!clip.humanMotion){EditorUtility.DisplayDialog("Clip incompatible","Importa su Rig como Humanoid.","OK");return;}
                if(!AssetDatabase.IsValidFolder("Assets/Resources"))AssetDatabase.CreateFolder("Assets","Resources");
                if(!AssetDatabase.IsValidFolder("Assets/Resources/CharacterMotion"))AssetDatabase.CreateFolder("Assets/Resources","CharacterMotion");
                const string path="Assets/Resources/CharacterMotion/CraftingMotion.asset";
                var settings=AssetDatabase.LoadAssetAtPath<CraftingMotionSet>(path);
                if(settings==null){settings=CreateInstance<CraftingMotionSet>();AssetDatabase.CreateAsset(settings,path);}
                Undo.RecordObject(settings,"Asignar animación de crafteo");settings.Work=clip;
                EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();Selection.activeObject=settings;
                Debug.Log("Animación de crafteo asignada. Reinicia Play para probarla.");
            }
            GUI.enabled=true;
        }
    }
}
#endif
