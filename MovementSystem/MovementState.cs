using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using ExtensionsPlugin;
using NWN.Core;

namespace MovementSystem
{
    internal sealed class MovementState : IDisposable
    {
        private static readonly Dictionary<NwCreature, MovementState> _states = new();


        private static MovementService? _movementService;
        public static MovementService MovementService
        {
            private get => _movementService ?? throw new InvalidOperationException("MovementService not set for MovementState");
            set
            {
                if(_movementService != null) throw new InvalidOperationException("MovementService not set for MovementState");
                else _movementService = value;
            }
        }
        private static EventService? _eventService;
        public static EventService EventService 
        {
            private get => _eventService ?? throw new InvalidOperationException("EventService not set for MovementState"); 
            set 
            {
                if(_eventService != null) throw new InvalidOperationException("EventService already set for MovementState");
                else _eventService = value;
            }
        }

        private int surface = -1;
        private int ridingSkill = 0;
        private string horse = string.Empty;
        private bool stealth = false;
        private bool flying = false;
        private bool crawling = false;
        private bool hasArmor = false;
        private bool hasShield = false;

        private float aberrationTravelEQModifier = 0f;


        [Flags] private enum FeatFlags : ushort
        {
            None = 0,
            // active feats
            BlindingSpeed = 1 << 0,
            FencerAlle = 1 << 1,
            MoonhuntBldCall = 1 << 2,
            EpicSpellFleetnessOfFoot = 1 << 3,
            SteadfastDefender = 1 << 4,

            // passive feats
            Gadabout = 1 << 5,
            LivelyStep = 1 << 6,
            RogueFastFoot = 1 << 7,
            LightingShadow = 1 << 8,
            ImprovedInitiative = 1 << 9,
            SuperiorInitiative = 1 << 10,
            TravelForm = 1 << 11
        }

        static FeatFlags FlagFromFeatId(int featId)
        {
            var cfmap = ServerData.DataProviders.CustomFeatsMap;
            if(featId == cfmap.FencerForth) return FeatFlags.FencerAlle;
            if(featId == cfmap.MoonhunterBloodCall) return FeatFlags.MoonhuntBldCall;
            if(featId == cfmap.EpicSpellFleetnessOfFoot) return FeatFlags.EpicSpellFleetnessOfFoot;
            if(featId == cfmap.SteadfastDefender) return FeatFlags.SteadfastDefender;
            if(featId == cfmap.Gadabout) return FeatFlags.Gadabout;
            if(featId == cfmap.LivelyStep) return FeatFlags.LivelyStep;
            if(featId == cfmap.RogueFastFoot) return FeatFlags.RogueFastFoot;
            if(featId == cfmap.LightingShadow) return FeatFlags.LightingShadow;
            if(featId == cfmap.TravelForm) return FeatFlags.TravelForm;

            return featId switch
            {
                NWScript.FEAT_EPIC_BLINDING_SPEED => FeatFlags.BlindingSpeed,
                NWScript.FEAT_IMPROVED_INITIATIVE => FeatFlags.ImprovedInitiative,
                NWScript.FEAT_EPIC_SUPERIOR_INITIATIVE => FeatFlags.SuperiorInitiative,
                _=>FeatFlags.None
            };
        }

        private static int FeatIDFromFlag(FeatFlags flag) => flag switch
        {
            FeatFlags.BlindingSpeed => NWScript.FEAT_EPIC_BLINDING_SPEED,
            FeatFlags.EpicSpellFleetnessOfFoot => ServerData.DataProviders.CustomFeatsMap.EpicSpellFleetnessOfFoot,
            FeatFlags.FencerAlle => ServerData.DataProviders.CustomFeatsMap.FencerForth,
            FeatFlags.Gadabout => ServerData.DataProviders.CustomFeatsMap.Gadabout,
            FeatFlags.ImprovedInitiative => NWScript.FEAT_IMPROVED_INITIATIVE,
            FeatFlags.LightingShadow => ServerData.DataProviders.CustomFeatsMap.LightingShadow,
            FeatFlags.LivelyStep => ServerData.DataProviders.CustomFeatsMap.LivelyStep,
            FeatFlags.MoonhuntBldCall => ServerData.DataProviders.CustomFeatsMap.MoonhunterBloodCall,
            FeatFlags.RogueFastFoot => ServerData.DataProviders.CustomFeatsMap.RogueFastFoot,
            FeatFlags.SteadfastDefender => ServerData.DataProviders.CustomFeatsMap.SteadfastDefender,
            FeatFlags.SuperiorInitiative => NWScript.FEAT_EPIC_SUPERIOR_INITIATIVE,
            FeatFlags.TravelForm => ServerData.DataProviders.CustomFeatsMap.TravelForm,
            _=>0
        };        
        
