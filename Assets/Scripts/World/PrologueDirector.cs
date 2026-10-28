using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hearthbound
{
    /// <summary>
    /// Dirige el prólogo: crea al jugador, la cámara y la pareja, y gestiona los objetivos:
    /// casa -> espada/escudo -> huerto -> arco y campana -> bombas -> cueva -> hogar.
    /// </summary>
    public class PrologueDirector : MonoBehaviour
    {
        // Referencias que rellena PrologueWorld
        public Interactable GearRack, Workbench, Elder;
        public BellTarget Bell;
        public BreakableWall CrackedWall, FinalRock;
        public PressurePlate Plate;
        public Chest Treasure;

        // Estado
        bool talkedPartner, gearTaken, gardenStarted, gardenDone, bowGiven, bellRung;
        bool caveEntered, wallBroken, doorOpened, ambushed, rockBroken, treasureOpened, benchUsed;
        public bool Finished { get; private set; }

        readonly List<EnemySlime> gardenSlimes = new List<EnemySlime>();
        PlayerController player;
        CharacterModel model;
        Health hp;
        CameraRig rig;
        Partner partner;
        Vector3 checkpoint;
        float deathTimer;
        float refillCooldown;
        float pitCooldown;
        public bool Dead { get; private set; }

        // ---------- Inicio y fin ----------

        public void BeginPrologue()
        {
            StopAllCoroutines();
            Cleanup();
            Game.Reset();
            talkedPartner = gearTaken = gardenStarted = gardenDone = bowGiven = bellRung = false;
            caveEntered = wallBroken = doorOpened = ambushed = rockBroken = treasureOpened = benchUsed = false;
            Finished = false;
            Dead = false;
            gardenSlimes.Clear();
            checkpoint = PrologueWorld.PlayerStart;

            PrologueWorld.Build(this);
            SpawnPlayer();
            SpawnPartner();

            Game.State = GameState.Playing;
            UpdateObjective();
            Game.Dialog("Pareja", "Buenos días. Hay ruidos raros cerca del huerto... ¿Puedes ayudarme? (Acércate y pulsa E)", 7f);
        }

        public void Cleanup()
        {
            PrologueWorld.Clear();
            if (player != null) Destroy(player.gameObject);
            if (partner != null) Destroy(partner.gameObject);
            if (rig != null) { rig.Target = null; rig.LockTarget = null; }
            Lockable.All.Clear();
            Interactable.All.Clear();
            Refuge.All.Clear();
            Game.Player = null;
            Game.Partner = null;
        }

        void SpawnPlayer()
        {
            GameObject go = new GameObject("Player");
            go.transform.position = PrologueWorld.PlayerStart;
            go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            hp = go.AddComponent<Health>();
            ApplyArmorStats(true);
            player = go.AddComponent<PlayerController>();
            model = go.AddComponent<CharacterModel>();
            PlayerCombat combat = go.AddComponent<PlayerCombat>();

            GameObject camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
            if (Camera.main == null)
            {
                Camera c = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
                camGo.AddComponent<AudioListener>();
                c.clearFlags = CameraClearFlags.SolidColor;
                c.backgroundColor = new Color(0.62f, 0.82f, 0.95f);
                c.fieldOfView = 50f;
                c.nearClipPlane = 0.2f;
                c.farClipPlane = 250f;
            }
            rig = camGo.GetComponent<CameraRig>();
            if (rig == null) rig = camGo.AddComponent<CameraRig>();
            rig.Target = go.transform;
            rig.Yaw = 0f;
            rig.LockTarget = null;

            player.Rig = rig;
            player.Model = model;
            player.Hp = hp;
            player.Combat = combat;
            combat.Pc = player;
            combat.Model = model;
            combat.Hp = hp;
            hp.Block = null;
            combat.enabled = false;
            combat.enabled = true;
            // Reasigna el filtro de bloqueo (PlayerCombat lo establece en Awake, pero Hp pudo cambiar).
            hp.Block = (Vector3 from) => { return combat.Blocking && Game.HasShield && Vector3.Angle(player.transform.forward, Nav.Flat(from - player.transform.position)) <= 80f && BlockEffect(combat, from); };

            hp.OnDied = OnPlayerDied;
            hp.OnDamaged = (float a, Vector3 f) => { Sfx.Play("hurt"); };

            RefreshModel();
            Game.Player = player;
            rig.Snap();
        }

        bool BlockEffect(PlayerCombat combat, Vector3 from)
        {
            Sfx.Play("shield");
            return true;
        }

        void SpawnPartner()
        {
            partner = Partner.Spawn(PrologueWorld.PartnerStart, null);
            partner.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            Game.Partner = partner;
            Interactable it = partner.gameObject.AddComponent<Interactable>();
            it.Prompt = "Hablar con tu pareja";
            it.Range = 3.2f;
            it.OnUse = OnPartnerTalked;
        }

        public void RefreshModel()
        {
            if (model != null) model.Build(Game.Look, Game.ArmorLevel, Game.HasSword, Game.HasShield, Game.HasBow);
        }

        void ApplyArmorStats(bool full)
        {
            if (hp == null) return;
            float max = 5f + 2f * (Game.ArmorLevel - 1);
            float frac = hp.Max > 0f ? hp.Hp / hp.Max : 1f;
            hp.Max = max;
            hp.Hp = full ? max : Mathf.Min(max, frac * max);
        }

        // ---------- Eventos del mundo ----------

        public void OnPartnerTalked()
        {
            talkedPartner = true;
            if (!gearTaken)
                Game.Dialog("Pareja", "Toma tu espada y tu escudo del perchero. Y ten cuidado: con el escudo, si bloqueas justo al golpe, haces un parry.", 7f);
            else if (!gardenDone)
                Game.Dialog("Pareja", "Los slimes están en el huerto, al oeste. Yo me quedo cerca de la caja. ¡Confío en ti!", 6f);
            else
                Game.Dialog("Pareja", "Pulsa C para decirme que te siga, espere o busque refugio. Siempre estaré cerca.", 6f);
            UpdateObjective();
        }

        public void OnGearTaken()
        {
            gearTaken = true;
            Game.HasSword = true;
            Game.HasShield = true;
            RefreshModel();
            Sfx.Play("pickup");
            Game.Say("Espada y escudo equipados", 3f);
            Game.Dialog("Pareja", "Te sienta bien. Al huerto, al oeste de casa. Tab fija al enemigo, J ataca, K bloquea.", 7f);
            UpdateObjective();
        }

        public void OnGardenEntered()
        {
            if (gardenStarted || !gearTaken) return;
            gardenStarted = true;
            Vector3 c = PrologueWorld.GardenCenter;
            Vector3[] spots = { c + new Vector3(-3f, 0f, -2f), c + new Vector3(2f, 0f, 3f), c + new Vector3(3f, 0f, -3f) };
            for (int i = 0; i < spots.Length; i++)
                gardenSlimes.Add(EnemySlime.Spawn(spots[i], PrologueWorld.Root));
            Game.Dialog("Pareja", "¡Slimes! Me escondo tras la caja. ¡Mantenlos lejos de mí!", 5f);
            Sfx.Play("hurt", 0.6f, 0.5f);
            UpdateObjective();
        }

        public void OnElderTalked()
        {
            if (!gardenDone)
            {
                Game.Dialog("Anciano", "Ya oí los slimes. Ahuyéntalos del huerto y vuelve a verme.", 5f);
                return;
            }
            if (!bowGiven)
            {
                bowGiven = true;
                Game.HasBow = true;
                Game.Arrows = 12;
                RefreshModel();
                Sfx.Play("pickup");
                Game.Dialog("Anciano", "Toma este arco. Mantén F para apuntar y suéltalo para disparar. Prueba con la campana al este: algo cuelga de ella.", 8f);
            }
            else if (!bellRung)
            {
                Game.Dialog("Anciano", "La campana está al este, cerca del camino. Fíjala con Tab para apuntar mejor.", 5f);
            }
            else
            {
                Game.Dialog("Anciano", "Las ruinas están al este, pasando el letrero. Cuidado con los muros agrietados: solo las bombas los rompen.", 7f);
            }
            UpdateObjective();
        }

        public void OnBellRung()
        {
            if (bellRung) return;
            bellRung = true;
            Pickup.Spawn(PrologueWorld.BellPos + new Vector3(-1.6f, 0f, -1.8f), PickupKind.Bombs, 8);
            Game.Dialog("Pareja", "¡Cayó una bolsa de la campana! Parecen bombas. Recógela.", 6f);
            UpdateObjective();
        }

        public void OnCaveEntered()
        {
            if (caveEntered) return;
            caveEntered = true;
            checkpoint = new Vector3(41f, 0.2f, 30f);
            if (!Game.HasBombs) Game.Say("Te falta equipo: vuelve por las bombas de la campana", 4f);
            Game.Dialog("Pareja", "Qué oscuro... Mira ese muro con grietas naranjas. Lanza una bomba con B desde lejos.", 7f);
            UpdateObjective();
        }

        public void OnWallBroken()
        {
            wallBroken = true;
            checkpoint = new Vector3(63f, 0.2f, 30f);
            Game.Dialog("Pareja", "¡Así se hace! Adelante hay un foso. Lanza una bomba al interruptor del otro lado.", 7f);
            UpdateObjective();
        }

        public void OnDoorOpened()
        {
            doorOpened = true;
            checkpoint = new Vector3(76f, 0.2f, 30f);
            UpdateObjective();
        }

        public void OnAmbush()
        {
            if (ambushed || !doorOpened) return;
            ambushed = true;
            checkpoint = new Vector3(80.5f, 0.2f, 30f);
            for (int i = 0; i < PrologueWorld.AmbushPoints.Length; i++)
                EnemySlime.Spawn(PrologueWorld.AmbushPoints[i], PrologueWorld.Root);
            Game.Dialog("Pareja", "¡Más slimes! Voy al refugio de la esquina. Luego vuela esa roca del fondo.", 6f);
            UpdateObjective();
        }

        public void OnRockBroken()
        {
            rockBroken = true;
            Game.Say("Hay un cofre detrás de la roca", 3.5f);
            UpdateObjective();
        }

        public void OnTreasureOpened()
        {
            treasureOpened = true;
            Game.HasLeather = true;
            Game.HasWand = true;
            Sfx.Play("heal");
            Game.Say("Obtienes cuero reforzado y una varita (próximamente)", 5f);
            Game.Dialog("Pareja", "¡Cuero reforzado! Vamos a casa: en la mesa de trabajo puedo ayudarte a hacer una armadura nueva.", 7f);
            UpdateObjective();
        }

        public void OnBenchUsed()
        {
            if (!treasureOpened)
            {
                Game.Dialog("Pareja", "Aún no tenemos materiales. Hay ruinas al este: algo útil nos espera.", 5f);
                return;
            }
            if (benchUsed) return;
            benchUsed = true;
            Game.ArmorLevel = 2;
            ApplyArmorStats(true);
            RefreshModel();
            Sfx.Play("heal");
            Game.Say("Armadura nivel 2 creada: más vida y defensa", 5f);
            StartCoroutine(EndRoutine());
            UpdateObjective();
        }

        IEnumerator EndRoutine()
        {
            yield return new WaitForSeconds(3.5f);
            Finished = true;
            Game.State = GameState.Ended;
        }

        // ---------- Bucle ----------

        void Update()
        {
            if (Game.State != GameState.Playing || player == null) return;

            // Pausa
            if (GameInput.Pressed(Act.Pause))
            {
                Game.State = GameState.Paused;
                Game.PauseFrame = Time.frameCount;
                Time.timeScale = 0f;
                return;
            }

            // Victoria del huerto
            if (gardenStarted && !gardenDone)
            {
                gardenSlimes.RemoveAll(s => s == null);
                if (gardenSlimes.Count == 0)
                {
                    gardenDone = true;
                    Game.Dialog("Pareja", "¡Lo lograste! El anciano en la aldea (norte) querrá saberlo.", 6f);
                    UpdateObjective();
                }
            }

            if (bellRung && !Game.HasBombs && bowGiven) { /* espera a recoger la bolsa */ }
            if (bellRung && Game.HasBombs) UpdateObjective();

            // Red de seguridad: sin flechas ni bombas no se bloquea el avance.
            if (Game.HasBow && Game.Arrows <= 0 && !bellRung) Game.Arrows = 4;
            if (refillCooldown > 0f) refillCooldown -= Time.deltaTime;
            if (caveEntered && !rockBroken && Game.HasBombs && Game.Bombs <= 0 && refillCooldown <= 0f && !HasLoosePickup(PickupKind.Bombs))
            {
                Pickup.Spawn(player.transform.position + player.transform.forward * -2f, PickupKind.Bombs, 3);
                Game.Say("Encuentras unas bombas sobrantes", 3f);
                refillCooldown = 4f;
            }

            // Caída al foso
            if (pitCooldown > 0f) pitCooldown -= Time.deltaTime;
            if (player.transform.position.y < -3f && pitCooldown <= 0f)
            {
                pitCooldown = 1f;
                player.Teleport(PrologueWorld.PitRespawn);
                hp.Damage(1f, player.transform.position);
                Game.Say("Caíste al foso. Usa una bomba contra el interruptor.", 3.5f);
            }

            UpdateAmbient();
            HandleInteraction();
        }

        bool HasLoosePickup(PickupKind kind)
        {
            Pickup[] ps = FindObjectsByType<Pickup>(FindObjectsSortMode.None);
            for (int i = 0; i < ps.Length; i++) if (ps[i].Kind == kind) return true;
            return false;
        }

        void HandleInteraction()
        {
            if (!GameInput.Pressed(Act.Interact) && !GameInput.Pressed(Act.Confirm)) return;
            Interactable it = Interactable.Nearest(player.transform.position);
            if (it != null && it.OnUse != null)
            {
                Sfx.Play("ui");
                it.OnUse();
            }
        }

        void UpdateAmbient()
        {
            // Oscurece el ambiente dentro de la cueva.
            float x = player.transform.position.x;
            float inCave = Mathf.Clamp01((x - PrologueWorld.CaveStartX + 2f) / 6f);
            Color day = new Color(0.62f, 0.64f, 0.72f);
            Color dark = new Color(0.20f, 0.20f, 0.28f);
            RenderSettings.ambientLight = Color.Lerp(day, dark, inCave);
            if (RenderSettings.sun != null) RenderSettings.sun.intensity = Mathf.Lerp(1.15f, 0.15f, inCave);
            Camera cam = Camera.main;
            if (cam != null) cam.backgroundColor = Color.Lerp(new Color(0.62f, 0.82f, 0.95f), new Color(0.06f, 0.06f, 0.09f), inCave);
            RenderSettings.fogColor = Color.Lerp(new Color(0.80f, 0.90f, 0.95f), new Color(0.05f, 0.05f, 0.08f), inCave);
        }

        // ---------- Muerte ----------

        void OnPlayerDied()
        {
            if (Dead) return;
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            Dead = true;
            Game.Say("Te desmayaste...", 2.5f);
            yield return new WaitForSeconds(2.2f);
            player.Teleport(checkpoint);
            player.ClearLock();
            hp.Revive(1f);
            Dead = false;
            if (partner != null && partner.Downed) { /* se recupera sola */ }
            Game.Say("Despiertas junto a tu pareja", 3f);
        }

        // ---------- Objetivos ----------

        public void UpdateObjective()
        {
            string o;
            if (!talkedPartner && !gearTaken) o = "Habla con tu pareja (E)";
            else if (!gearTaken) o = "Toma la espada y el escudo del perchero";
            else if (!gardenDone) o = gardenStarted ? "Derrota a los slimes del huerto (" + (3 - gardenSlimes.FindAll(s => s != null).Count) + "/3)" : "Protege el huerto (al oeste de la casa)";
            else if (!bowGiven) o = "Habla con el anciano en la aldea (norte)";
            else if (!bellRung) o = "Toca la campana con el arco (este)";
            else if (!Game.HasBombs) o = "Recoge la bolsa de bombas junto a la campana";
            else if (!caveEntered) o = "Ve a las ruinas al este (letrero)";
            else if (!wallBroken) o = "Lanza una bomba (B) al muro agrietado";
            else if (!doorOpened) o = "Lanza una bomba al interruptor, al otro lado del foso";
            else if (!rockBroken) o = ambushed ? "Derrota a los slimes y vuela la roca del fondo" : "Avanza por la puerta";
            else if (!treasureOpened) o = "Abre el cofre (E)";
            else if (!benchUsed) o = "Vuelve a casa y usa la mesa de trabajo";
            else o = "Prólogo completado";
            Game.Objective = o;
        }

        public string CurrentHint()
        {
            if (!gearTaken) return "";
            if (!gardenDone) return "J: atacar   K: escudo (parry justo al golpe)   Tab: fijar   Espacio: esquivar";
            if (bowGiven && !bellRung) return "Mantén F para apuntar, suelta para disparar";
            if (Game.HasBombs && !wallBroken) return "B: lanzar bomba (ten distancia: te hace daño)";
            return "C: ordenar a tu pareja";
        }
    }
}
