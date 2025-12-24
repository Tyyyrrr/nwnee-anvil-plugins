using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Anvil.API;
using Anvil.Services;

using NLog;

using MySQLClient;
using CharacterAppearance.UI;
using NWN.Core;
using ExtensionsPlugin;
using EasyConfig;

namespace CharacterAppearance
{
    [ServiceBinding(typeof(CharacterAppearanceService))]
    public sealed class CharacterAppearanceService
    {
        void EnsureTable()
        {
            string query = ServerData.DataProviders.BodyAppearanceSQLMap.CreateTableIfNotExistsQuery;
            
            _ = _mySQL.ExecuteQuery(query);
        }

        private static EditorConfig? _editorCfg = null;

        internal static (float, float) ArmorEditCostMultiplierMinMax => _editorCfg == null ? (-1,-1) : (_editorCfg.ArmorEditCostMultiplierMin, _editorCfg.ArmorEditCostMultiplierMax);
        internal static float ArmorEditColorToPartRatio => _editorCfg == null ? 0f : _editorCfg.ArmorEditColorToPartRatio;
        internal static (float, float) WeaponEditCostMultiplierMinMax => _editorCfg == null ? (-1,-1) : (_editorCfg.WeaponEditCostMultiplierMin, _editorCfg.WeaponEditCostMultiplierMax);

        internal static int HairChangeCost => _editorCfg?.HairChangeCost ?? -1;
        internal static int HairColorChangeCost => _editorCfg?.HairColorChangeCost ?? -1;
        internal static int TattooCreateCost => _editorCfg?.TattooCreateCost ?? -1;
        internal static int TattooRemoveCost => _editorCfg?.TattooRemoveCost ?? -1;
        internal static int TattooColorChangeCost => _editorCfg?.TattooColorChangeCost ?? -1;


        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private readonly MySQLService _mySQL;

        private static EventService? _eventService;
        public static EventService EventService => _eventService ?? throw new InvalidOperationException("EventService is not loaded");

        public CharacterAppearanceService(MySQLService mySQL, ConfigurationService easyCfg, EventService evtService)
        {
            _mySQL = mySQL;
            _editorCfg = easyCfg.GetConfig<EditorConfig>();
            _eventService = evtService;

            EnsureTable();

            AvailableItems.CachePartsAsync();
            AvailableHeads.CollectAllCreatureHeadsAsync();
            AvailableWeapons.CacheAvailableWeaponPartsAndShieldModelsAsync();

            PrintFlags();
        }

        static void PrintFlags()
        {
            var str = "Character Appearance Editor Flags:";
            foreach(var e in Enum.GetValues<EditorFlags>())
            {
                if(e == EditorFlags.FreeOfCharge) continue;

                else if(e.HasFlag(EditorFlags.FreeOfCharge))
                {
                    str += $"\n{e}: {(int)(e & ~EditorFlags.FreeOfCharge)} (free of charge: {(int)e})";
                }

                else str += $"\n{e}: {(int)e} (free of charge: {(int)(e | EditorFlags.FreeOfCharge)})";
            }
            _log.Info(str);
        }

        [ScriptHandler("app_edit_nui")]
        ScriptHandleResult HandleEditorNUIRequest(CallInfo info)
        {
            var obj = info.ObjectSelf;
            if(obj == null || !obj.IsValid)
            {
                _log.Error("Null or invalid OBJECT_SELF");
                return ScriptHandleResult.NotHandled;
            }

            var locVar = obj.GetObjectVariable<LocalVariableInt>("AppearanceEditorFlags");

            int flags;

            NwCreature? pc;
            
            if (!locVar.HasValue || locVar.Value == 0)
            {
                if (!info.ScriptParams.IsSet("AppearanceEditorFlags") || !int.TryParse(info.ScriptParams["AppearanceEditorFlags"], out flags))
                {
                    _log.Error("Missing or invalid parameter");
                    return ScriptHandleResult.NotHandled;
                }
                pc = obj as NwCreature;
            }
            else
            {
                flags = locVar.Value;
                if ((obj as NwPlaceable) != null)
                {
                    pc = NWScript.GetLastUsedBy().ToNwObjectSafe<NwCreature>();

                    if(pc == null || !pc.IsValid)
                    {
                        _log.Error("Null or invalid PC");
                        return ScriptHandleResult.NotHandled;
                    }

                    if(flags <= 0 || flags > ushort.MaxValue || (((EditorFlags)flags & ~EditorFlags.All) != 0))
                    {                
                        _log.Error("Flags out of range");
                        return ScriptHandleResult.NotHandled;
                    }

                    if(pc.Level > 3 && ((EditorFlags)flags).HasFlag(EditorFlags.FreeOfCharge))
                    {
                        var p = pc.ControllingPlayer;
                        if(p == null || !p.IsValid)
                        {
                            _log.Error("Not controlled by valid player");
                            return ScriptHandleResult.NotHandled;
                        }

                        p.SendServerMessage("Darmowa edycja wyglądu jest dostępna wyłącznie poniżej 4-go poziomu postaci.", ColorConstants.Red);
                        return ScriptHandleResult.Handled;
                    }
                }
                else
                {
                    _log.Error("Not a placeable");
                    return ScriptHandleResult.NotHandled;
                }
            }

            NwPlayer? player;

            if (pc == null || !pc.IsValid)
            {
                _log.Error("Null or invalid PC");
                return ScriptHandleResult.NotHandled;
            }
            else if(!pc.IsLoginPlayerCharacter(out player))
            {
                _log.Error("Not a login player character");
                return ScriptHandleResult.NotHandled;
            }
            else if(player == null || !player.IsValid)
            {
                _log.Error("Null or invalid player");
                return ScriptHandleResult.NotHandled;
            }
            else if(flags <= 0 || flags > ushort.MaxValue || (((EditorFlags)flags & ~EditorFlags.All) != 0))
            {                
                _log.Error("Flags out of range");
                return ScriptHandleResult.NotHandled;
            }
            
            OpenAppearanceEditorNUI(player, (EditorFlags)flags);

            return ScriptHandleResult.Handled;
        }