        private bool IsFeatSuppressed(FeatFlags flag) => flag switch
        {
            FeatFlags.RogueFastFoot => !stealth || HasActiveEffect(EffectKeys.Haste),
            FeatFlags.LightingShadow => !stealth,
            FeatFlags.LivelyStep or
            FeatFlags.ImprovedInitiative or
            FeatFlags.SuperiorInitiative => hasArmor || hasShield,
            _ => false,
        };

        private enum EffectKeys
        {
            // positive spells
            Haste,
            ExpeditiousRetreat,

            // negative spells
            Slow,
            BigbyHand,

            // negative effects
            Entangle,
            Paralyze,
            Daze,
            Stun,
            Knockdown,
            Petrify,
            Sleep,
            NegativeSpellEffect,
            COUNT
        }

        static EffectKeys KeyFromEffectType(EffectType effectType) => effectType switch
        {
            EffectType.Haste => EffectKeys.Haste,
            EffectType.Slow => EffectKeys.Slow,
            EffectType.Paralyze => EffectKeys.Paralyze,
            EffectType.Entangle => EffectKeys.Entangle,
            EffectType.Stunned => EffectKeys.Stun,
            EffectType.Dazed => EffectKeys.Daze,
            EffectType.Petrify => EffectKeys.Petrify,
            EffectType.Knockdown => EffectKeys.Knockdown,
            EffectType.Sleep => EffectKeys.Sleep,
            _=>EffectKeys.COUNT
        };

        static EffectKeys KeyFromSpellId(int spellId) => spellId switch
        {
            NWScript.SPELL_HASTE => EffectKeys.Haste,
            NWScript.SPELL_SLOW => EffectKeys.Slow,
            NWScript.SPELL_EXPEDITIOUS_RETREAT => EffectKeys.ExpeditiousRetreat,
            NWScript.SPELL_BIGBYS_CRUSHING_HAND or NWScript.SPELL_BIGBYS_GRASPING_HAND => EffectKeys.BigbyHand,
            _=>EffectKeys.COUNT
        };

        private FeatFlags featFlags = FeatFlags.None;
        private readonly int[] _refCounts = new int[(int)EffectKeys.COUNT];

        private bool HasActiveEffect(EffectKeys key) => key != EffectKeys.COUNT && _refCounts[(int)key] > 0;
        private bool HasFeatFlag(FeatFlags flag) => flag != FeatFlags.None && featFlags.HasFlag(flag);

        void UpdateActiveFeatFlags()
        {
            var objId = _pc.ObjectId;
            foreach(var feat in MovementService.MovementAffectingActiveFeats)
            {
                if(feat == ServerData.DataProviders.CustomFeatsMap.FencerForth || feat == ServerData.DataProviders.CustomFeatsMap.SteadfastDefender)
                    continue; // its resolved during ResolveEffects

                var flag = FlagFromFeatId(feat);
                if(flag == FeatFlags.None) continue;
                if(NWScript.GetHasFeatEffect(feat, objId) > 0)
                    featFlags |= flag;
                else featFlags &= ~flag;
                
            }
        }
        void UpdatePassiveFeatFlags()
        {
            var objId = _pc.ObjectId;
            foreach(var feat in MovementService.MovementAffectingPassiveFeats)
            {
                var flag = FlagFromFeatId(feat);
                if(flag == FeatFlags.None) continue;
                if(NWScript.GetHasFeat(feat, objId) > 0) 
                {
                    featFlags |= flag;

                    if(flag == FeatFlags.SuperiorInitiative)
                        featFlags &= ~FeatFlags.ImprovedInitiative;
                }
                else featFlags &= ~flag;
            }

            if (featFlags.HasFlag(FeatFlags.TravelForm))
            {
                int nApp = NWScript.GetAppearanceType(objId);
                if(!ServerData.DataProviders.CustomFeatsMap.IsTravelFormAppearanceType(nApp))
                    featFlags &= ~FeatFlags.TravelForm;
            }
        }
        void IncrementRefCount(EffectKeys key)
        {
            var index = (int)key;
            if(index >= 0 && index < _refCounts.Length)
            {
                var oldValue = _refCounts[index];
                _refCounts[index] = Math.Max(0, oldValue) + 1;
            }
        }


