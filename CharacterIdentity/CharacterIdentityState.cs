using System;
using System.Collections.Generic;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

using NLog;

using ExtensionsPlugin;

using NWNXRename = NWN.Core.NWNX.RenamePlugin;
using System.Linq;
using CharacterIdentity.UI;


namespace CharacterIdentity
{
    internal sealed class CharacterIdentityState : IDisposable
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private static readonly Dictionary<NwPlayer, CharacterIdentityState> _onlinePlayersIdentities = new();

        private static readonly string _colorPrefix = "<c¥¥¥>";
        private static readonly string _colorSuffix = "</c>";


        // DISGUISED status overrides by gender:
        private static readonly string _strangerMale = "Nieznajomy";
        private static readonly string _strangerFemale = "Nieznajoma";


        public static CharacterIdentityState? GetState(NwPlayer player)
        {
            if (!player.IsValid)
                ClearFromPlayer(player);

            if (_onlinePlayersIdentities.TryGetValue(player, out var cis))
                return cis;

            return null;
        }


        private static readonly HashSet<NwPlayer> _keysToRemove = new();
        /// <summary>
        ///  // todo: ensure OnClientDisconnect is working every time and move cleanup logic there if it is .
        /// </summary>
        private static void CleanupInvalidPlayersCache()
        {
            _keysToRemove.Clear();

            foreach (var cis in _onlinePlayersIdentities)
                if (!cis.Key.IsValid)
                {
                    cis.Value.Dispose();
                    _keysToRemove.Add(cis.Key);
                }

            if (_keysToRemove.Count > 0) _log.Warn("Cleaning up " + _keysToRemove.Count + " invalid player keys from cache");

            else return;

            foreach (var key in _keysToRemove)
                _ = _onlinePlayersIdentities.Remove(key);
        }

        private static async void RefreshIdentityAppearanceAsync(int id, NwCreature creature)
        {
            await NwTask.Delay(TimeSpan.FromSeconds(0.2));
            if(!creature.IsValid) return;
            if(!IdentityManager.CharacterAppearance.LoadBodyAppearance(id, creature))
            {
                await NwTask.Delay(TimeSpan.FromSeconds(0.2));
                if(!creature.IsValid) return;
                IdentityManager.CharacterAppearance.SaveBodyAppearance(id, creature);
            }
        }


