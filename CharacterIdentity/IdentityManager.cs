using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

using Anvil.API;
using Anvil.Services;

using MySQLClient;

using NLog;
using CharacterAppearance;


namespace CharacterIdentity
{
    internal static class IdentityManager
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private static EventService? _eventService;
        internal static EventService EventService
        {
            get => _eventService ?? throw new NullReferenceException($"{nameof(Anvil.Services.EventService)} instance is not provided yet.");
            set { if (_eventService == null) _eventService = value; else throw new InvalidOperationException($"Re-assignment of {nameof(Anvil.Services.EventService)} is prohibited."); }
        }

        private static MySQLService? _mySQL;
        internal static MySQLService MySQL
        {
            get => _mySQL ?? throw new NullReferenceException($"{nameof(MySQLService)} instance is not provided yet.");
            set { if (_mySQL == null) _mySQL = value; else throw new InvalidOperationException($"Re-assignment of {nameof(MySQLService)} is prohibited."); }
        }

        private static PortraitStorageService? _portraitStorage;
        internal static PortraitStorageService PortraitStorage
        {
            get => _portraitStorage ?? throw new NullReferenceException($"{nameof(PortraitStorageService)} instance is not provided yet.");
            set { if (_portraitStorage == null) _portraitStorage = value; else throw new InvalidOperationException($"Re-assignment of {nameof(PortraitStorageService)} is prohibited."); }
        }

        private static CharacterAppearanceService? _characterAppearance;
        internal static CharacterAppearanceService CharacterAppearance
        {
            get => _characterAppearance ?? throw new NullReferenceException($"{nameof(CharacterAppearanceService)} instance is not provided yet.");
            set { if (_characterAppearance == null) _characterAppearance = value; else throw new InvalidOperationException($"Re-assignment of {nameof(CharacterAppearanceService)} is prohibited."); }
        }

        private static readonly NwSkill _bluffSkill = NwSkill.FromSkillType(Skill.Bluff) ?? throw new InvalidOperationException("\'Bluff\' skill not found.");
        private static readonly NwSkill _performSkill = NwSkill.FromSkillType(Skill.Perform) ?? throw new InvalidOperationException("\'Perform\' skill not found.");
        private static NwSkill? _senseMotiveSkill = null;


        public static int GetMaxIdentities(NwCreature pc) => pc.GetSkillRank(_bluffSkill, true) / CharacterIdentityService.ServiceConfig.BluffRanksPerIdentity;
        
        public static int GetIdentityRank(NwCreature pc)
        {
            if (CharacterIdentityService.ServiceConfig.AddPerformRanksToDC)
                return pc.GetSkillRank(_bluffSkill, true) + pc.GetSkillRank(_performSkill, true) / CharacterIdentityService.ServiceConfig.PerformRanksPerBonusPointDC;

            else return pc.GetSkillRank(_bluffSkill, true);

        }

        private static int GetObserverRank(NwCreature oc)
        {
            _senseMotiveSkill ??= NwSkill.FromSkillId(CharacterIdentityService.ServiceConfig.SenseMotiveSkillID)
            ?? throw new InvalidOperationException("Failed to get NwSkill from SenseMotiveSkillID " + CharacterIdentityService.ServiceConfig.SenseMotiveSkillID);

            return oc.GetSkillRank(_senseMotiveSkill, true);
        }

        public static bool CanObserverSeeTrueName(NwPlayer player, NwPlayer observer) => GetObserverRank(player.ControlledCreature!) > GetIdentityRank(observer.ControlledCreature!);
        //dbg:
        // public static bool CanObserverSeeTrueName(NwPlayer player, NwPlayer observer)
        // {
        //     bool result = GetObserverRank(observer.ControlledCreature!) > GetIdentityRank(player.ControlledCreature!);
        //     if (result) _log.Warn("Observer " + observer.ControlledCreature!.Name + " sees through false identity of " + player.ControlledCreature!.Name);
        //     else _log.Info("Observer " + observer.ControlledCreature!.Name + " can't see through false identity of " + player.ControlledCreature!.Name);
        //     return result;
        // }





        public static bool DeleteIdentity(int id)
        {
            var sqlMap = ServerData.DataProviders.IdentitySQLMap;
            MySQL.QueryBuilder.DeleteFrom(sqlMap.TableName).Where(sqlMap.ID, id);
            return MySQL.ExecuteQuery().Rows > 0;
        }