        public static void CreateForPC(NwCreature pc)
        {
            ClearFromPC(pc);

            var ms = new MovementState(pc);

            _states.Add(pc, ms);
        }

        public static void ClearFromPC(NwCreature pc)
        {
            if(_states.TryGetValue(pc, out var ms))
            {
                ms.Dispose();
                _=_states.Remove(pc);
            }
        }


        public static bool TryGetState(NwCreature pc, [NotNullWhen(true)] out MovementState? ms) => _states.TryGetValue(pc, out ms);


        public void Refresh() => RecalculateNextFrame();

        private void Resolve()
        {
            surface = MovementService.GetSurfaceMaterial(_pc);

            aberrationTravelEQModifier = 0;

            ridingSkill = Math.Min(20,NWScript.GetSkillRank(MovementService.RideSkillID,_pc.ObjectId) / 2);

            horse = MovementService.GetMountedHorse(_pc);

            crawling = _pc.IsCrawling();

            stealth = _pc.StealthModeActive;

            flying =  _pc.IsFlying();// || !_pc.HasLegs();

            if(!string.IsNullOrEmpty(horse) && (stealth || _pc.StealthModeActive))
                NWScript.SetActionMode(_pc.ObjectId, NWScript.ACTION_MODE_STEALTH, 0);
            else stealth = _pc.StealthModeActive;

            hasArmor = false;
            hasShield = false;

            featFlags = FeatFlags.None;
            Array.Fill(_refCounts, 0);

            ResolveItems();
            ResolveEffects();
            UpdateActiveFeatFlags();
            UpdatePassiveFeatFlags();
        }

        private void ResolveItems()
        {
            foreach(var slot in Enum.GetValues<InventorySlot>())
            {
                var item = _pc.GetItemInSlot(slot);

                if(item == null) continue;

                if(slot == InventorySlot.Chest && item.BaseACValue > 0) hasArmor = true;
                if(slot == InventorySlot.LeftHand)
                {
                    var bit = item.BaseItem.ItemType;
                    switch (bit)
                    {
                        case BaseItemType.SmallShield:
                        case BaseItemType.LargeShield:
                        case BaseItemType.TowerShield:
                            hasShield = true;
                            break;
                    }
                }

                if(item.ItemProperties.Any(p=>p.Property.PropertyType == ItemPropertyType.Haste))
                    _refCounts[(int)EffectKeys.Haste]++;

                if(MovementService.IsAberrationTravelEquipment(item))
                    aberrationTravelEQModifier = MovementService.GetAberrationEQSpeedModifier(item);
            }
        }

        float negativeSpellPenalty = 0f;
        private void ResolveEffects()
        {
            negativeSpellPenalty = 0f;
            foreach(var effect in _pc.ActiveEffects)
            {
                var type = effect.EffectType;

                var spell = effect.Spell;
                if(spell != null)
                {
                    if(effect.EffectType == EffectType.MovementSpeedDecrease)
                    {
                        var val = effect.IntParams[0];
                        if(val == 100 || spell.Id == NWScript.SPELL_SLOW){}
                        else
                        {
                            IncrementRefCount(EffectKeys.NegativeSpellEffect);
                            negativeSpellPenalty = Math.Max(negativeSpellPenalty, ((float)val)/100);
                        }
                    }

                    if(spell.Id == ServerData.DataProviders.CustomFeatsMap.FencerAlleSpellID) featFlags |= FeatFlags.FencerAlle;
                    else if(spell.Id == ServerData.DataProviders.CustomFeatsMap.SteadfastDefenderSpellID) featFlags |= FeatFlags.SteadfastDefender;
                    else if(spell.Id == NWScript.SPELL_SLOW)
                    {
                        IncrementRefCount(EffectKeys.Slow);
                    }
                    else{
                        var key = KeyFromSpellId(spell.Id);

                        if(key != EffectKeys.COUNT){
                            IncrementRefCount(key);
                        }
                    }
                }
                else
                {
                    var key = KeyFromEffectType(type);
                    if(key == EffectKeys.COUNT) continue;
                    IncrementRefCount(key);
                }
            }
        }

