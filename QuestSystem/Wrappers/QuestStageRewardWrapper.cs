using System;
using System.Threading.Tasks;

using Anvil.API;
using NLog;


namespace QuestSystem.Wrappers
{
    internal sealed class QuestStageRewardWrapper
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        public readonly QuestStageReward Reward;
        public QuestStageRewardWrapper(QuestStageReward reward) { Reward = reward; }

        public void GrantReward(NwPlayer player)
        {
            _log.Info("Granting reward...");
            _ = NwTask.Run(async () =>
            {
                try
                {
                    if (!await GrantRewardAsync(player))
                    {
                        _log.Warn("Failed to grant reward");
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Exception while granting reward");
                }
            });
        }

        private async Task<bool> GrantRewardAsync(NwPlayer player)
        {
            await NwTask.Delay(TimeSpan.FromSeconds(0.7f));

            await NwTask.SwitchToMainThread();

            if(!player.IsValid || player.ControlledCreature is not NwCreature pc || !pc.IsValid) 
                return false;

            await pc.WaitForObjectContext();

            if (Reward.Items.Count > 0)
            {
                int count = 0;
                var createdItems = new NwItem[Reward.Items.Count];
                foreach (var kvp in Reward.Items)
                {
                    var item = await NwItem.Create(kvp.Key, pc);

                    if(item == null) break;
                    
                    createdItems[count] = item;

                    count++;
                }

                if(count != Reward.Items.Count) // if failed to create ANY item, destroy all items granted, and skip the reward
                {
                    foreach(var item in createdItems)
                    {
                        await item.WaitForObjectContext();
                        item.IsDestroyable = true;
                        item.Destroy();
                    }

                    return false;
                }
            }

            pc.Xp += Math.Max(0,Reward.Xp);

            pc.GiveGold(Reward.Gold, Reward.NotifyPlayer);

            var alginmentChange = ClampAlignmentChange(pc.GoodEvilValue, Reward.GoodEvilChange);

            if(alginmentChange != 0)
            {
                pc.GoodEvilValue += alginmentChange;

                if(Reward.NotifyPlayer) 
                    player.SendServerMessage($"Twój charakter zbliża się o {Math.Abs(alginmentChange)} w stronę {(alginmentChange < 0 ? "złego" : "dobrego")}");
            }

            alginmentChange = ClampAlignmentChange(pc.LawChaosValue, Reward.LawChaosChange);

            if(alginmentChange != 0)
            {
                pc.GoodEvilValue += alginmentChange;

                if(Reward.NotifyPlayer) 
                    player.SendServerMessage($"Twój charakter zbliża się o {Math.Abs(alginmentChange)} w stronę {(alginmentChange < 0 ? "chaotycznego":"praworządnego")}");
            }

            return true;
        }

        private static int ClampAlignmentChange(int currentValue, int change)
        {
            if(change == 0) return 0;
            else if(change < 0) return currentValue < -change ? -currentValue : change;
            else return currentValue + change > 100 ? 100 - currentValue : change;
        }
    }
}