        private static IdentityInfo? CreateTrueIdentity(NwCreature pc)
        {
            var identity = new Identity(
                firstName: pc.OriginalFirstName,
                lastname: pc.OriginalLastName,
                age: pc.Age,
                gender: pc.Gender,
                description: pc.Description,
                portrait: pc.PortraitResRef
            );

            if (!TrySaveCharacterIdentityInDatabase(pc.UUID, identity, true, out var info))
                return null;

            return info;
        }

        internal static bool EnsureTrueIdentity(NwCreature pc)
        {
            if (!pc.IsValid || !pc.IsLoginPlayerCharacter(out var player) || !player.IsValid || !pc.TryGetUUID(out var pcGuid))
            {
                return false;
            }

            var identitySQLMap = ServerData.DataProviders.IdentitySQLMap;
            var playerSQLMap = ServerData.DataProviders.PlayerSQLMap;

            MySQL.QueryBuilder.Select(identitySQLMap.TableName, identitySQLMap.ID)
            .Where(playerSQLMap.UUID, pcGuid.ToUUIDString())
            .And(identitySQLMap.IsTrue, 1)
            .Limit(2);

            using var result = MySQL.ExecuteQuery();

            var count = !result.HasData ? 0 : result.Count();

            switch (count)
            {
                case -1: return false;
                case 0: break;
                case 1: return true;
                default: _log.Error(count.ToString() + " TRUE identities found for character " + pc.Name); return false;
            }

            var info = CreateTrueIdentity(pc);

            if (info == null)
            {
                _log.Error("Failed to create true identity.");
                return false;
            }
            else
            {
                _log.Info("Gracefully created a new true identity for character " + info.Name);
            }

            MySQL.QueryBuilder.Update(identitySQLMap.TableName, identitySQLMap.IsActive, 1)
            .Where(identitySQLMap.ID, info.ID)
            .Limit(1);

            if (MySQL.ExecuteQuery().Rows != 1)
            {
                MySQL.QueryBuilder.DeleteFrom(identitySQLMap.TableName).Where(identitySQLMap.ID, info.ID);
                _ = MySQL.ExecuteQuery();

                _log.Error("Failed to activate the new true identity.");
                
                return false;
            }

            return true;
        }

        internal static bool EnsureActiveIdentity(NwCreature pc)
        {
            if (!pc.IsValid || !pc.IsLoginPlayerCharacter(out var player) || !player.IsValid || !pc.TryGetUUID(out var pcGuid))
            {
                return false;
            }

            var identitySQLMap = ServerData.DataProviders.IdentitySQLMap;
            var playerSQLMap = ServerData.DataProviders.PlayerSQLMap;

            MySQL.QueryBuilder.Select(identitySQLMap.TableName, identitySQLMap.ID)
            .Where(playerSQLMap.UUID, pcGuid.ToUUIDString())
            .And(identitySQLMap.IsActive, 1)
            .Limit(2);

            using var result = MySQL.ExecuteQuery();

            var count = !result.HasData ? 0 : result.Count();

            switch (count)
            {
                case -1:
                case 0: return false;
                case 1: return true;
                default: _log.Error(count.ToString() + " ACTIVE identities found for character " + pc.Name); return false;
            }
        }

        /// <summary>
        /// For handling external changes
        /// </summary>
        internal static void UpdateTrueIdentity(NwCreature pc)
        {
            if(!pc.IsValid) return;

            string desc = pc.Description;
            string portrait = pc.PortraitResRef;

            var identitySQLMap = ServerData.DataProviders.IdentitySQLMap;
            var playerSQLMap = ServerData.DataProviders.PlayerSQLMap;

            MySQL.QueryBuilder.Update(identitySQLMap.TableName, $"{identitySQLMap.Portrait}, {identitySQLMap.Description}", portrait, desc)
            .Where(playerSQLMap.UUID, pc.UUID.ToUUIDString())
            .And(identitySQLMap.IsTrue, 1)
            .Limit(1);

            if(MySQL.ExecuteQuery().Rows != 1)
            {
                _log.Error("Failed to update True Identity data for character " + pc.Name);
            }
        }


