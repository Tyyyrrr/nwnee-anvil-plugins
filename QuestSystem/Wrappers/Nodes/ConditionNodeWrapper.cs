using System;
using System.Collections.Generic;
using Anvil.API;
using NLog;
using QuestSystem.Nodes;

namespace QuestSystem.Wrappers.Nodes
{
    internal sealed class ConditionNodeWrapper : NodeWrapper<ConditionNode>
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        public ConditionNodeWrapper(ConditionNode node) : base(node){}

        public Dictionary<NwPlayer, bool> _playerResults = new();
        public override bool IsRoot => false;

        public int NextIDWhenFalse {get;set;} = -1;
        public int NextIDWhenTrue {get;set;} = -1;

        int GetNextID(bool value) => value ? NextIDWhenTrue : NextIDWhenFalse;
        bool GetResult(NwPlayer player)
        {
            foreach(var con in Node.Conditions)
            {
                if(con.Invert ? CheckCondition(player, con) : !CheckCondition(player,con))
                    return false;
            }
            return true;
        }

        static bool CompareInts(int value, int condition, QuestCondition.ComparisonMode comparison) => comparison switch
        {
            QuestCondition.ComparisonMode.Less => value < condition,
            QuestCondition.ComparisonMode.LessOrEqual => value <= condition,
            QuestCondition.ComparisonMode.Greater => value > condition,
            QuestCondition.ComparisonMode.GreaterOrEqual => value >= condition,
            QuestCondition.ComparisonMode.NotEqual => value != condition,
            _ => value == condition
        };
    
        bool CheckCondition(NwPlayer player, QuestCondition condition)
        {
            switch (condition.Type)
            {
                case QuestCondition.ConditionType.SkillRoll:
                    {
                        var skill = NwSkill.FromSkillId(condition.IntParameter);

                        if(skill == null)
                        {
                            _log.Error("Invalid skill ID: " + condition.IntParameter);
                            return false;
                        }

                        var diceRoll = Random.Shared.Next(player.ControlledCreature!.GetSkillRank(skill));

                        return CompareInts(diceRoll,condition.IntCondition, condition.Comparison);
                    }

                case QuestCondition.ConditionType.SkillRank:
                    {
                        var skill = NwSkill.FromSkillId(condition.IntParameter);

                        if(skill == null)
                        {
                            _log.Error("Invalid skill ID: " + condition.IntParameter);
                            return false;
                        }

                        var rank = player.ControlledCreature!.GetSkillRank(skill);

                        return CompareInts(rank,condition.IntCondition, condition.Comparison);
                    }

                case QuestCondition.ConditionType.AttributeRoll:
                    {
                        var diceRoll = Random.Shared.Next(player.ControlledCreature!.GetAbilityScore((Ability)condition.IntParameter));

                        return CompareInts(diceRoll,condition.IntCondition, condition.Comparison);
                    }
                    
                case QuestCondition.ConditionType.AttributeRank:
                    {
                        var rank = player.ControlledCreature!.GetAbilityScore((Ability)condition.IntParameter);

                        return CompareInts(rank,condition.IntCondition, condition.Comparison);
                    }
                    
                case QuestCondition.ConditionType.Level:
                    {
                        var level = player.ControlledCreature!.Level;

                        return CompareInts(level,condition.IntCondition, condition.Comparison);
                    }

                case QuestCondition.ConditionType.ClassLevel:
                    {
                        var c =  NwClass.FromClassId(condition.IntParameter);

                        if(c == null)
                        {
                            _log.Error("Invalid Class ID: " + condition.IntParameter);
                            return false;
                        }

                        var ci = player.ControlledCreature!.GetClassInfo(c);

                        int level = ci?.Level ?? 0;

                        return CompareInts(level,condition.IntCondition, condition.Comparison);
                    }

                case QuestCondition.ConditionType.Race:
                    {
                        var playerRaceId = player.ControlledCreature!.Race.Id;

                        return playerRaceId == condition.IntCondition;
                    }

                case QuestCondition.ConditionType.Subrace:
                    {
                        var playerSubrace = player.ControlledCreature!.SubRace.ToLower();

                        return playerSubrace == condition.StringCondition;
                    }

                case QuestCondition.ConditionType.AlignmentGoodEvil:
                    {
                        var aligmnent = player.ControlledCreature!.GoodEvilValue;

                        return CompareInts(aligmnent, condition.IntCondition, condition.Comparison);
                    }
                    
                case QuestCondition.ConditionType.AlignmentLawChaos:
                    {
                        var aligmnent = player.ControlledCreature!.LawChaosValue;

                        return CompareInts(aligmnent, condition.IntCondition, condition.Comparison);
                    }

                case QuestCondition.ConditionType.OnQuest:
                    {
                        var isOnQuest = QuestManager.PlayerIsOnQuest(player, condition.StringCondition, out var stageId);

                        return isOnQuest && (condition.IntParameter < 0 || stageId == condition.IntParameter);
                    }

                case QuestCondition.ConditionType.CompletedQuest:
                    {
                        var hasCompletedQuest = QuestManager.PlayerHasCompletedQuest(player, condition.StringCondition, out var stageId);

                        return hasCompletedQuest && (condition.IntParameter < 0 || stageId == condition.IntParameter);
                    }

                case QuestCondition.ConditionType.HasItem:
                    {
                        var splitStr = condition.StringCondition.Split(':');
                        var resRef = splitStr[0];
                        var tag = splitStr.Length > 1 ? splitStr[1] : null;
                        int requiredAmount = condition.IntCondition;

                        foreach(var item in player.ControlledCreature!.Inventory.Items)
                        {
                            if(item.ResRef != resRef) continue;
                            if(tag != null && item.Tag != tag) continue;
                            if(item.StackSize < requiredAmount)
                                requiredAmount -= item.StackSize;
                            else requiredAmount = 0;

                            if(requiredAmount <= 0) return true;
                        }
                        return false;
                    }

                case QuestCondition.ConditionType.HasTaggedEffect:
                    {
                        foreach(var e in player.ControlledCreature!.ActiveEffects)
                            if(e.Tag == condition.StringCondition) 
                                return true;
                        return false;
                    }

                default: throw new InvalidOperationException("Unknown condition type");
            }
        }
        
        protected override bool ProtectedEvaluate(NwPlayer player, out int nextId)
        {
            bool playerResult = GetResult(player);

            nextId = playerResult ? GetNextID(playerResult) : -1;

            return playerResult;
        }

        protected override void ProtectedDispose(){}
    }
}