        private volatile bool reacalculateNextFrame = false;
        private async void RecalculateNextFrame()
        {
            if(reacalculateNextFrame) return;

            reacalculateNextFrame = true;

            await NwTask.NextFrame();

            if(_pc.IsValid)
            {
                Resolve();
                Recalculate();
            }

            reacalculateNextFrame = false;
        }

        private void Recalculate()
        {

            float cap = MovementService.MaxSpeed;
            float speed = 1f;

            bool immobilized = 
            HasActiveEffect(EffectKeys.BigbyHand)
            || HasActiveEffect(EffectKeys.Daze)
            || HasActiveEffect(EffectKeys.Entangle)
            || HasActiveEffect(EffectKeys.Knockdown)
            || HasActiveEffect(EffectKeys.Paralyze)
            || HasActiveEffect(EffectKeys.Petrify)
            || HasActiveEffect(EffectKeys.Sleep)
            || HasActiveEffect(EffectKeys.Stun)
            || HasFeatFlag(FeatFlags.SteadfastDefender);


            if(immobilized)
            {
                _pc.MovementRateFactor = 0;
                return;
            }

            if (!string.IsNullOrEmpty(horse))
            {
                float horseSpeed = MovementService.GetHorseSpeed(horse) + (float)ridingSkill / 100;

                horseSpeed = Math.Min(1.6f, horseSpeed);

                var area = _pc.Area;

                float horseEnvironmentModifier = area != null ? MovementService.GetHorseEnvironmentModifier(horse, area) : 0f;
                float horseSurfaceMaterialModifier = MovementService.GetHorseSurfaceMaterialModifier(horse, surface);

                cap = horseSpeed + (horseEnvironmentModifier > 0 ? horseEnvironmentModifier : 0) + (horseSurfaceMaterialModifier > 0 ? horseSurfaceMaterialModifier : 0);
                speed = cap + (horseEnvironmentModifier < 0 ? horseEnvironmentModifier : 0) + (horseSurfaceMaterialModifier < 0 ? horseSurfaceMaterialModifier : 0);

                MovementService.SetMovementRateFactorCap(_pc, cap);
                _pc.MovementRateFactor = speed;

                return;
            }

            if(aberrationTravelEQModifier != 0f)
            {
                cap += aberrationTravelEQModifier;
                speed += aberrationTravelEQModifier;
            }

            uint pcObj = _pc.ObjectId;

            if (stealth)
            {
                if(HasFeatFlag(FeatFlags.RogueFastFoot) && !HasActiveEffect(EffectKeys.Haste)){
                    
                    var classLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_ROGUE, pcObj);
                    speed += MovementService.GetFeatSpeedModifier(ServerData.DataProviders.CustomFeatsMap.RogueFastFoot) * classLevels;
                }

                if(HasFeatFlag(FeatFlags.LightingShadow)) speed += MovementService.GetFeatSpeedModifier(ServerData.DataProviders.CustomFeatsMap.LightingShadow);
            }

            if(!hasArmor && !hasShield)
            {
                if(HasFeatFlag(FeatFlags.SuperiorInitiative)) speed += MovementService.GetFeatSpeedModifier(NWScript.FEAT_EPIC_SUPERIOR_INITIATIVE);
                else if(HasFeatFlag(FeatFlags.ImprovedInitiative)) speed += MovementService.GetFeatSpeedModifier(NWScript.FEAT_IMPROVED_INITIATIVE);

                if(HasFeatFlag(FeatFlags.LivelyStep))
                {
                    var monkClasses = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_MONK, pcObj) + NWScript.GetLevelByClass(ServerData.DataProviders.CustomClassesMap.Sage, pcObj);
                    var wisMod = NWScript.GetAbilityModifier(NWScript.ABILITY_WISDOM);
                    speed += MovementService.GetFeatSpeedModifier(ServerData.DataProviders.CustomFeatsMap.Gadabout) * Math.Min(wisMod, monkClasses);
                }
            }


            if(HasActiveEffect(EffectKeys.Haste)) speed += 0.5f;

            if(HasActiveEffect(EffectKeys.ExpeditiousRetreat)) speed += 0.1f;

            if(HasActiveEffect(EffectKeys.Slow)) speed -= 0.5f;

