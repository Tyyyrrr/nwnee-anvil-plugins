using System;
using System.Collections.Generic;

using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

using EasyConfig;
using CharactersRegistry;

using NWN.Core;

using MovementSystem.Configuration;
using System.Linq;
using ExtensionsPlugin;


namespace MovementSystem
{
    [ServiceBinding(typeof(MovementService))]
    public sealed class MovementService
    {
        /// <summary>
        /// Surface materials used by this system
        /// </summary>
        public enum SurfaceMaterial
        {
            Dirt = 1,
            Grass = 3,
            Leaves = 14,
            Water = 6,
            Puddles = 11,
            Swamp = 12,
            Mud = 13,
            Snow = 19,
            Sand = 20
        }

        public const float AberrationMovementRateBonusForNaturalTerrain = 0.1f;

        const string LOCVAR_PARAM = "MvtSysCSBridgeParameter";
        const string LOCVAR_RESULT = "MvtSysCSBridgeResult";

        const int PARAM_PRINT = 1;
        const int PARAM_CRAWL = 2;
        const int PARAM_HORSE = 3;
        const int PARAM_MOUNTING = 4;
        const int PARAM_DISMOUNTING = 5;
        const int PARAM_SURF_MAT_CHANGE = 6;
        const int PARAM_FLY = 7;

        private enum BridgeParam
        {
            Print = PARAM_PRINT,
            Crawl = PARAM_CRAWL,
            Horse = PARAM_HORSE,
            Mounting = PARAM_MOUNTING,
            Dismounting = PARAM_DISMOUNTING,
            SurfMatChange = PARAM_SURF_MAT_CHANGE,
            FlyToggle = PARAM_FLY
        }


        private readonly GeneralConfig _generalConfig;
        private readonly SurfaceConfig _surfaceConfig;
        private readonly MountConfig _mountConfig;
        private readonly FeatsConfig _featConfig;

        private readonly VirtualMachine _vm;

        private readonly CharactersRegistryService _registry;

        internal IEnumerable<int> MovementAffectingActiveFeats => _featConfig.MovementAffectingActiveFeats;
        internal IEnumerable<int> MovementAffectingPassiveFeats => _featConfig.MovementAffectingPassiveFeats;
        internal int RideSkillID => _mountConfig.RideSkillID;

        internal float MaxSpeed => _generalConfig.MaxSpeed;
        internal float MaxCrawlingSpeed => _generalConfig.CrawlingMaxSpeed;
        internal float DefaultCrawlingSpeed => _generalConfig.CrawlingDefaultSpeed;

        public MovementService(
            ConfigurationService easyCfg, 
            CharactersRegistryService registry,
            EventService eventService
        )
        {
            _generalConfig = easyCfg.GetConfig<GeneralConfig>();
            _surfaceConfig = easyCfg.GetConfig<SurfaceConfig>();
            _mountConfig = easyCfg.GetConfig<MountConfig>();
            _featConfig = easyCfg.GetConfig<FeatsConfig>();

            _vm = Anvil.AnvilCore.GetService<VirtualMachine>() ?? throw new InvalidOperationException(nameof(VirtualMachine) + " core service not initialized.");

            _registry = registry;

            NwModule.Instance.OnClientEnter += OnClientEnter;
            NwModule.Instance.OnPlayerRest += OnPlayerRest;

            foreach(var area in NwModule.Instance.Areas)
                area.OnEnter += OnAreaEnter;

            MovementState.EventService = eventService;
            MovementState.MovementService = this;
        }


        [ScriptHandler("mvtsys_csbridge")]
        ScriptHandleResult MvtSysCSBridgeHandler(CallInfo info)
        {
            var pc = info.ObjectSelf as NwCreature;

            if(pc == null || !pc.IsValid || !MovementState.TryGetState(pc, out var ms)){
                NLog.LogManager.GetCurrentClassLogger().Error((pc == null || !pc.IsValid) ? "PC is invalid." : "No MovementState object on PC.");
                return ScriptHandleResult.NotHandled;
            }
            

            var param = pc.GetObjectVariable<LocalVariableInt>(LOCVAR_PARAM);

            if(!param.HasValue) 
            {
                NLog.LogManager.GetCurrentClassLogger().Error("Parameter is not set." + param.Value.ToString());
                return ScriptHandleResult.NotHandled;
            }

            var val = param.Value;
            param.Delete();

            if(!Enum.IsDefined(typeof(BridgeParam), val))
            {
                NLog.LogManager.GetCurrentClassLogger().Error("Invalid parameter value: " + param.Value.ToString());
                return ScriptHandleResult.NotHandled;
            }

            switch ((BridgeParam)val)
            {
                case BridgeParam.Print:
                    ms.PrintSpeed();
                    break;
                    
                case BridgeParam.Crawl:
                    ms.RefreshCrawling();
                    break;

                case BridgeParam.Mounting:
                    ms.MountHorseAsync();
                    break;

                case BridgeParam.Dismounting:
                    ms.DismountHorse();
                    break;                
                    
                case BridgeParam.SurfMatChange:
                    ms.OnSurfaceMaterialChanged();
                    break;

                case BridgeParam.FlyToggle:
                    ms.RefreshFlying();
                    break;

                default: return ScriptHandleResult.NotHandled;
            }

            return ScriptHandleResult.Handled;
        }

        internal static int GetSurfaceMaterial(NwCreature creature) => NWScript.GetSurfaceMaterial(NWScript.GetLocation(creature.ObjectId)); 