        internal static void CreateForPlayer(NwPlayer player)
        {
            CleanupInvalidPlayersCache();

            if (!player.IsValid)
                return;

            var creature = player.LoginCreature;

            if (creature == null || !creature.IsValid || !creature.TryGetUUID(out var guid))
                return;

            var uuid = guid.ToUUIDString();


            IdentityInfo? trueIdentityInfo = null;

            IdentityInfo? activeIdentityInfo = null;

            var mySQL = IdentityManager.MySQL;

            var playerSQLMap = ServerData.DataProviders.PlayerSQLMap;
            var identitySQLMap = ServerData.DataProviders.IdentitySQLMap;

            // query for active and true identity data, should get 1 or 2 rows, depending on whether currently active identity is the actual true identity 
            string query = $@"
                SELECT {IdentityInfo.SQL_SELECT} 
                FROM {identitySQLMap.TableName} 
                WHERE {playerSQLMap.UUID} = '{uuid}' 
                AND ({identitySQLMap.IsActive} = 1 OR {identitySQLMap.IsTrue} = 1) 
                LIMIT 2;"; // not using builder because of lack of support for AndGroup() with OR

            using (var result = mySQL.ExecuteQuery(query))
            {
                if (!result.HasData)
                {
                    player.BootPlayer("Nie znaleziono prawdziwej / aktywnej tożsamości w bazie danych");

                    _log.Error("No true or active identity (or both) in database for player " + player.PlayerName);

                    return;
                }

                foreach (var row in result)
                {
                    var info = IdentityInfo.FromSqlRowData(row, out var isTrue, out var isActive);

                    if (isTrue) trueIdentityInfo = info;
                    if (isActive) activeIdentityInfo = info;
                }
            }

            if (string.IsNullOrEmpty(trueIdentityInfo?.Name) || string.IsNullOrEmpty(activeIdentityInfo?.Name))
            {
                player.BootPlayer("Prawdziwa / Aktywna tożsamość jest nieprawidłowa");

                _log.Error("Invalid true or active identity (or both) in database for player " + player.PlayerName);

                return;
            }


            // get all acquaintances of this character from database    
            // // TODO: support custom acquaintance name overrides
            HashSet<Guid> acquaintances = new();

            var acquaintanceSQLMap = ServerData.DataProviders.AcquaintanceSQLMap;

            mySQL.QueryBuilder.Select(acquaintanceSQLMap.TableName, acquaintanceSQLMap.UUID)
            .Where(playerSQLMap.UUID, uuid);

            using (var result = mySQL.ExecuteQuery())
            {
                if (result.HasData)
                    foreach (var row in result)
                    {
                        if (row.TryGet<string>(0, out var uuidStr) && Guid.TryParse(uuidStr, out var acquaintanceGuid))
                            _ = acquaintances.Add(acquaintanceGuid);
                    }
            }

            // check agaist edge case when the player disconnected mid-query
            if (player == null || !player.IsValid) return;


            if (_onlinePlayersIdentities.TryGetValue(player, out var cis)) // clean old state object if it exists (it shouldn't exist)
            {
                _log.Warn($"Cleaning up orphaned {nameof(CharacterIdentityState)} object from player {player.PlayerName}.");

                cis.Dispose();

                cis = new CharacterIdentityState(player, creature, trueIdentityInfo, activeIdentityInfo, acquaintances);

                _onlinePlayersIdentities[player] = cis;
            }
            else
            {
                cis = new CharacterIdentityState(player, creature, trueIdentityInfo, activeIdentityInfo, acquaintances);

                _onlinePlayersIdentities.Add(player, cis);
            }

            if(!cis.IsPolymorphed) RefreshIdentityAppearanceAsync(cis.ActiveIdentity.ID, creature);

            cis.RefreshNameOverrides(true);
        }

        public static void ClearFromPlayer(NwPlayer player)
        {

            if (_onlinePlayersIdentities.TryGetValue(player, out var cis))
                cis.Dispose();

            _ = _onlinePlayersIdentities.Remove(player);
        }


        private readonly NwPlayer _player;
        private readonly NwCreature _loginCharacter;
        private readonly HashSet<Guid> _acquaintances;

        private readonly Action<OnPolymorphApply> _onPolymorphApplyDelegate;
        private readonly Action<OnPolymorphRemove> _onPolymorphRemoveDelegate;
        private readonly Action<OnItemEquip> _onItemEquipDelegate;
        private readonly Action<OnItemUnequip> _onItemUnequipDelegate;
        private readonly Action<OnLevelDown> _onLevelDownDelegate;
        private readonly Action<OnLevelUp> _onLevelUpDelegate;
        private readonly Action<CreatureEvents.OnUserDefined> _onUserDefinedDelegate;


        private string? polymorphName;
        private string? disguiseName;


        internal bool IsPolymorphed => currentState.HasFlag(State.Polymorphed);

        public readonly IdentityInfo TrueIdentity;
        private IdentityInfo? _falseIdentity;

        public IdentityInfo ActiveIdentity => _falseIdentity ?? TrueIdentity;
        private string CurrentNameOverride => polymorphName ?? disguiseName ?? ActiveIdentity.Name;

        private bool IsValid => currentState != State.Invalid && _player.IsValid && _loginCharacter.IsValid;

        [Flags] private enum State : byte
        {
            Invalid = 0,
            TrueIdentity = 1,
            FalseIdentity = 2,
            Disguised = 4,
            Polymorphed = 8,
            SelfAcquaintance = 16
        }
        private State currentState = default;