            if(HasFeatFlag(FeatFlags.BlindingSpeed)) speed += MovementService.GetFeatSpeedModifier(NWScript.FEAT_EPIC_BLINDING_SPEED);

            if(HasFeatFlag(FeatFlags.FencerAlle)) speed += MovementService.GetFeatSpeedModifier(ServerData.DataProviders.CustomFeatsMap.FencerForth);

            if(HasFeatFlag(FeatFlags.MoonhuntBldCall))
            {
                var rank = NWScript.GetSkillRank(NWScript.SKILL_ANIMAL_EMPATHY, pcObj, 1);
                speed += MovementService.GetFeatSpeedModifier(ServerData.DataProviders.CustomFeatsMap.MoonhunterBloodCall) * rank;
            }
            if(HasFeatFlag(FeatFlags.EpicSpellFleetnessOfFoot)) speed += MovementService.GetFeatSpeedModifier(ServerData.DataProviders.CustomFeatsMap.EpicSpellFleetnessOfFoot);

            if (HasFeatFlag(FeatFlags.Gadabout))
            {
                var barbarianClassLevels = NWScript.GetLevelByClass(NWScript.CLASS_TYPE_BARBARIAN, pcObj);
                speed += MovementService.GetFeatSpeedModifier(ServerData.DataProviders.CustomFeatsMap.Gadabout) * Math.Min(10,barbarianClassLevels);
            }
            
            if(HasFeatFlag(FeatFlags.TravelForm)) speed += MovementService.GetFeatSpeedModifier(ServerData.DataProviders.CustomFeatsMap.TravelForm);

            speed += MovementService.GetSurfaceSpeedModifierForCreature(surface,_pc);

            if (crawling)
            {   
                cap = MovementService.MaxCrawlingSpeed;
                speed = MovementService.DefaultCrawlingSpeed * speed * (stealth ? 0.75f : 1f);
            }

            if(HasActiveEffect(EffectKeys.NegativeSpellEffect))
                speed -= negativeSpellPenalty;

