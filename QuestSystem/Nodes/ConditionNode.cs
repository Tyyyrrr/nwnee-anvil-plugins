using System;
using QuestSystem.Wrappers.Nodes;

namespace QuestSystem.Nodes
{
    public class QuestCondition : ICloneable
    {        
        public enum ConditionType
        {
            SkillRoll,
            SkillRank,
            AttributeRoll,
            AttributeRank,
            Level,
            ClassLevel,
            Race,
            Subrace,
            AlignmentGoodEvil,
            AlignmentLawChaos,
            OnQuest,
            CompletedQuest,
            HasItem,
            HasTaggedEffect
        }        
        
        public enum ComparisonMode
        {
            Equal,
            NotEqual,
            Less,
            Greater,
            LessOrEqual,
            GreaterOrEqual
        }

        public string StringCondition {get;set;} = string.Empty;

        public int IntParameter {get;set;}
        public int IntCondition {get;set;}

        public ConditionType Type {get;set;} = default;
        public ComparisonMode Comparison {get;set;} = default;
        public bool Invert {get;set;}

        public object Clone()
        {
            return new QuestCondition()
            {
                StringCondition = (string)this.StringCondition.Clone(),

                IntParameter = this.IntParameter,
                IntCondition = this.IntCondition,

                Type = this.Type,
                Comparison = this.Comparison,
                Invert = this.Invert
            };
        }
    }

    public class ConditionNode : NodeBase
    {
        public QuestCondition[] Conditions{get;set;} = Array.Empty<QuestCondition>();

        public int NextIDWhenTrue { get; set; } = -1;
        public int NextIDWhenFalse { get; set; } = -1;

        public override object Clone()
        {
            var arr = new QuestCondition[Conditions.Length];
            for(int i = 0; i < arr.Length; i++)
            {
                var c = Conditions[i];
                arr[i] = (QuestCondition)c.Clone();
            }

            return new ConditionNode() 
            {
                ID = base.ID,
                NextID = base.NextID,
                Rollback = this.Rollback,

                Conditions = arr,
                NextIDWhenTrue = this.NextIDWhenTrue,
                NextIDWhenFalse = this.NextIDWhenFalse,
            };
        }

        internal override ConditionNodeWrapper Wrap() => new(this);
    }
}