        private CharacterIdentityState(NwPlayer player, NwCreature creature, IdentityInfo trueIdentity, IdentityInfo activeIdentity, HashSet<Guid> acquaintances)
        {
            _player = player;

            _loginCharacter = creature;


            TrueIdentity = trueIdentity;

            _falseIdentity = trueIdentity == activeIdentity ? null : activeIdentity;


            _acquaintances = acquaintances;

            currentState = _falseIdentity == TrueIdentity ? State.TrueIdentity : State.FalseIdentity;

            if (_acquaintances.Contains(_loginCharacter.UUID))
                currentState |= State.SelfAcquaintance;

            if (!_loginCharacter.Race.IsPlayerRace)
            {
                currentState |= State.Polymorphed;

                var lastPolyTypeVar = _loginCharacter.GetObjectVariable<LocalVariableInt>("LastPolymorphTypeRowIndex");
                var lastDisguisedVar = _loginCharacter.GetObjectVariable<LocalVariableBool>("WasDisguisedBeforePolymorph");

                if (lastDisguisedVar.Value)
                {
                    currentState |= State.Disguised;
                    disguiseName = _loginCharacter.Gender == Gender.Male ? _strangerMale : _strangerFemale;
                }

                if (lastPolyTypeVar.HasValue)
                    polymorphName = CharacterIdentityService.PolymorphCreatureNamesConfig[lastPolyTypeVar.Value];
                else
                    polymorphName = CharacterIdentityService.PolymorphCreatureNamesConfig[-1]; // fallback to "Unknown Polymorph"
            }
            else
            {
                var headItem = _loginCharacter.GetItemInSlot(InventorySlot.Head);

                if (headItem != null && headItem.IsValid && headItem.HiddenWhenEquipped < 1)
                {
                    currentState |= State.Disguised;
                    _loginCharacter.PortraitResRef = !currentState.HasFlag(State.SelfAcquaintance) ? _loginCharacter.GetDefaultPortraitResRef() : ActiveIdentity.Identity.Portrait;
                    disguiseName = _loginCharacter.Gender == Gender.Male ? _strangerMale : _strangerFemale;
                }
            }

            _onPolymorphApplyDelegate = OnCreaturePolymorphedAfter;
            IdentityManager.EventService.Subscribe<OnPolymorphApply, OnPolymorphApply.Factory>(creature, _onPolymorphApplyDelegate, EventCallbackType.After);

            _onPolymorphRemoveDelegate = OnCreatureUnPolymorphedAfter;
            IdentityManager.EventService.Subscribe<OnPolymorphRemove, OnPolymorphRemove.Factory>(creature, _onPolymorphRemoveDelegate, EventCallbackType.After);

            _onItemEquipDelegate = OnCreatureEquipItemAfter;
            IdentityManager.EventService.Subscribe<OnItemEquip, OnItemEquip.Factory>(creature, _onItemEquipDelegate, EventCallbackType.After);

            _onItemUnequipDelegate = OnCreatureUnequipItemAfter;
            IdentityManager.EventService.Subscribe<OnItemUnequip, OnItemUnequip.Factory>(creature, _onItemUnequipDelegate, EventCallbackType.After);

            _onLevelDownDelegate = OnCreatureLevelDownAfter;
            IdentityManager.EventService.Subscribe<OnLevelDown, OnLevelDown.Factory>(creature, _onLevelDownDelegate, EventCallbackType.After);

            _onLevelUpDelegate = OnCreatureLevelUpAfter;
            IdentityManager.EventService.Subscribe<OnLevelUp, OnLevelUp.Factory>(creature, _onLevelUpDelegate, EventCallbackType.After);

            _onUserDefinedDelegate = OnUserDefined;
            creature.OnUserDefined += _onUserDefinedDelegate;
        }

