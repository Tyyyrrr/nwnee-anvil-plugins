using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Anvil.API;

using NLog;

using QuestSystem.Nodes;


namespace QuestSystem.Wrappers.Nodes
{
    internal sealed class RewardNodeWrapper : NodeWrapper<RewardNode>
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        public RewardNodeWrapper(RewardNode reward) : base(reward){}

        public override bool IsRoot => false;

        public void GiveReward(NwPlayer player)
        {
            if(Node.IsEmpty) return;

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

            if (Node.Items.Count > 0)
            {
                int count = 0;
                var createdItems = new NwItem[Node.Items.Count];
                foreach (var kvp in Node.Items)
                {
                    var item = await NwItem.Create(kvp.Key, pc);

                    if(item == null) break;

                    createdItems[count] = item;

                    count++;
                }

                await pc.WaitForObjectContext();

                if(count != Node.Items.Count) // if failed to create ANY item, destroy all items granted, and skip the reward
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

            pc.Xp += Math.Max(0,Node.Xp);
            pc.GiveGold(Node.Gold, Node.NotifyPlayer);
            pc.GoodEvilValue += ClampAlignmentChange(pc.GoodEvilValue, Node.GoodEvilChange);
            pc.LawChaosValue += ClampAlignmentChange(pc.LawChaosValue, Node.LawChaosChange);

            return true;
        }

        private static int ClampAlignmentChange(int currentValue, int change)
        {
            if(change == 0) return 0;
            else if(change < 0) return currentValue < -change ? -currentValue : change;
            else return currentValue + change > 100 ? 100 - currentValue : change;
        }



        protected override bool ProtectedEvaluate(NwPlayer player, out int nextId)
        {
            if(_rewarderPlayers.Add(player))
                GiveReward(player);
            nextId = NextID;
            return true;
        }



        private readonly HashSet<NwPlayer> _rewarderPlayers = new();
        public override void Reset(NwPlayer player) => _rewarderPlayers.Remove(player);

        protected override void ProtectedDispose()
        {
            //todo: cancel async task, or make it safe if not canceled
            _rewarderPlayers.Clear();
        }
    }
}