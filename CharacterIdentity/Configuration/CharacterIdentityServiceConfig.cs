using System;
using System.Collections.Generic;

using Anvil.API;

using EasyConfig;



namespace CharacterIdentity.Configuration
{
    [ConfigFile("Service")]
    public sealed class CharacterIdentityServiceConfig : IConfig
    {
        private const int CharacterIdentityUserEventOffset = 3230910;
    
        public int SenseMotiveSkillID { get; set; } = -1;

        public int MaximumFalseIdentities { get; set; } = 4;
        public int BluffRanksPerIdentity { get; set; } = 10;

        public bool AddPerformRanksToDC { get; set; } = false;
        public int PerformRanksPerBonusPointDC { get; set; } = 5;
        public int OnIdentityNuiOpenUserEventNumber { get; set; } = CharacterIdentityUserEventOffset + 1;
        public int OnAcquaintancesChangedUserEventNumber { get; set; } = CharacterIdentityUserEventOffset + 2;
        public int OnHeadSlotVisibilityChangedUserEventNumber { get; set; } = CharacterIdentityUserEventOffset + 3;
        public int OnCharacterSheetUpdate {get;set;} = CharacterIdentityUserEventOffset + 4;
        
        public Dictionary<string, int> RequiredClassLevels { get; set; } = new()
        {
            {nameof(ClassType.Rogue), 5},
            {nameof(ClassType.Bard), 5},
            {nameof(ClassType.Shadowdancer), 5},
            {nameof(ClassType.Assassin), 5},
            // todo: add custom class IDs (in config)
        };

        public void Coerce()
        {
            if (SenseMotiveSkillID < 0)
            {
                var skill = NwSkill.FromSkillId(0) ?? throw new InvalidOperationException("Skill id 0 is invalid");

                NLog.LogManager.GetCurrentClassLogger()
                .Warn($"{nameof(SenseMotiveSkillID)} has been set to invalid value ({SenseMotiveSkillID}). It will be replaced to default skill ({skill.Name}) to make things work. Make sure to edit the configuration file and provide a valid ID");
                
                SenseMotiveSkillID = 0;
            }
        }
        public bool IsValid(out string? error)
        {
            error = "";

            if (NwSkill.FromSkillId(SenseMotiveSkillID) == null)
                error += $"{nameof(SenseMotiveSkillID)} must be set to the valid skill ID";

            if (MaximumFalseIdentities < 0 || MaximumFalseIdentities > 10)
                error += $"{nameof(MaximumFalseIdentities)} out of inclusive range 0-10\n";

            if (BluffRanksPerIdentity < 1 || BluffRanksPerIdentity > 100)
                error += $"{nameof(BluffRanksPerIdentity)} out of inclusive range 1-100\n";

            if (PerformRanksPerBonusPointDC < 1 || PerformRanksPerBonusPointDC > 100)
                error += $"{nameof(PerformRanksPerBonusPointDC)} out of inclusive range 1-100\n";

            foreach (var kvp in RequiredClassLevels)
            {
                NwClass? nwClass;

                if (Enum.TryParse(kvp.Key, out ClassType classType))
                {
                    nwClass = NwClass.FromClassType(classType);
                    if (nwClass == null)
                    {
                        error += $"Invalid ClassType \'{kvp.Key}\'\n";
                        continue;
                    }
                }
                else if (int.TryParse(kvp.Key, out int classId))
                {
                    nwClass = NwClass.FromClassId(classId);
                    if (nwClass == null)
                    {
                        error += $"Invalid ClassId \'{kvp.Key}\'\n";
                        continue;
                    }
                }
                else
                {
                    error += $"ClassId or ClassType \'{kvp.Key}\' is invalid\n";
                    continue;
                }

                if (kvp.Value < 1 || kvp.Value > 40)
                {
                    error += $"{nwClass.Name} in {nameof(RequiredClassLevels)} out of inclusive range 1-40\n";
                }
            }

            error = error == string.Empty ? null : error;
            return error == null;
        }
    }

}