        public void Dispose()
        {
            IdentityManager.EventService.Unsubscribe<OnPolymorphApply, OnPolymorphApply.Factory>(_loginCharacter, _onPolymorphApplyDelegate, EventCallbackType.After);
            IdentityManager.EventService.Unsubscribe<OnPolymorphRemove, OnPolymorphRemove.Factory>(_loginCharacter, _onPolymorphRemoveDelegate, EventCallbackType.After);
            IdentityManager.EventService.Unsubscribe<OnItemEquip, OnItemEquip.Factory>(_loginCharacter, _onItemEquipDelegate, EventCallbackType.After);
            IdentityManager.EventService.Unsubscribe<OnItemUnequip, OnItemUnequip.Factory>(_loginCharacter, _onItemUnequipDelegate, EventCallbackType.After);
            IdentityManager.EventService.Unsubscribe<OnLevelDown, OnLevelDown.Factory>(_loginCharacter, _onLevelDownDelegate, EventCallbackType.After);
            IdentityManager.EventService.Unsubscribe<OnLevelUp, OnLevelUp.Factory>(_loginCharacter, _onLevelUpDelegate, EventCallbackType.After);

            _loginCharacter.OnUserDefined -= _onUserDefinedDelegate;
        }

        void OnUserDefined(CreatureEvents.OnUserDefined eventData)
        {
            if (!IsValid || eventData.Creature != _loginCharacter)
                return;

            var evtNum = eventData.EventNumber;

            if (evtNum == CharacterIdentityService.ServiceConfig.OnIdentityNuiOpenUserEventNumber)
                FalseIdentityUI.Open(_player);

            else if (evtNum == CharacterIdentityService.ServiceConfig.OnAcquaintancesChangedUserEventNumber)
                OnAcquaintancesChanged();

            else if (evtNum == CharacterIdentityService.ServiceConfig.OnHeadSlotVisibilityChangedUserEventNumber)
                OnHeadSlotVisibilityChanged();

            else if(evtNum == CharacterIdentityService.ServiceConfig.OnCharacterSheetUpdate)
                OnCharacterSheetUpdate();
        }

        /// <summary>
        /// Set to null to restore the original true identity
        /// </summary>
        public void SetFalseIdentity(IdentityInfo? newIdentity = null, bool updateHowPlayerSeeOthers = false)
        {
            if ((newIdentity == null && _falseIdentity == null) || newIdentity == ActiveIdentity) return; // identity didn't change

            var uuid = _loginCharacter.UUID.ToUUIDString();

            var identitySQLMap = ServerData.DataProviders.IdentitySQLMap;
            var playerSQLMap = ServerData.DataProviders.PlayerSQLMap;

            IdentityManager.MySQL.QueryBuilder.Update(identitySQLMap.TableName, identitySQLMap.IsActive, 0)
            .Where(playerSQLMap.UUID, uuid)
            .And(identitySQLMap.ID, ActiveIdentity.ID);

            _ = IdentityManager.MySQL.ExecuteQuery();

            _falseIdentity = newIdentity;

            IdentityManager.MySQL.QueryBuilder.Update(identitySQLMap.TableName, identitySQLMap.IsActive, 1)
            .Where(playerSQLMap.UUID, uuid)
            .And(identitySQLMap.ID, ActiveIdentity.ID);

            _ = IdentityManager.MySQL.ExecuteQuery();


            if (newIdentity != TrueIdentity)
                currentState = (currentState | State.FalseIdentity) & ~State.TrueIdentity;
            else
                currentState = (currentState | State.TrueIdentity) & ~State.FalseIdentity;



            if (currentState.HasFlag(State.Polymorphed))
            {
                _loginCharacter.Description = " "; // hide character description when polymorphed
                RefreshNameOverrides(updateHowPlayerSeeOthers);
                return;
            }

            _loginCharacter.Age = ActiveIdentity.Identity.Age;
            _loginCharacter.Description = currentState.HasFlag(State.Polymorphed) ? string.Empty : ActiveIdentity.Identity.Description;
            _loginCharacter.PortraitResRef = (currentState.HasFlag(State.Disguised) && !currentState.HasFlag(State.SelfAcquaintance)) ? _loginCharacter.GetDefaultPortraitResRef() : ActiveIdentity.Identity.Portrait;
            _loginCharacter.Gender = ActiveIdentity.Identity.Gender;

            RefreshNameOverrides(updateHowPlayerSeeOthers);

            RefreshIdentityAppearanceAsync(ActiveIdentity.ID, _loginCharacter);
        }
        
