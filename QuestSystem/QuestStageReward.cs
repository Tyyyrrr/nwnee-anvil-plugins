using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Anvil.API;

namespace QuestSystem
{
    public sealed class QuestStageReward
    {

        public static string? Serialize(QuestStageReward questStageReward) => QuestSerializer.Serialize(questStageReward);
        public static QuestStageReward? Deserialize(string json) => QuestSerializer.Deserialize<QuestStageReward>(json);

        public bool NotifyPlayer {get;set;}=true;
        public int Xp {get;set;}
        public int Gold {get;set;}
        public int GoodEvilChange {get;set;}
        public int LawChaosChange {get;set;}
        public Dictionary<string, int> Items {get;set;} = new();
        public Dictionary<string, bool> ObjectVisibility {get;set;} = new();

        public async ValueTask GrantReward(NwCreature creature)
        {
            creature.Xp += Math.Max(0,Xp);

            creature.GiveGold(Math.Max(0,Gold));

            creature.GoodEvilValue = Math.Clamp(creature.GoodEvilValue + GoodEvilChange,0,100);
            creature.LawChaosValue = Math.Clamp(creature.LawChaosValue + LawChaosChange,0,100);

            if(Items.Count == 0) return;

            foreach(var kvp in Items)
            {
                _ = await NwItem.Create(kvp.Key,creature);
            }
        }
    }
}