        internal static bool TrySaveCharacterIdentityInDatabase(Guid pcGuid, Identity identity, bool isTrue, [NotNullWhen(true)] out IdentityInfo? info)
        {
            info = null;

            if (identity.IsEmpty) return false;

            var uuid = pcGuid.ToUUIDString();

            var identitySQLMap = ServerData.DataProviders.IdentitySQLMap;
            var playerSQLMap = ServerData.DataProviders.PlayerSQLMap;

            MySQL.QueryBuilder.InsertInto(identitySQLMap.TableName, IdentityInfo.SQL_INSERT, IdentityInfo.GetInsertIdentityQuery(pcGuid, identity, isTrue));

            if (MySQL.ExecuteQuery().Rows != 1) return false;

            MySQL.QueryBuilder.Select(identitySQLMap.TableName, identitySQLMap.ID)
            .Where(playerSQLMap.UUID, uuid)
            .And(identitySQLMap.IsTrue, 1)
            .Limit(1);

            using var result = MySQL.ExecuteQuery();

            if (!result.HasData) return false;

            info = new(result.First().Get<int>(0), identity);

            return true;
        }

        internal static bool TryGetCharacterIdentitityInfosFromDatabase(Guid pcGuid, [NotNullWhen(true)] out IdentityInfo[]? infos, out int trueIdentityID, out int activeIdentityID)
        {
            var identitySQLMap = ServerData.DataProviders.IdentitySQLMap;
            var playerSQLMap = ServerData.DataProviders.PlayerSQLMap;

            MySQL.QueryBuilder.Select(identitySQLMap.TableName, IdentityInfo.SQL_SELECT)
            .Where(playerSQLMap.UUID, pcGuid.ToUUIDString());

            trueIdentityID = 0;
            activeIdentityID = 0;

            infos = null;

            using var result = MySQL.ExecuteQuery();

            if (!result.HasData)
            {
                _log.Error("No result!");
                return false;
            }

            List<IdentityInfo> list;

            list = new();

            foreach (var row in result)
            {
                var info = IdentityInfo.FromSqlRowData(row, out var isTrue, out var isActive);

                if (info.ID <= 0 || info.Identity.IsEmpty)
                {
                    _log.Error("Invalid identity info obtained from database for character " + pcGuid.ToUUIDString());
                    return false;
                }

                if (isTrue)
                {
                    if (trueIdentityID != 0)
                    {
                        _log.Error("More than one true identity of character " + pcGuid.ToUUIDString() + " found in database");
                        return false;
                    }

                    trueIdentityID = info.ID;
                }

                if (isActive)
                {
                    if (activeIdentityID != 0)
                    {
                        _log.Error("More than one active identity of character " + pcGuid.ToUUIDString() + " found in database");
                        return false;
                    }

                    activeIdentityID = info.ID;
                }

                list.Add(info);
            }

            if (activeIdentityID == 0 || trueIdentityID == 0)
            {
                _log.Error("Character must have exactly one active and one true identity (can be both), but none has been found in database");

                return false;
            }

            infos = list.OrderBy(i => i.Identity.FirstName).ToArray();

            return true;
        }

        internal static bool TryUpdateIdentityInfoInDatabase(IdentityInfo info)
        {
            if (info.ID <= 0 || info.Identity.IsEmpty) return false;

            var identitySQLMap = ServerData.DataProviders.IdentitySQLMap;

            MySQL.QueryBuilder.Update(identitySQLMap.TableName, IdentityInfo.SQL_UPDATE, info.GetUpdateIdentityQuery())
            .Where(identitySQLMap.ID, info.ID);

            var rows = MySQL.ExecuteQuery().Rows;

            if (rows > 1) throw new InvalidOperationException("Multiple identity entries updated in database. Identity ID has to be unique!");

            return rows > 0;
        }

        internal static bool FindExactIdentity(Guid pcGuid, Identity data)
        {
            var identitySQLMap = ServerData.DataProviders.IdentitySQLMap;
            var playerSQLMap = ServerData.DataProviders.PlayerSQLMap;

            MySQL.QueryBuilder.Select(identitySQLMap.TableName, IdentityInfo.SQL_SELECT)
            .Where(playerSQLMap.UUID, pcGuid.ToUUIDString());

            using var result = MySQL.ExecuteQuery();

            if (!result.HasData) return false;

            foreach (var row in result)
            {
                var info = IdentityInfo.FromSqlRowData(row, out _, out _);

                if (info.Identity.FirstName == data.FirstName
                && info.Identity.LastName == data.LastName)
                // && info.Identity.Age == data.Age
                // && info.Identity.Gender == data.Gender
                // && info.Identity.Description == data.Description
                // && info.Identity.Portrait == data.Portrait)
                    return true;
            }

            return false;
        }
    }
}