        private void RefreshNameForObserver(CharacterIdentityState observerState)
        {
            if
            (
                _acquaintances.Contains(observerState._loginCharacter.UUID)
                || (currentState.HasFlag(State.FalseIdentity) && IdentityManager.CanObserverSeeTrueName(_player, observerState._player))
            )
            NWNXRename.SetPCNameOverride(_loginCharacter.ObjectId, TrueIdentity.Name, oObserver: observerState._loginCharacter.ObjectId);

            else NWNXRename.ClearPCNameOverride(_loginCharacter.ObjectId, observerState._loginCharacter.ObjectId, 0); // clear personal override if any (personal have higher priority than global)
        }

        private void ResolveTrueIdentityData()
        {
            var identitySQLMap = ServerData.DataProviders.IdentitySQLMap;
            var playerSQLMap = ServerData.DataProviders.PlayerSQLMap;

            IdentityManager.MySQL.QueryBuilder.Select(identitySQLMap.TableName, IdentityInfo.SQL_SELECT)
            .Where(playerSQLMap.UUID, _loginCharacter.UUID.ToUUIDString()).And(identitySQLMap.IsTrue, 1).Limit(1);

            using var result = IdentityManager.MySQL.ExecuteQuery();

            if (!result.HasData) return;

            var info = IdentityInfo.FromSqlRowData(result.First(), out _, out _);

            TrueIdentity.Identity = info.Identity; // update internal identity for TrueIdentity wrapper
        }
        

        /// <summary>
        /// Update overrides for all observers, and also update how this player see observer names. Apply appearance changes if relevant.
        /// </summary>
        void RefreshNameOverrides(bool updateHowPlayerSeeOthers)
        {
            if (!IsValid) return;

            ResolveTrueIdentityData();// update true identity data in case there were any changes made to the database entry, i.e portrait/description changed

            // skip updating observers if the true name is visible for all
            if ((currentState & (State.Disguised | State.Polymorphed)) != 0 && currentState.HasFlag(State.SelfAcquaintance) && currentState.HasFlag(State.TrueIdentity))
            {
                NWNXRename.ClearPCNameOverride(_loginCharacter.ObjectId, clearAll: 1);

                if (updateHowPlayerSeeOthers)
                    foreach (var player in NwModule.Instance.Players)
                    {
                        if (player == _player) continue;

                        GetState(player)?.RefreshNameForObserver(this);
                    }

                return;
            }

            // add color for stranger/polymorph names
            if ((currentState & (State.Polymorphed | State.Disguised)) != 0 && !currentState.HasFlag(State.SelfAcquaintance))
            {
                NWNXRename.SetPCNameOverride(_loginCharacter.ObjectId, CurrentNameOverride, _colorPrefix, _colorSuffix, 2);
            }
            else NWNXRename.SetPCNameOverride(_loginCharacter.ObjectId, ActiveIdentity.Name, iPlayerNameState: 2);


            // now when the global name is set we can iterate over each player and clear the override for specific observer if specific conditions are met
            foreach (var observer in NwModule.Instance.Players)
            {
                if (observer == _player) continue;

                var obsState = GetState(observer);

                if (obsState == null) continue;

                RefreshNameForObserver(obsState);

                if (updateHowPlayerSeeOthers)
                    obsState.RefreshNameForObserver(this);
            }
        }


        private void OnCreaturePolymorphedAfter(OnPolymorphApply eventData)
        {
            if (!IsValid) return;

            if (eventData.PreventPolymorph) return;

            var pc = eventData.Creature;

            if (!pc.IsLoginPlayerCharacter) return;
            
            if (currentState.HasFlag(State.Polymorphed)) return; // handle double-fired events

            currentState |= State.Polymorphed;

            var polyID = eventData.PolymorphType.RowIndex;

            polymorphName = CharacterIdentityService.PolymorphCreatureNamesConfig[eventData.PolymorphType.RowIndex];

            _loginCharacter.GetObjectVariable<LocalVariableInt>("LastPolymorphTypeRowIndex").Value = polyID;
            _loginCharacter.GetObjectVariable<LocalVariableBool>("WasDisguisedBeforePolymorph").Value = currentState.HasFlag(State.Disguised);

            _loginCharacter.Description = " "; // hide character description when polymorphed

            RefreshNameOverrides(false);
        }

