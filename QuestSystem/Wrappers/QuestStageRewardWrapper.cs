using System;
using Anvil.API;

namespace QuestSystem.Wrappers
{
    internal sealed class QuestStageRewardWrapper
    {
        public readonly QuestStageReward Reward;
        public QuestStageRewardWrapper(QuestStageReward reward) { Reward = reward; }

        public void GrantReward(NwPlayer player)
        {
            throw new NotImplementedException();
        }
    }
}