        internal float GetSurfaceSpeedModifierForCreature(int surface, NwCreature creature)
        {
            if(NWScript.GetHasFeat(ServerData.DataProviders.CustomFeatsMap.WildAgility) > 0 || creature.IsFlying()/* || !creature.HasLegs()*/) return 0f;

            float surfSpeedMod = _surfaceConfig.GetSpeedModifierForSurface(surface);

            switch (surface)
            {
                case (int)SurfaceMaterial.Dirt:
                case (int)SurfaceMaterial.Grass:
                case (int)SurfaceMaterial.Leaves:
                    {
                        if(creature.IsSatyr())
                            surfSpeedMod = AberrationMovementRateBonusForNaturalTerrain;
                    }
                    break;

                case (int)SurfaceMaterial.Puddles:
                case (int)SurfaceMaterial.Water:
                    {
                        if(creature.IsWaterNymph())
                            surfSpeedMod = 0f;
                    }
                    break;

                case (int)SurfaceMaterial.Snow:
                    {
                        if(creature.IsVanir())
                            surfSpeedMod = 0f;
                        else if(creature.IsSvart())
                            surfSpeedMod = 0.05f;
                        else if(creature.IsDesertElf() || creature.IsArabian())
                            surfSpeedMod -= 0.05f;
                    }
                    break;

                case (int)SurfaceMaterial.Sand:
                    {
                        if(creature.IsDesertElf() || creature.IsArabian())
                            surfSpeedMod = 0.05f;
                        else if(creature.IsSvart())
                            surfSpeedMod -= 0.05f;
                    }
                    break;

                default: break;
            }

            return surfSpeedMod;
        }


        internal string GetSurfaceMaterialName(int surface) => _surfaceConfig.GetName(surface);


        internal string GetMountedHorse(NwCreature pc)
        {
            NWScript.SetLocalInt(pc.ObjectId, LOCVAR_PARAM, (int)BridgeParam.Horse);

            _vm.Execute("mvtsys_csbridge",pc,(LOCVAR_PARAM, "horse"));

            var result = pc.GetObjectVariable<LocalVariableString>(LOCVAR_RESULT);

            var val = result.HasValue ? result.Value : string.Empty;

            result.Delete();

            return string.IsNullOrEmpty(val) ? string.Empty : val;
        }

        internal float GetHorseSpeed(string horseResRef) => _mountConfig.Mounts.TryGetValue(horseResRef, out var data) ? data.Speed : 1f;

        internal float GetHorseSurfaceMaterialModifier(string horseResRef, int surface)
        {
            var modifier = _surfaceConfig.GetSpeedModifierForSurface(surface);

            if(!_mountConfig.Mounts.TryGetValue(horseResRef, out var data)) return modifier;

            var matName = _surfaceConfig.GetMaterialOriginalName(surface);

            if(string.IsNullOrEmpty(matName)) return modifier;

            if(!data.SurfaceMaterialBonuses.TryGetValue(matName, out var bonus))
                return data.IgnoreSurfaceMaterialPenalty.Contains(matName) ? 0f : modifier;

            else if(data.IgnoreSurfaceMaterialPenalty.Contains(matName)) return bonus;
            
            else return modifier + bonus;
        }

        internal float GetHorseEnvironmentModifier(string horseResRef, NwArea area)
        {
            if(!_mountConfig.Mounts.TryGetValue(horseResRef, out var data)) return 0f;

            bool isNight = NwModule.Instance.IsNight;
            bool isUnderground = area.IsUnderGround;

            float bonus = 0f;

            if(data.NightBonus != 0 && isNight && area.IsExterior) bonus += data.NightBonus;
            if(data.DayBonus != 0 && !isNight && area.IsExterior) bonus += data.DayBonus;

            if(data.UndergroundBonus != 0 && isUnderground && area.IsExterior) bonus += data.UndergroundBonus;
            if(data.AboveGroundBonus != 0 && !isUnderground && area.IsExterior) bonus += data.AboveGroundBonus;

            return bonus;
        }

        internal bool IsAberrationTravelEquipment(NwItem item) => _mountConfig.AberrationEQSpeed.ContainsKey(item.ResRef);
        internal float GetAberrationEQSpeedModifier(NwItem item) => _mountConfig.AberrationEQSpeed.TryGetValue(item.ResRef, out var val) ? val : 1f;


        internal static void SetMovementRateFactorCap(NwCreature pc, float cap) => NWN.Core.NWNX.CreaturePlugin.SetMovementRateFactorCap(pc.ObjectId, cap);


        void OnClientEnter(ModuleEvents.OnClientEnter data)
        {
            var player = data.Player;
            if(!_registry.KickPlayerIfCharacterNotRegistered(player, out var pc))
                return;

            MovementState.CreateForPC(pc);
        }

        void OnPlayerRest(ModuleEvents.OnPlayerRest data)
        {
            var player = data.Player;
            var pc = player.ControlledCreature;
            if(pc == null || !pc.IsValid || !MovementState.TryGetState(pc, out var ms))
                return;

            ms.Refresh();
        }


        static void OnAreaEnter(AreaEvents.OnEnter data)
        {
            var obj = data.EnteringObject;

            if(obj is not NwCreature pc || !pc.IsValid || !pc.IsLoginPlayerCharacter(out _) || !MovementState.TryGetState(pc, out var ms))
                return;

            NwTask.Run(async()=>{await NwTask.Delay(TimeSpan.FromSeconds(0.6f)); ms.Refresh();});
        }


        internal float GetFeatSpeedModifier(int featId) => _featConfig.GetFeatSpeedModifier(featId);
        internal string? GetFeatDisplayName(int featId) => _featConfig.GetFeatName(featId);
    }
}