            MovementService.SetMovementRateFactorCap(_pc, cap);
            _pc.MovementRateFactor = speed;
        }

        private readonly NwCreature _pc;

        private MovementState(NwCreature pc)
        {
            _pc = pc;

            SubscribeEvents(pc);            
        }
        private void SubscribeEvents(NwCreature pc)
        {
            if(!pc.IsValid) return;

            EventService.Subscribe<OnItemEquip, OnItemEquip.Factory>(pc, OnItemEquip, EventCallbackType.After);
            EventService.Subscribe<OnItemUnequip, OnItemUnequip.Factory>(pc, OnItemUnequip, EventCallbackType.After);
            EventService.Subscribe<OnEffectApply, OnEffectApply.Factory>(pc, OnEffectApply);
            EventService.Subscribe<OnEffectRemove, OnEffectRemove.Factory>(pc, OnEffectRemove);
            EventService.Subscribe<OnPolymorphApply, OnPolymorphApply.Factory>(pc, OnPolymorphApply, EventCallbackType.After);
            EventService.Subscribe<OnPolymorphRemove, OnPolymorphRemove.Factory>(pc, OnPolymorphRemove, EventCallbackType.After);
            EventService.Subscribe<OnLevelUp, OnLevelUp.Factory>(pc, OnLevelUp, EventCallbackType.After);
            EventService.Subscribe<OnLevelDown, OnLevelDown.Factory>(pc, OnLevelDown, EventCallbackType.After);
            EventService.Subscribe<OnStealthModeUpdate, OnStealthModeUpdate.Factory>(pc, OnStealthModeUpdate, EventCallbackType.After);
            EventService.Subscribe<OnUseFeat, OnUseFeat.Factory>(pc, OnUseFeat, EventCallbackType.After);
        }

        private void UnsubscribeEvents(NwCreature pc)
        {
            if(!pc.IsValid) return;
            
            EventService.Unsubscribe<OnItemEquip, OnItemEquip.Factory>(pc, OnItemEquip, EventCallbackType.After);
            EventService.Unsubscribe<OnItemUnequip, OnItemUnequip.Factory>(pc, OnItemUnequip, EventCallbackType.After);
            EventService.Unsubscribe<OnEffectApply, OnEffectApply.Factory>(pc, OnEffectApply);
            EventService.Unsubscribe<OnEffectRemove, OnEffectRemove.Factory>(pc, OnEffectRemove);
            EventService.Unsubscribe<OnPolymorphApply, OnPolymorphApply.Factory>(pc, OnPolymorphApply, EventCallbackType.After);
            EventService.Unsubscribe<OnPolymorphRemove, OnPolymorphRemove.Factory>(pc, OnPolymorphRemove, EventCallbackType.After);
            EventService.Unsubscribe<OnLevelUp, OnLevelUp.Factory>(pc, OnLevelUp, EventCallbackType.After);
            EventService.Unsubscribe<OnLevelDown, OnLevelDown.Factory>(pc, OnLevelDown, EventCallbackType.After);         
            EventService.Unsubscribe<OnStealthModeUpdate, OnStealthModeUpdate.Factory>(pc, OnStealthModeUpdate, EventCallbackType.After);
            EventService.Unsubscribe<OnUseFeat, OnUseFeat.Factory>(pc, OnUseFeat, EventCallbackType.After);
        }
        public void Dispose()
        {
            UnsubscribeEvents(_pc);
        }

        void OnItemEquip(OnItemEquip data)
        {
            if(data.PreventEquip) return;
            Refresh();
        }        
        
        void OnItemUnequip(OnItemUnequip data)
        {
            if(data.PreventUnequip) return;
            Refresh();
        }


        void OnPolymorphApply(OnPolymorphApply data)
        {
            if(data.PreventPolymorph) return;
            Refresh();
        }

        void OnPolymorphRemove(OnPolymorphRemove data)
        {
            if(data.PreventRemove) return;
            Refresh();
        }

        void OnUseFeat(OnUseFeat data)
        {
            Refresh();
        }

        void OnEffectApply(OnEffectApply data)
        {
            if(data.PreventApply) return;
            Refresh();
        }


        void OnEffectRemove(OnEffectRemove data)
        {          
            if(data.PreventRemove) return;
            Refresh();
        }

        void OnLevelUp(OnLevelUp data) => Refresh();
        void OnLevelDown(OnLevelDown data) => Refresh();


        void OnStealthModeUpdate(OnStealthModeUpdate data)
        {
            bool enter = data.EventType == ToggleModeEventType.Enter;

            if(!enter && data.PreventExit) return;

            stealth = enter;

            Refresh();
        }

        internal void RefreshCrawling()
        {
            crawling = _pc.IsCrawling();

            if(!crawling && (stealth || _pc.StealthModeActive))
            {
                NWScript.SetActionMode(_pc.ObjectId, NWScript.ACTION_MODE_STEALTH, 0);
                NWScript.SetActionMode(_pc.ObjectId, NWScript.ACTION_MODE_STEALTH, 1);
            }

            Refresh();
        }


        internal async void MountHorseAsync()
        {
            await NwTask.Delay(TimeSpan.FromSeconds(0.2f));
            Refresh();
        }


        internal void DismountHorse()
        {
            Refresh(); 
        }

        internal void OnSurfaceMaterialChanged()
        {
            Refresh();
        }

        internal void RefreshFlying()
        {
            flying = _pc.IsFlying();

            if(_pc.IsPixie())
            {
                _pc.MovementRate = flying ? MovementRate.PC : MovementRate.Slow;
                var vt = _pc.VisualTransform.Translation;
                _pc.VisualTransform.Translation = new System.Numerics.Vector3(vt.X,vt.Y,flying ? 1.0f : 0.0f);
            }

            Refresh();
        }


        

        internal void PrintSpeed()
        {
            var speed = _pc.MovementRateFactor * 100;
            var cap = NWN.Core.NWNX.CreaturePlugin.GetMovementRateFactorCap(_pc.ObjectId) * 100;
            var fspeed = (float)Math.Round(speed,1);
            var fcap = (float)Math.Round(cap,1);
            string str = $"Aktualna prędkość ruchu: {fspeed}% / {fcap}%\n";

            string bonusStr = string.Empty;
            string penaltyStr = string.Empty;

            bool immobilized = false;

            foreach(var effKey in Enum.GetValues<EffectKeys>())
            {
                if(!HasActiveEffect(effKey)) continue;

                string effectName = string.Empty;
                switch (effKey)
                {
                    case EffectKeys.Haste: effectName = "Przyspieszenie\n"; break;
                    case EffectKeys.ExpeditiousRetreat: effectName = "Szybki odwrót\n"; break;
                    case EffectKeys.Slow: effectName = "Spowolnienie\n"; break;
                    case EffectKeys.NegativeSpellEffect: effectName = $"Obniżona prędkość ruchu o {(int)(negativeSpellPenalty*100)}%\n"; break;

                    default: immobilized = true; break;
                }
                
                if(effKey == EffectKeys.Slow || effKey == EffectKeys.NegativeSpellEffect)
                {
                    penaltyStr = "\nKary:\n";
                    penaltyStr += effectName.ColorString(ColorConstants.Red);
                }
                else if(effectName != string.Empty) {
                    if(bonusStr == string.Empty) bonusStr = "\nBonusy:\n";
                    bonusStr += effectName.ColorString(ColorConstants.Green);
                }

            }

            if(immobilized){
                if(penaltyStr == string.Empty) penaltyStr = "\nKary:\n";
                penaltyStr += "Unieruchomienie\n".ColorString(ColorConstants.Red);
            }

            foreach(var flag in Enum.GetValues<FeatFlags>())
            {
                if(!HasFeatFlag(flag) || IsFeatSuppressed(flag)) continue;

                var fName = MovementService.GetFeatDisplayName(FeatIDFromFlag(flag));
                if(string.IsNullOrEmpty(fName)) fName = "Błąd: nieznana umiejętność".ColorString(ColorConstants.Maroon);

                bool negativeFeat = flag == FeatFlags.SteadfastDefender;

                if (negativeFeat){
                    if(penaltyStr == string.Empty) penaltyStr = "\nKary:\n";
                    penaltyStr += fName.ColorString(ColorConstants.Red) + "\n";
                }
                else{
                    if(bonusStr == string.Empty) bonusStr = "\nBonusy:\n";
                    bonusStr += fName.ColorString(ColorConstants.Green) + "\n";
                }
            }

            if(bonusStr != string.Empty) str += bonusStr;
            if(penaltyStr != string.Empty) str += penaltyStr;

            if(!string.IsNullOrEmpty(horse))
            {
                str += $"\nWierzchowiec:\n";
                var details = 
                ($"Wartość bazowa: {(float)Math.Round(MovementService.GetHorseSpeed(horse)*100,1)}%\n"
                +$"Jeździectwo: {(ridingSkill > 0 ? "+" : "")}{ridingSkill}%\n").ColorString(ColorConstants.Gray);

                var surfMod = MovementService.GetHorseSurfaceMaterialModifier(horse, surface);

                if(surfMod < 0)
                    details+=$"Podłoże ({MovementService.GetSurfaceMaterialName(surface)}): {(float)Math.Round(surfMod*100, 1)}%\n".ColorString(ColorConstants.Red);
                else if(surfMod > 0)
                    details+=$"Podłoże ({MovementService.GetSurfaceMaterialName(surface)}): +{(float)Math.Round(surfMod*100, 1)}%\n".ColorString(ColorConstants.Green);

                var envMod = _pc.Area == null ? 0f : MovementService.GetHorseEnvironmentModifier(horse, _pc.Area);

                if(envMod < 0)
                    details += $"Środowisko: {(float)Math.Round(envMod*100, 1)}%".ColorString(ColorConstants.Red);
                else if(envMod > 0)
                    details += $"Środowisko: +{(float)Math.Round(envMod*100, 1)}%".ColorString(ColorConstants.Green);

                str += details;
            }

            if(aberrationTravelEQModifier != 0) str += _pc.IsSatyr() ? $"Podkowy: +{Math.Round(aberrationTravelEQModifier*100,1)}%" : $"Lotki: +{Math.Round(aberrationTravelEQModifier*100,1)}%";

            var surfSpeedMod = MovementService.GetSurfaceSpeedModifierForCreature(surface, _pc);
            surfSpeedMod = (float)Math.Round(surfSpeedMod*100, 1);

            if(string.IsNullOrEmpty(horse)){
                if(surfSpeedMod < 0) str += $"\nTrudny teren ({MovementService.GetSurfaceMaterialName(surface)}): {surfSpeedMod}%".ColorString(ColorConstants.Red);
                else if(surfSpeedMod > 0) str += $"\nNaturalny teren ({MovementService.GetSurfaceMaterialName(surface)}): +{surfSpeedMod}%".ColorString(ColorConstants.Green);
            }

            _pc.ControllingPlayer?.SendServerMessage(str);
        }

    }
}