using System.Collections.Generic;
using UnityEngine;

namespace Hearthbound
{
    public enum QuestId { None, GoblinRescue, GoblinHunt, Firewood }

    public class QuestInfo
    {
        public QuestId Id;
        public string Title, Text, RewardText;
        public bool UnlocksCave;
    }

    /// <summary>
    /// Misiones del gremio del pueblo. Solo hay una activa a la vez. Las dos que tienen que ver con los goblins
    /// entregan el emblema del gremio, que es lo que hace ceder el sello de roca de la cueva de la montaña.
    /// </summary>
    public static class Quests
    {
        public const int HuntGoal = 8;
        public const int WoodGoal = 15;

        public static readonly QuestInfo[] All =
        {
            new QuestInfo
            {
                Id = QuestId.GoblinRescue, UnlocksCave = true,
                Title = "Rescate en la cripta",
                Text = "Un aldeano fue capturado por los goblins que viven en una cueva sellada de la montaña. Entra, encuentra su jaula y tráelo de vuelta con vida.",
                RewardText = "80 gemas y 6 bombas"
            },
            new QuestInfo
            {
                Id = QuestId.GoblinHunt, UnlocksCave = true,
                Title = "Plaga de goblins",
                Text = "Los goblins de la cripta saquean las caravanas del camino. El gremio paga por cada banda que se reduzca: derrota a " + HuntGoal + " goblins.",
                RewardText = "60 gemas y 20 flechas"
            },
            new QuestInfo
            {
                Id = QuestId.Firewood, UnlocksCave = false,
                Title = "Leña para el gremio",
                Text = "Se acerca el invierno y no queda leña. Tala árboles con el hacha (T) y entrega " + WoodGoal + " de madera a la maestra del gremio.",
                RewardText = "40 gemas y 5 semillas de árbol"
            }
        };

        public static QuestId Active = QuestId.None;
        public static bool GuildSeal;          // emblema del gremio: abre la cueva
        public static bool EverAccepted;       // ya cogió alguna misión del tablón
        public static int GoblinKills;
        static int huntStart;
        static bool villagerRescued;
        static bool readyNotified;
        static readonly HashSet<QuestId> done = new HashSet<QuestId>();

        public static void Reset()
        {
            Active = QuestId.None;
            GuildSeal = false;
            EverAccepted = false;
            readyNotified = false;
            GoblinKills = 0;
            huntStart = 0;
            villagerRescued = false;
            done.Clear();
        }

        public static QuestInfo Info(QuestId id)
        {
            for (int i = 0; i < All.Length; i++) if (All[i].Id == id) return All[i];
            return null;
        }

        public static bool IsDone(QuestId id) { return done.Contains(id); }

        public static bool IsReady(QuestId id)
        {
            switch (id)
            {
                case QuestId.GoblinRescue: return villagerRescued;
                case QuestId.GoblinHunt: return GoblinKills - huntStart >= HuntGoal;
                case QuestId.Firewood: return Game.Wood >= WoodGoal;
            }
            return false;
        }

        public static string Progress(QuestId id)
        {
            switch (id)
            {
                case QuestId.GoblinRescue: return villagerRescued ? "aldeano a salvo: vuelve al gremio" : "aldeano sin rescatar";
                case QuestId.GoblinHunt: return Mathf.Min(HuntGoal, GoblinKills - huntStart) + "/" + HuntGoal + " goblins";
                case QuestId.Firewood: return Mathf.Min(WoodGoal, Game.Wood) + "/" + WoodGoal + " de madera";
            }
            return "";
        }

        public static string ObjectiveText
        {
            get
            {
                if (Active == QuestId.None) return "";
                QuestInfo q = Info(Active);
                if (IsReady(Active)) return q.Title + " · vuelve al gremio y habla con la maestra";
                switch (Active)
                {
                    case QuestId.GoblinRescue: return q.Title + " · entra en la cueva de la montaña (punto morado) y rescata al aldeano";
                    case QuestId.GoblinHunt: return q.Title + " · derrota goblins en la cueva (" + Progress(Active) + ")";
                    default: return q.Title + " · reúne madera (" + Progress(Active) + ")";
                }
            }
        }

        public static void Accept(QuestId id)
        {
            QuestInfo q = Info(id);
            if (q == null || Active != QuestId.None || done.Contains(id)) return;
            Active = id;
            EverAccepted = true;
            readyNotified = false;
            if (id == QuestId.GoblinHunt) huntStart = GoblinKills;
            Sfx.Play("pickup", 1.1f, 0.8f);
            if (q.UnlocksCave)
            {
                GuildSeal = true;
                Game.Dialog("Maestra del gremio", "Misión aceptada. Toma el emblema del gremio: el sello de la cueva, al pie de la montaña, lo reconoce y se abrirá al acercarte.", 8f);
                Game.Say("Recibes el emblema del gremio", 4f);
            }
            else
            {
                Game.Dialog("Maestra del gremio", "Misión aceptada. Vuelve cuando tengas la madera.", 6f);
            }
        }

        /// <summary>Avisa una sola vez cuando la misión activa ya se puede entregar.</summary>
        public static void Tick()
        {
            if (Active == QuestId.None || readyNotified || !IsReady(Active)) return;
            readyNotified = true;
            Game.Say("Misión lista: vuelve al gremio y habla con la maestra", 5f);
        }

        public static void OnGoblinKilled() { GoblinKills++; }

        public static void OnVillagerRescued()
        {
            villagerRescued = true;
            if (Active == QuestId.GoblinRescue) Game.Say("Misión: vuelve al gremio con la noticia", 5f);
        }

        /// <summary>Hablar con la maestra del gremio: entrega la misión terminada o recuerda la actual.</summary>
        public static void TalkToMaster()
        {
            if (Active == QuestId.None)
            {
                bool all = true;
                for (int i = 0; i < All.Length; i++) if (!done.Contains(All[i].Id)) all = false;
                Game.Dialog("Maestra del gremio", all
                    ? "Has terminado todos los encargos del tablón. El gremio te debe mucho."
                    : "Bienvenido al gremio. Mira el tablón de misiones de la pared del fondo y elige un encargo.", 6f);
                return;
            }
            if (!IsReady(Active))
            {
                Game.Dialog("Maestra del gremio", Info(Active).Title + ": " + Progress(Active) + ". Vuelve cuando lo tengas.", 5f);
                return;
            }
            Complete();
        }

        static void Complete()
        {
            QuestId id = Active;
            QuestInfo q = Info(id);
            Active = QuestId.None;
            done.Add(id);
            switch (id)
            {
                case QuestId.GoblinRescue:
                    Game.Gems += 80;
                    Game.HasBombs = true;
                    Game.Bombs = Mathf.Min(20, Game.Bombs + 6);
                    break;
                case QuestId.GoblinHunt:
                    Game.Gems += 60;
                    Game.Arrows = Mathf.Min(30, Game.Arrows + 20);
                    break;
                case QuestId.Firewood:
                    Game.Wood -= WoodGoal;
                    Game.Gems += 40;
                    Game.Seeds += 5;
                    break;
            }
            Sfx.Play("heal", 1f, 0.8f);
            Game.Dialog("Maestra del gremio", "¡Misión cumplida: " + q.Title + "! Recompensa: " + q.RewardText + ".", 7f);
            Game.Say("Misión completada: " + q.Title, 5f);
        }
    }
}
