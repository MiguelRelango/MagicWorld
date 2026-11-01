#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Hearthbound.EditorTools
{
    public class HumanMotionSetup : EditorWindow
    {
        AnimationClip idle, walk, run, jump;
        [MenuItem("Hearthbound/Configurar animaciones Humanoid")]
        static void Open() { GetWindow<HumanMotionSetup>("Animaciones del personaje"); }
        void OnGUI()
        {
            EditorGUILayout.HelpBox("Importa Human Basic Motions FREE desde Package Manager > My Assets. Arrastra aquí los clips Humanoid. Sin asignarlos, el personaje usa sus animaciones articuladas provisionales.",MessageType.Info);
            idle=(AnimationClip)EditorGUILayout.ObjectField("Quieto / Idle",idle,typeof(AnimationClip),false);
            walk=(AnimationClip)EditorGUILayout.ObjectField("Caminar / Walk",walk,typeof(AnimationClip),false);
            run=(AnimationClip)EditorGUILayout.ObjectField("Correr / Run",run,typeof(AnimationClip),false);
            jump=(AnimationClip)EditorGUILayout.ObjectField("Salto (opcional)",jump,typeof(AnimationClip),false);
            EditorGUILayout.HelpBox("Usa clips in-place. Activa Loop Time en Idle, Walk y Run. El desplazamiento y las colisiones siguen a cargo del controlador del juego.",MessageType.None);
            using(new EditorGUI.DisabledScope(idle == null || walk == null || run == null))
                if(GUILayout.Button("Crear controlador de locomoción")) Create();
        }
        void Create()
        {
            foreach(var clip in new[]{idle,walk,run,jump})
                if(clip != null && !clip.humanMotion)
                { EditorUtility.DisplayDialog("Clip incompatible",clip.name+" no está importado como Humanoid. Configura su Rig y vuelve a intentarlo.","OK");return; }
            const string folder="Assets/Resources/CharacterMotion";
            if(!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets","Resources");
            if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resources","CharacterMotion");
            const string path=folder+"/HumanLocomotion.controller";
            // Keep an existing controller intact; updates can be made in Animator.
            if(AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path) != null)
            { Selection.activeObject=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);EditorGUIUtility.PingObject(Selection.activeObject);return; }
            AnimatorController controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded",AnimatorControllerParameterType.Bool);
            AnimatorStateMachine sm=controller.layers[0].stateMachine;
            AnimatorState locomotion=sm.AddState("Locomotion");sm.defaultState=locomotion;
            var tree=new BlendTree {name="Idle Walk Run",blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};
            tree.AddChild(idle,0f);tree.AddChild(walk,1f);tree.AddChild(run,1.5f);
            AssetDatabase.AddObjectToAsset(tree,controller);locomotion.motion=tree;
            if(jump != null)
            {
                AnimatorState air=sm.AddState("Jump");air.motion=jump;
                var takeoff=locomotion.AddTransition(air);takeoff.hasExitTime=false;takeoff.duration=.12f;takeoff.AddCondition(AnimatorConditionMode.IfNot,0,"Grounded");
                var landing=air.AddTransition(locomotion);landing.hasExitTime=false;landing.duration=.16f;landing.AddCondition(AnimatorConditionMode.If,0,"Grounded");
            }
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();Selection.activeObject=controller;
            Debug.Log("Controlador creado. Reinicia Play para cargar los clips en el personaje.");
        }
    }
}
#endif
