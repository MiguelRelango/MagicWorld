using UnityEngine;

namespace Hearthbound
{
    public sealed class CraftingSystem : MonoBehaviour
    {
        public static CraftingSystem Instance;
        public static bool IsWorking { get { return Instance != null && Instance.working; } }
        Transform station;
        PlayerController actor;
        Health health;
        CharacterJoints joints;
        bool working;
        int recipe;
        float elapsed;
        const float Duration=3f;
        string message="";
        public static string RecipeName(int id) { return id == 0 ? "6 flechas" : "2 bombas"; }
        public static string Cost(int id) { return id == 0 ? "3 madera" : "4 madera + 2 gemas"; }

        public static bool CanCraft(int id, out string reason)
        {
            reason="";
            if(id != 0 && id != 1) reason="Receta desconocida";
            else if(id == 0 && !Game.HasBow) reason="Consigue primero el arco";
            else if(id == 1 && !Game.HasBombs) reason="Desbloquea primero las bombas";
            else if(id == 0 && Game.Arrows > 24) reason="Necesitas espacio para 6 flechas (máximo 30)";
            else if(id == 1 && Game.Bombs > 18) reason="Necesitas espacio para 2 bombas (máximo 20)";
            else if(Game.Wood < (id == 0 ? 3 : 4) || (id == 1 && Game.Gems < 2)) reason="Faltan materiales";
            return reason.Length == 0;
        }
        public static bool TryComplete(int id)
        {
            if(!CanCraft(id,out _)) return false;
            Game.Wood-=id == 0 ? 3 : 4;
            if(id == 0) Game.Arrows+=6;
            else { Game.Gems-=2; Game.Bombs+=2; }
            return true;
        }
        void Awake() { Instance=this; }
        void OnDestroy() { Cancel(false);if(Instance==this)Instance=null; }
        public void Open(Transform bench)
        {
            if(Game.State != GameState.Playing || Game.Player == null || working) return;
            if(Game.Player.Dodging || Game.Player.Airborne || (Game.Player.Combat != null && (Game.Player.Combat.MovementLocked || Game.Player.Combat.Blocking || Game.Player.Combat.Aiming))) return;
            station=bench;message="";
            Game.Panel=UiPanel.Crafting;Game.State=GameState.Paused;Game.PauseFrame=Time.frameCount;Time.timeScale=0;
        }
        void CloseMenu()
        {
            if(Game.Panel != UiPanel.Crafting) return;
            Game.Panel=UiPanel.None;Game.State=GameState.Playing;Game.PauseFrame=Time.frameCount;Time.timeScale=1;
        }
        void Begin(int id)
        {
            if(station == null || Game.Player == null || !CanCraft(id,out message)) return;
            actor=Game.Player;health=actor.Hp;
            if(health == null || health.Dead || Vector3.Distance(actor.transform.position,station.position)>3.2f)return;
            CloseMenu();recipe=id;elapsed=0;working=true;
            Vector3 direction=Nav.Flat(station.position-actor.transform.position);
            if(direction.sqrMagnitude>.01f)actor.transform.rotation=Quaternion.LookRotation(direction);
            joints=actor.GetComponentInChildren<CharacterJoints>();
            if(joints != null)joints.StartCraftingMotion();
            health.OnDamaged+=Damaged;
            Game.Say("Fabricando "+RecipeName(id)+"…",Duration);
        }
        void Damaged(float amount,Vector3 from) { Cancel(true); }
        void Cancel(bool notify)
        {
            bool wasWorking=working;
            working=false;
            if(health != null)health.OnDamaged-=Damaged;
            if(joints != null)joints.StopCraftingMotion();
            health=null;joints=null;actor=null;
            if(notify && wasWorking)Game.Say("Fabricación cancelada. Conservas los materiales.");
        }
        void Update()
        {
            if(Game.Panel==UiPanel.Crafting && Time.frameCount>Game.PauseFrame && GameInput.Pressed(Act.Pause))CloseMenu();
            if(!working)return;
            if(actor == null || actor != Game.Player || health == null || health.Dead || station == null || Game.State==GameState.Menu || Game.State==GameState.Ended)
            { Cancel(false);return; }
            if(Game.State != GameState.Playing)return;
            if(Vector3.Distance(actor.transform.position,station.position)>3.2f){Cancel(true);return;}
            elapsed+=Time.deltaTime;
            if(elapsed<Duration)return;
            bool done=TryComplete(recipe);
            string result=done?"Fabricaste "+RecipeName(recipe):"No se completó: cambiaron los materiales o el espacio disponible";
            Cancel(false);Game.Say(result);if(done)Sfx.Play("pickup");
        }
        void OnGUI()
        {
            if(Game.Panel==UiPanel.Crafting)
            {
                float width=Mathf.Min(480,Screen.width-24);
                GUILayout.BeginArea(new Rect((Screen.width-width)/2,Mathf.Max(12,(Screen.height-300)/2),width,300),GUI.skin.box);
                GUILayout.Label("BANCO DE TRABAJO");
                GUILayout.Label("Madera: "+Game.Wood+"   Gemas: "+Game.Gems);
                GUILayout.Label("Flechas: "+Game.Arrows+"/30   Bombas: "+Game.Bombs+"/20");
                for(int i=0;i<2;i++)
                {
                    bool can=CanCraft(i,out string reason);
                    GUI.enabled=can;
                    if(GUILayout.Button("Fabricar "+RecipeName(i)+" — "+Cost(i),GUILayout.Height(38)))Begin(i);
                    GUI.enabled=true;if(!can)GUILayout.Label(reason);
                }
                if(message.Length>0)GUILayout.Label(message);
                if(GUILayout.Button("Cerrar"))CloseMenu();
                GUILayout.EndArea();
            }
            else if(working && Game.State==GameState.Playing)
            {
                float width=Mathf.Min(360,Screen.width-20);
                GUILayout.BeginArea(new Rect((Screen.width-width)/2,Screen.height-145,width,120),GUI.skin.box);
                GUILayout.Label("Fabricando "+RecipeName(recipe)+" — "+Mathf.RoundToInt(elapsed/Duration*100)+" %");
                if(GUILayout.Button("Cancelar",GUILayout.Height(32)))Cancel(true);
                GUILayout.EndArea();
            }
        }
        public static void Bench(Transform parent,Vector3 pos)
        {
            var go=new GameObject("CraftingBench");go.transform.SetParent(parent,false);go.transform.position=pos;
            WorldLook.Surface(Prim.Box(go.transform,new Vector3(0,.85f,0),new Vector3(1.8f,.16f,.85f),Color.white,true,"BenchTop"),"ChestWood",1.2f);
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
                WorldLook.Surface(Prim.Box(go.transform,new Vector3(x*.72f,.4f,z*.3f),new Vector3(.14f,.8f,.14f),Color.white,true,"BenchLeg"),"ChestWood",1f);
            Prim.Box(go.transform,new Vector3(.3f,.98f,0),new Vector3(.4f,.1f,.3f),new Color(.3f,.32f,.34f),false,"Anvil");
            var interact=go.AddComponent<Interactable>();interact.Prompt="Fabricar en banco de trabajo";interact.Range=2.4f;
            interact.OnUse=()=>{if(Instance!=null)Instance.Open(go.transform);};
            var wood=Prim.Cyl(parent,pos+new Vector3(1.5f,.18f,0),new Vector3(.35f,.6f,.35f),new Color(.45f,.28f,.15f),false,"WoodSupply");
            wood.transform.rotation=Quaternion.Euler(0,0,90);
            var gather=wood.AddComponent<Interactable>();gather.Prompt="Recoger 2 madera";gather.Range=1.6f;
            float ready=0;
            gather.OnUse=()=>{if(Time.time<ready){Game.Say("Espera un momento para recoger más madera");return;}Game.Wood+=2;ready=Time.time+5;Game.Say("+2 madera");Sfx.Play("pickup");};
        }
    }
}