        public static void OpenAppearanceEditorNUI(NwPlayer player, EditorFlags flags) => AppearanceEditorUI.Open(player, flags);
        public static event Action<NwPlayer, bool>? OnBodyAppearanceEditComplete;
        internal static void RaiseOnBodyAppearanceEditComplete(NwPlayer player, bool applyChanges) => OnBodyAppearanceEditComplete?.Invoke(player, applyChanges);

        private static readonly string _sqlSaveBodyColumns = $"{ServerData.DataProviders.IdentitySQLMap.ID}, {ServerData.DataProviders.BodyAppearanceSQLMap.Serialized}";
        private sealed class SerializableAppearance
        {
            public int Phenotype {get;set;} = 0;
            public Dictionary<int, int> Parts {get;set;} = new();
            public Dictionary<int, int> Colors {get;set;} = new();

            public SerializableAppearance(){}
            public SerializableAppearance(NwCreature creature)
            {
                var pheno = (int)creature.Phenotype;
    //                                                                  //          mounted phenos          // //   crawling pheno  //
                Phenotype = (pheno == (int)Anvil.API.Phenotype.Big || pheno == 25 || pheno == 8 || pheno == 5 || pheno == 73)? (int)Anvil.API.Phenotype.Big : (int)Anvil.API.Phenotype.Normal;

                Parts = new();
                foreach(var part in Enum.GetValues<CreaturePart>())
                    Parts.Add((int)part, creature.GetCreatureBodyPart(part));
                
                Colors = new();
                foreach(var channel in Enum.GetValues<ColorChannel>())
                    Colors.Add((int)channel, creature.GetColor(channel));
            }

            public void Apply(NwCreature creature)
            {
                var vt = creature.VisualTransform.Translation;
                if (creature.IsFlying())
                {
                    if(Phenotype == (int)Anvil.API.Phenotype.Big)
                        creature.Phenotype = (Phenotype)25;
                    else creature.Phenotype = (Phenotype)16;

                    if(creature.IsPixie())
                    {
                        creature.MovementRate = MovementRate.PC;
                        creature.VisualTransform.Translation = new System.Numerics.Vector3(vt.X, vt.Y, 1.0f);
                    }
                    else
                    {
                        creature.MovementRate = MovementRate.Slow;
                        creature.VisualTransform.Translation = new System.Numerics.Vector3(vt.X, vt.Y, 0.0f);
                    }
                }
                else
                {              
                    creature.MovementRate = creature.IsBrownie() ? MovementRate.Slow : MovementRate.PC;      
                    creature.VisualTransform.Translation = new System.Numerics.Vector3(vt.X, vt.Y, 0.0f);
                    var pheno = (int)creature.Phenotype;

                    if(
                        pheno != 3 && pheno != 5 && pheno != 6 && pheno != 8 && // mounted phenos
                        pheno != 72 && pheno != 73 // crawling phenos
                    )
                    {
                        creature.Phenotype = (Phenotype)Phenotype;
                    }
                }

                // fix tail for pixies if subrace has changed
                if (creature.IsBrownie() && creature.HasTail() && !creature.IsMischiefling()) creature.TailType = CreatureTailType.None;
                

                foreach(var kvp in Parts)
                    creature.SetCreatureBodyPart((CreaturePart)kvp.Key, kvp.Value);

                foreach(var kvp in Colors)
                    creature.SetColor((ColorChannel)kvp.Key, kvp.Value);
            }
        }