        private void OnCreatureUnPolymorphedAfter(OnPolymorphRemove eventData)
        {
            if (!IsValid) return;

            if (eventData.PreventRemove) return;

            var pc = eventData.Creature;

            if (!pc.IsLoginPlayerCharacter) return;

            if (!currentState.HasFlag(State.Polymorphed)) return; // handle double-fired events

            currentState &= ~State.Polymorphed;

            polymorphName = null;

            _loginCharacter.GetObjectVariable<LocalVariableInt>("LastPolymorphTypeRowIndex").Delete();
            _loginCharacter.GetObjectVariable<LocalVariableBool>("WasDisguisedBeforePolymorph").Delete();

            _loginCharacter.Age = ActiveIdentity.Identity.Age;
            _loginCharacter.Description = ActiveIdentity.Identity.Description;
            _loginCharacter.PortraitResRef = (currentState.HasFlag(State.Disguised) && !currentState.HasFlag(State.SelfAcquaintance)) ? _loginCharacter.GetDefaultPortraitResRef() : ActiveIdentity.Identity.Portrait;
            _loginCharacter.Gender = ActiveIdentity.Identity.Gender;

            RefreshNameOverrides(false);

            LoadAppearanceAfterUnPolymorphAsync();
        }


        private async void LoadAppearanceAfterUnPolymorphAsync()
        {
            if(!IsValid) return;
            _loginCharacter.Gender = ActiveIdentity.Identity.Gender;
            await NwTask.Delay(TimeSpan.FromSeconds(0.6f));
            if(!IsValid) return;
            IdentityManager.CharacterAppearance.LoadBodyAppearance(ActiveIdentity.ID, _loginCharacter);
        }

        private void OnCreatureEquipItemAfter(OnItemEquip eventData)
        {
            if (!IsValid) return;

            if (eventData.PreventEquip || eventData.Slot != InventorySlot.Head) return;

            if (_loginCharacter.GetItemInSlot(InventorySlot.Head) == null) return; // prevent unequip does not seem to work here (?)

            var item = eventData.Item;


            if (item == null || !item.IsValid || item.HiddenWhenEquipped > 0)
            {
                if (currentState.HasFlag(State.Disguised))
                {
                    currentState &= ~State.Disguised;

                    _loginCharacter.PortraitResRef = ActiveIdentity.Identity.Portrait;

                    disguiseName = null;
                    
                    RefreshNameOverrides(false);
                }
                return;
            }

            if (!currentState.HasFlag(State.Disguised))
            {
                currentState |= State.Disguised;

                _loginCharacter.PortraitResRef = !currentState.HasFlag(State.SelfAcquaintance) ? _loginCharacter.GetDefaultPortraitResRef() : ActiveIdentity.Identity.Portrait;

                disguiseName = _loginCharacter.Gender == Gender.Male ? _strangerMale : _strangerFemale;
                    
                RefreshNameOverrides(false);
            }
        }

        private void OnCreatureUnequipItemAfter(OnItemUnequip eventData)
        {
            if (!IsValid) return;

            if (eventData.PreventUnequip) return;

            var item = _loginCharacter.GetItemInSlot(InventorySlot.Head);

            if (item == null || !item.IsValid || item.HiddenWhenEquipped > 0)
            {
                if (currentState.HasFlag(State.Disguised))
                {
                    currentState &= ~State.Disguised;

                    disguiseName = null;

                    _loginCharacter.PortraitResRef = ActiveIdentity.Identity.Portrait;

                    RefreshNameOverrides(false);
                }

                return;
            }

            if (!currentState.HasFlag(State.Disguised))
            {
                currentState |= State.Disguised;

                _loginCharacter.PortraitResRef = !currentState.HasFlag(State.SelfAcquaintance) ? _loginCharacter.GetDefaultPortraitResRef() : ActiveIdentity.Identity.Portrait;

                disguiseName = _loginCharacter.Gender == Gender.Male ? _strangerMale : _strangerFemale;

                RefreshNameOverrides(false);
            }
        }


