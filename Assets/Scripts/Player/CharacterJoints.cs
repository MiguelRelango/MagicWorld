using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace Hearthbound
{
    // Rigid primitive meshes follow a humanoid skeleton; no ragdoll forces fight the controller.
    public sealed class CharacterJoints : MonoBehaviour
    {
        readonly Dictionary<HumanBodyBones, Transform> bones = new Dictionary<HumanBodyBones, Transform>();
        readonly Dictionary<HumanBodyBones, Quaternion> neutral = new Dictionary<HumanBodyBones, Quaternion>();
        Transform modelRoot;
        Animator animator;
        Avatar avatar;
        PlayerController player;
        PlayerCombat combat;
        float desiredSpeed, speed, phase, roll = -1f;
        bool clipsAvailable;
        bool crafting;
        PlayableGraph craftGraph;
        AnimationClipPlayable craftPlayable;
        float craftPhase;
        AnimationClip craftClip;
        static readonly int SpeedKey = Animator.StringToHash("Speed");
        static readonly int GroundedKey = Animator.StringToHash("Grounded");

        public Transform Bone(HumanBodyBones bone) { return bones[bone]; }

        Transform Add(HumanBodyBones id, Transform parent, Vector3 modelPosition)
        {
            Transform t = new GameObject(id.ToString()).transform;
            t.SetParent(parent, false);
            t.position = modelRoot.TransformPoint(modelPosition);
            bones[id] = t;
            return t;
        }

        public void Build(Transform visualRoot, Transform oldBody, CharacterModel model, Color cloth, Color skin)
        {
            modelRoot = visualRoot;
            player = model.GetComponent<PlayerController>();
            combat = model.GetComponent<PlayerCombat>();
            var hips = Add(HumanBodyBones.Hips, transform, new Vector3(0,.65f,0));
            var spine = Add(HumanBodyBones.Spine, hips, new Vector3(0,.88f,0));
            var chest = Add(HumanBodyBones.Chest, spine, new Vector3(0,1.10f,0));
            var neck = Add(HumanBodyBones.Neck, chest, new Vector3(0,1.27f,0));
            Add(HumanBodyBones.Head, neck, new Vector3(0,1.50f,0));
            for (int side = -1; side <= 1; side += 2)
            {
                bool left = side < 0;
                var shoulder = Add(left ? HumanBodyBones.LeftShoulder : HumanBodyBones.RightShoulder, chest, new Vector3(side*.30f,1.14f,0));
                var arm = Add(left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm, shoulder, new Vector3(side*.42f,1.14f,0));
                var forearm = Add(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm, arm, new Vector3(side*.70f,1.14f,0));
                Add(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand, forearm, new Vector3(side*.98f,1.14f,0));
                var thigh = Add(left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg, hips, new Vector3(side*.17f,.62f,0));
                var calf = Add(left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg, thigh, new Vector3(side*.17f,.34f,0));
                var foot = Add(left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot, calf, new Vector3(side*.17f,.10f,0));
                Add(left ? HumanBodyBones.LeftToes : HumanBodyBones.RightToes, foot, new Vector3(side*.17f,.08f,.16f));
            }
            // Build the avatar in T-pose before adding meshes or lowering the arms.
            CreateAvatar();
            Bone(HumanBodyBones.LeftUpperArm).localRotation = Quaternion.Euler(0,0,90);
            Bone(HumanBodyBones.RightUpperArm).localRotation = Quaternion.Euler(0,0,-90);
            foreach (var pair in bones) neutral[pair.Key] = pair.Value.localRotation;

            var pieces = new List<Transform>();
            foreach (Transform t in oldBody) pieces.Add(t);
            foreach (Transform t in pieces)
            {
                string n = t.name;
                if (n == "ArmL" || n == "ArmR" || n == "LegL" || n == "LegR")
                { t.gameObject.SetActive(false); Destroy(t.gameObject); continue; }
                HumanBodyBones target = HumanBodyBones.Chest;
                if (n == "Belt") target = HumanBodyBones.Hips;
                else if (n == "BootL") target = HumanBodyBones.LeftFoot;
                else if (n == "BootR") target = HumanBodyBones.RightFoot;
                else if (n == "HandL") target = HumanBodyBones.LeftHand;
                else if (n == "HandR") target = HumanBodyBones.RightHand;
                else if (n.StartsWith("Hair") || n.StartsWith("Eye") || n == "Head" || n == "Helm" || n == "Crest") target = HumanBodyBones.Head;
                else if (n == "PauldronL" || n == "PlateL") target = HumanBodyBones.LeftUpperArm;
                else if (n == "PauldronR" || n == "PlateR") target = HumanBodyBones.RightUpperArm;
                t.SetParent(Bone(target), true);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                bool left = side < 0;
                HumanBodyBones arm = left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm;
                HumanBodyBones lower = left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm;
                HumanBodyBones hand = left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
                HumanBodyBones thigh = left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg;
                HumanBodyBones calf = left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg;
                HumanBodyBones foot = left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot;
                Segment(arm,lower,.18f,cloth); Segment(lower,hand,.16f,cloth);
                Segment(thigh,calf,.22f,cloth*.75f); Segment(calf,foot,.20f,cloth*.75f);
                Joint(lower,.17f,cloth); Joint(calf,.21f,cloth*.75f);
            }
            Transform rightSocket = Socket(HumanBodyBones.RightHand);
            Transform leftSocket = Socket(HumanBodyBones.LeftHand);
            Attach(model.SwordPivot,rightSocket,new Vector3(.03f,.04f,.10f));
            Attach(model.WandPivot,rightSocket,new Vector3(0,.06f,.12f));
            Attach(model.ShieldPivot,leftSocket,new Vector3(-.08f,.16f,.18f));
            Attach(model.BowPivot,leftSocket,new Vector3(0,.12f,.18f));
            animator = gameObject.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            RuntimeAnimatorController clips = Resources.Load<RuntimeAnimatorController>("CharacterMotion/HumanLocomotion");
            clipsAvailable = clips != null && avatar != null && avatar.isValid && avatar.isHuman;
            if (avatar != null && avatar.isValid && avatar.isHuman) animator.avatar = avatar;
            if (clipsAvailable) animator.runtimeAnimatorController = clips;
            animator.enabled = clipsAvailable;
        }

        void CreateAvatar()
        {
            var human = new List<HumanBone>();
            foreach (var pair in bones)
            {
                string name = System.Array.Find(HumanTrait.BoneName, s => s.Replace(" ", "") == pair.Key.ToString());
                if (name != null) human.Add(new HumanBone { boneName=pair.Value.name, humanName=name, limit=new HumanLimit { useDefaultValues=true } });
            }
            Transform parent = transform.parent;
            Vector3 pos = transform.localPosition, scale = transform.localScale;
            Quaternion rot = transform.localRotation;
            transform.SetParent(null, false); transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity); transform.localScale=Vector3.one;
            try
            {
                var skeleton = new List<SkeletonBone>();
                foreach(Transform t in GetComponentsInChildren<Transform>())
                    skeleton.Add(new SkeletonBone { name=t.name, position=t.localPosition, rotation=t.localRotation, scale=t.localScale });
                avatar=AvatarBuilder.BuildHumanAvatar(gameObject,new HumanDescription {
                    human=human.ToArray(),skeleton=skeleton.ToArray(),upperArmTwist=.5f,lowerArmTwist=.5f,
                    upperLegTwist=.5f,lowerLegTwist=.5f,armStretch=.05f,legStretch=.05f,feetSpacing=0f });
                if(avatar != null) avatar.name="Hearthbound procedural humanoid";
                if(avatar == null || !avatar.isValid || !avatar.isHuman)
                    Debug.LogWarning("Humanoid avatar needs validation; using procedural joint animation.");
            }
            finally { transform.SetParent(parent,false);transform.localPosition=pos;transform.localRotation=rot;transform.localScale=scale; }
        }

        void Segment(HumanBodyBones from, HumanBodyBones to, float width, Color color)
        {
            Vector3 a=modelRoot.InverseTransformPoint(Bone(from).position), b=modelRoot.InverseTransformPoint(Bone(to).position);
            var shape=Prim.Cyl(modelRoot,(a+b)*.5f,new Vector3(width,Vector3.Distance(a,b)*.5f,width),color,false,"Limb");
            shape.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
            shape.transform.SetParent(Bone(from),true);
        }
        void Joint(HumanBodyBones bone,float size,Color color)
        { Prim.Ball(Bone(bone),Vector3.zero,Vector3.one*size,color,false,"Joint"); }
        Transform Socket(HumanBodyBones bone)
        {
            Transform socket=new GameObject("EquipmentSocket").transform;
            socket.SetParent(Bone(bone),false);socket.rotation=modelRoot.rotation;
            return socket;
        }
        static void Attach(Transform item,Transform socket,Vector3 offset)
        { if(item == null)return;item.SetParent(socket,false);item.localPosition=offset;item.localRotation=Quaternion.identity; }

        public void Motion(float value) { desiredSpeed=Mathf.Clamp(value,0,1.5f); roll=-1f; }
        public void Roll(float progress) { roll=Mathf.Clamp01(progress);desiredSpeed=0; }
        public void ClearRoll() { roll=-1f; }
        void Pose(HumanBodyBones bone,float x,float y,float z,float blend)
        { Transform t=Bone(bone);t.localRotation=Quaternion.Slerp(t.localRotation,Quaternion.Euler(x,y,z)*neutral[bone],blend); }

        public void StartCraftingMotion()
        {
            StopCraftingMotion(); crafting=true;craftPhase=0;
            var settings=Resources.Load<CraftingMotionSet>("CharacterMotion/CraftingMotion");
            craftClip=settings != null ? settings.Work : null;
            if(craftClip == null || !craftClip.humanMotion || avatar == null || !avatar.isValid || !avatar.isHuman) return;
            animator.enabled=true;animator.applyRootMotion=false;
            craftGraph=PlayableGraph.Create("Crafting motion");craftGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            craftPlayable=AnimationClipPlayable.Create(craftGraph,craftClip);
            craftPlayable.SetApplyFootIK(true);
            var output=AnimationPlayableOutput.Create(craftGraph,"Crafting",animator);output.SetSourcePlayable(craftPlayable);
            craftGraph.Play();
        }
        public void StopCraftingMotion()
        {
            crafting=false;
            if(craftGraph.IsValid())craftGraph.Destroy();
            if(animator != null)animator.enabled=clipsAvailable;
        }

        void LateUpdate()
        {
            if(bones.Count == 0) return;
            bool paused = player != null && (Game.State != GameState.Playing || (player.Hp != null && player.Hp.Dead));
            if (animator != null) animator.speed = paused ? 0f : 1f;
            if (paused) return;
            float dt=Time.deltaTime, blend=1f-Mathf.Exp(-16f*dt);
            if(crafting)
            {
                craftPhase+=dt;
                if(craftGraph.IsValid())
                {
                    craftPlayable.SetTime(craftPhase % Mathf.Max(.01f,craftClip.length));craftGraph.Evaluate(0f);return;
                }
                animator.enabled=false;
                Bone(HumanBodyBones.Hips).localPosition=new Vector3(0,.65f,0);
                Pose(HumanBodyBones.Spine,12f,0,0,blend);Pose(HumanBodyBones.Head,15f,0,0,blend);
                Pose(HumanBodyBones.LeftUpperArm,-65f,0,0,blend);
                Pose(HumanBodyBones.RightUpperArm,-70f+Mathf.Sin(craftPhase*8f)*18f,0,0,blend);
                Pose(HumanBodyBones.LeftLowerArm,0,35f,0,blend);Pose(HumanBodyBones.RightLowerArm,0,-45f,0,blend);
                Pose(HumanBodyBones.LeftUpperLeg,0,0,0,blend);Pose(HumanBodyBones.RightUpperLeg,0,0,0,blend);
                Pose(HumanBodyBones.LeftLowerLeg,0,0,0,blend);Pose(HumanBodyBones.RightLowerLeg,0,0,0,blend);
                return;
            }
            speed=Mathf.Lerp(speed,desiredSpeed,1f-Mathf.Exp(-10f*dt));
            bool airborne=player != null && player.Airborne;
            bool combatPose=combat != null && (combat.MovementLocked || combat.Blocking || combat.Aiming || (combat.Model != null && combat.Model.WandPivot.gameObject.activeSelf));
            bool hasJump = clipsAvailable && animator.HasState(0, Animator.StringToHash("Base Layer.Jump"));
            bool useClips=clipsAvailable && roll < 0 && !combatPose && (!airborne || hasJump);
            animator.enabled=useClips;
            if(useClips)
            {
                animator.SetFloat(SpeedKey,speed);
                animator.SetBool(GroundedKey,!airborne);
                return;
            }
            phase+=dt*Mathf.Lerp(2f,12f,Mathf.Clamp01(speed));
            float stride=Mathf.Sin(phase)*32f*speed;
            float tuck=roll >= 0 ? Mathf.Sin(roll*Mathf.PI) : 0f;
            Bone(HumanBodyBones.Hips).localPosition=new Vector3(0,.65f+Mathf.Abs(Mathf.Sin(phase))*0.025f*speed-.08f*tuck,0);
            Pose(HumanBodyBones.Spine,8f*speed+25f*tuck,0,0,blend);
            Pose(HumanBodyBones.Chest,0,Mathf.Sin(phase)*4f*speed,0,blend);
            Pose(HumanBodyBones.Head,-3f*speed-15f*tuck,0,0,blend);
            Pose(HumanBodyBones.LeftUpperLeg,airborne?-22f:-stride-65f*tuck,0,0,blend);
            Pose(HumanBodyBones.RightUpperLeg,airborne?-12f:stride-65f*tuck,0,0,blend);
            Pose(HumanBodyBones.LeftLowerLeg,airborne?45f:Mathf.Max(0,stride)*1.35f+100f*tuck,0,0,blend);
            Pose(HumanBodyBones.RightLowerLeg,airborne?60f:Mathf.Max(0,-stride)*1.35f+100f*tuck,0,0,blend);
            Pose(HumanBodyBones.LeftFoot,0,0,0,blend);Pose(HumanBodyBones.RightFoot,0,0,0,blend);
            float leftArm=stride*.7f-55f*tuck,rightArm=-stride*.7f-55f*tuck;
            if(combatPose) { rightArm=-65f; leftArm=(combat.Blocking||combat.Aiming)?-65f:-15f; }
            Pose(HumanBodyBones.LeftUpperArm,leftArm,0,-4f,blend);
            Pose(HumanBodyBones.RightUpperArm,rightArm,0,4f,blend);
            Pose(HumanBodyBones.LeftLowerArm,0,12f+18f*speed+65f*tuck,0,blend);
            Pose(HumanBodyBones.RightLowerArm,0,-12f-18f*speed-65f*tuck,0,blend);
        }
        void OnDestroy() { StopCraftingMotion(); if(avatar != null) Destroy(avatar); }
    }
}