        public void SaveBodyAppearance(int identityID, NwCreature creature)
        {
            //_log.Warn("Saving identity " + identityID.ToString() + " appearance");
            FixInvalidBodyParts(creature);

            var app = new SerializableAppearance(creature);
            app.Apply(creature);

            var serialized = JsonSerializer.Serialize(app);

            _mySQL.QueryBuilder.InsertOrUpdate(
                ServerData.DataProviders.BodyAppearanceSQLMap.TableName,
                _sqlSaveBodyColumns,
                identityID, serialized
            );

            _ = _mySQL.ExecuteQuery();

        }

        /// <returns>False, if there is no appearance for this identity in database or an error occurred. Otherwise true.</returns>
        public bool LoadBodyAppearance(int identityID, NwCreature creature)
        {
            var bodyAppSQLMap = ServerData.DataProviders.BodyAppearanceSQLMap;
            var identitySQLMap = ServerData.DataProviders.IdentitySQLMap;

            //_log.Warn("Loading identity " + identityID.ToString() + " appearance");
            _mySQL.QueryBuilder.Select(bodyAppSQLMap.TableName, bodyAppSQLMap.Serialized)
            .Where(identitySQLMap.ID, identityID).Limit(1);

            using var result = _mySQL.ExecuteQuery();
            if (!result.HasData)
            {
                return false;
            }

            var serialized = result.First().Get<string>(0);

            if (string.IsNullOrEmpty(serialized))
            {
                _log.Error($"Character {creature.UUID.ToUUIDString()} body appearance \'{identityID}\' has no serialized data.");
                return false;
            }

            var app = JsonSerializer.Deserialize<SerializableAppearance>(serialized);

            if(app == null)
            {
                _log.Error("Failed to deserialize character body appearance");
                return false;
            }

            app.Apply(creature);

            FixInvalidBodyParts(creature);

            return true;
        }

        private static void FixInvalidBodyParts(NwCreature creature)
        {
            // ensure correct size for the creature
            var validScale = ServerData.DataProviders.BodyAppearanceProvider.GetMinMaxBodyHeightForCreature(creature);
            var scale = creature.VisualTransform.Scale;
            if(scale < validScale.Item1) creature.VisualTransform.Scale = validScale.Item1;
            if(scale > validScale.Item2) creature.VisualTransform.Scale = validScale.Item2;

            // ensure correct skin color for the creature
            var validSkinColors = ServerData.DataProviders.BodyAppearanceProvider.GetSkinColorsForCreature(creature);
            var skinColor = creature.GetColor(ColorChannel.Skin);

            if(validSkinColors.Count > 0 && !validSkinColors.Contains(skinColor))
                creature.SetColor(ColorChannel.Skin, validSkinColors[0]);

            //_log.Warn("FixInvalidBodyParts for gender " + creature.Gender.ToString() + " appType: " + creature.Appearance.RowIndex.ToString());
            var headID = creature.GetCreatureBodyPart(CreaturePart.Head);

            var heads = AvailableHeads.GetHeadsForCreature(creature);

            if(heads.Count > 0 && !heads.Contains(headID))
            {
                //_log.Warn("Fixing head from " + headID.ToString() + " to " + heads.First().ToString());
                creature.SetCreatureBodyPart(CreaturePart.Head, heads[0]);
            }
            

            var parts = ServerData.DataProviders.BodyAppearanceProvider.GetMiscellaneousBodyPartsForCreature(creature);

            if(parts == null || parts.Count == 0) return;

            var appType = creature.Appearance.RowIndex;

            bool isKobold = ServerData.DataProviders.BodyAppearanceProvider.IsKoboldAppearanceType(appType);
            
            foreach(var kvp in parts)
            {
                switch (kvp.Key)
                {
                    case CreaturePart.RightBicep:
                    case CreaturePart.RightForearm:{
                        var opposite = AppearanceEditorModel.GetOppositePart(kvp.Key);
                        
                        var bp = creature.GetCreatureBodyPart(kvp.Key);
                        var oppBp = creature.GetCreatureBodyPart(opposite);

                        if(!kvp.Value.Contains(bp) || !kvp.Value.Contains(oppBp))
                        {
                            if(!isKobold && kvp.Value.Contains(bp-1)) break;
                            creature.SetCreatureBodyPart(kvp.Key, kvp.Value.First());
                        }

                        if (!kvp.Value.Contains(oppBp))
                        {
                            if(!isKobold && kvp.Value.Contains(oppBp-1)) break;
                            creature.SetCreatureBodyPart(opposite, kvp.Value.First());
                        }
                    }
                    break;

                    case CreaturePart.Torso:
                    {
                        var bp = creature.GetCreatureBodyPart(kvp.Key);

                        if(!isKobold && ((creature.Gender == Gender.Female && bp == 202) || bp == 2)) break;

                        if (!kvp.Value.Contains(creature.GetCreatureBodyPart(kvp.Key)))
                        {
                            creature.SetCreatureBodyPart(kvp.Key, kvp.Value.First());
                        }
                    }
                    break;
                }
            }
        }
    }
}