using System;
using EasyConfig;

namespace MovementSystem.Configuration
{
    [ConfigFile("General")]
    public sealed class GeneralConfig : IConfig
    {
        public float MaxSpeed {get;set;} = 2f;
        public float CrawlingMaxSpeed {get;set;} = 0.75f;
        public float CrawlingDefaultSpeed {get;set;} = 0.5f;
        public void Coerce() 
        { 
            MaxSpeed = Math.Max(1f,MaxSpeed);
            CrawlingDefaultSpeed = Math.Max(0,CrawlingDefaultSpeed);
            CrawlingMaxSpeed = Math.Max(CrawlingDefaultSpeed,CrawlingMaxSpeed);
        }
        public bool IsValid(out string? error) {error = null;return true;}
    }
}