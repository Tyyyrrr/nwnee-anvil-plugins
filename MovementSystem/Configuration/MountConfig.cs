using System;
using System.Collections.Generic;
using EasyConfig;

namespace MovementSystem.Configuration
{
    [ConfigFile("Mount")]
    public sealed class MountConfig : IConfig
    {

        public sealed class MountData
        {
            public float Speed {get;set;} = 1f;
            public string[] IgnoreSurfaceMaterialPenalty {get;set;} = Array.Empty<string>();
            public Dictionary<string, float> SurfaceMaterialBonuses {get;set;} = new();
            public float NightBonus {get;set;} = 0f;
            public float DayBonus {get;set;} = 0f;
            public float UndergroundBonus {get;set;} = 0f;
            public float AboveGroundBonus {get;set;} = 0f;
        }

        public int RideSkillID {get;set;} = 27;
        public Dictionary<string, float> AberrationEQSpeed {get;set;} = new(){{"itemResRef", 1.25f}};
        public Dictionary<string, MountData> Mounts {get;set;} = new(){{"mountResRef", new(){Speed = 2.5f, IgnoreSurfaceMaterialPenalty = new string[]{"Mud"}}}};

        public MountConfig(){}

        public void Coerce() { }

        public bool IsValid(out string? error) 
        {
            error = null;
            return true;
        }
        
    }
}