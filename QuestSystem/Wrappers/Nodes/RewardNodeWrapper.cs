using System;
using System.Collections.Generic;
using System.Linq;
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

            var node = (RewardNode)Node.Clone();

            await NwTask.SwitchToMainThread();

            if(!player.IsValid || player.ControlledCreature is not NwCreature pc || !pc.IsValid)
                return false;

            await pc.WaitForObjectContext();

            if (node.Items.Count > 0)
            {
                int count = 0;
                var createdItems = new NwItem[node.Items.Count];
                foreach (var kvp in node.Items)
                {
                    var splitKey = kvp.Key.Split(':');
                    var resRef = splitKey[0];
                    var tag = splitKey.Length > 1 ? splitKey[1] : null;

                    var item = await NwItem.Create(kvp.Key, pc);

                    if(item == null) break;

                    if(tag != null) item.Tag = tag;

                    createdItems[count] = item;

                    count++;
                }

                await pc.WaitForObjectContext();

                if(count != node.Items.Count) // if failed to create ANY item, destroy all items granted, and skip the reward
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

            pc.Xp += Math.Max(0,node.Xp);
            pc.GiveGold(node.Gold, node.NotifyPlayer);
            pc.GoodEvilValue += ClampAlignmentChange(pc.GoodEvilValue, node.GoodEvilChange);
            pc.LawChaosValue += ClampAlignmentChange(pc.LawChaosValue, node.LawChaosChange);

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