        bool CanUseFalseIdentities
        {
            get
            {
                if(IsValid && _loginCharacter.IsGypsy()) return true;
                
                if (!IsValid || _loginCharacter.GetSkillRank(NwSkill.FromSkillType(Skill.Bluff)!, true) < CharacterIdentityService.ServiceConfig.BluffRanksPerIdentity) 
                    return false;

                foreach (var c in _loginCharacter.Classes)
                {
                    if (CharacterIdentityService.ServiceConfig.RequiredClassLevels.TryGetValue(c.Class.ClassType.ToString(), out var levels) && levels <= c.Level) 
                        return true;
                }
                
                return false;
            }
        }
        
        private void OnCreatureLevelDownAfter(OnLevelDown eventData)
        {
            if (!IsValid) return;

            if (!CanUseFalseIdentities)
                SetFalseIdentity(null, true);
        }


        private void OnCreatureLevelUpAfter(OnLevelUp eventData)
        {
            if (!IsValid) return;

            RefreshNameOverrides(true);
        }


        private void OnAcquaintancesChanged()
        {
            if (!IsValid) return;

            _acquaintances.Clear();

            var acquaintanceSQLMap = ServerData.DataProviders.AcquaintanceSQLMap;
            var playerSQLMap = ServerData.DataProviders.PlayerSQLMap;

            IdentityManager.MySQL.QueryBuilder.Select(acquaintanceSQLMap.TableName, acquaintanceSQLMap.UUID)
            .Where(playerSQLMap.UUID, _loginCharacter.UUID);

            using (var result = IdentityManager.MySQL.ExecuteQuery())
            {
                if (result.HasData)
                    foreach (var row in result)
                    {
                        if (row.TryGet<string>(0, out var uuidStr) && Guid.TryParse(uuidStr, out var acquaintanceGuid))
                            _ = _acquaintances.Add(acquaintanceGuid);
                    }
            }

            if (_acquaintances.Contains(_loginCharacter.UUID)) currentState |= State.SelfAcquaintance;

            else currentState &= ~State.SelfAcquaintance;

            _loginCharacter.PortraitResRef = (currentState.HasFlag(State.Disguised) && !currentState.HasFlag(State.SelfAcquaintance)) ? _loginCharacter.GetDefaultPortraitResRef() : ActiveIdentity.Identity.Portrait;

            RefreshNameOverrides(false);
        }

        private void OnHeadSlotVisibilityChanged()
        {
            if (!IsValid) return;

            var item = _loginCharacter.GetItemInSlot(InventorySlot.Head);

            if (item == null || !item.IsValid || item.HiddenWhenEquipped > 0)
            {
                if (!currentState.HasFlag(State.Disguised))
                    return;

                currentState &= ~State.Disguised;

                _loginCharacter.PortraitResRef = ActiveIdentity.Identity.Portrait;
                disguiseName = null;
            }
            else
            {
                if (currentState.HasFlag(State.Disguised))
                    return;

                currentState |= State.Disguised;

                _loginCharacter.PortraitResRef = !currentState.HasFlag(State.SelfAcquaintance) ? _loginCharacter.GetDefaultPortraitResRef() : ActiveIdentity.Identity.Portrait;
                disguiseName = _loginCharacter.Gender == Gender.Male ? _strangerMale : _strangerFemale;
            }

            RefreshNameOverrides(false);
        }

        private void OnCharacterSheetUpdate()
        {
            if(!IsValid) return;

            if(ActiveIdentity.ID == TrueIdentity.ID) IdentityManager.UpdateTrueIdentity(_loginCharacter);
            
            else _loginCharacter.ControllingPlayer?.SendServerMessage("Zmian w fałszywych tożsamościach należy dokonywać przez menu 'Tożsamości'");
        }
    }
}