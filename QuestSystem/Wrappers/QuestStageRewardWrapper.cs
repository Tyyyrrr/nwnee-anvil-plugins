using System;
using System.Threading.Tasks;

using Anvil.API;
using NLog;


namespace QuestSystem.Wrappers
{
    internal sealed class QuestStageRewardWrapper : BaseWrapper
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        public readonly QuestStageReward Reward;
        public QuestStageRewardWrapper(QuestStageReward reward) { Reward = reward; }

        public void GiveReward(NwPlayer player)
        {
            if(Reward.IsEmpty) return;

            _log.Info($"Giving quest stage reward");

            _ = NwTask.Run(async () =>
            {
                try
                {
                    if (!await GiveRewardAsync(player))
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
        
        private async Task<bool> GiveRewardAsync(NwPlayer player)
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

                await pc.WaitForObjectContext();

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

            await pc.WaitForObjectContext();

            pc.Xp += Math.Max(0,Reward.Xp);
            pc.GiveGold(Reward.Gold, Reward.NotifyPlayer);
            pc.GoodEvilValue += ClampAlignmentChange(pc.GoodEvilValue, Reward.GoodEvilChange);
            pc.LawChaosValue += ClampAlignmentChange(pc.LawChaosValue, Reward.LawChaosChange);

            return true;
        }

        private static int ClampAlignmentChange(int currentValue, int change)
        {
            if(change == 0) return 0;
            else if(change < 0) return currentValue < -change ? -currentValue : change;
            else return currentValue + change > 100 ? 100 - currentValue : change;
        }

        public override void Dispose()
        {
            base.Dispose();
            //todo: cancel async task, or make it safe if not canceled
        }
    }
}