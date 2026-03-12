using System.Collections.Generic;
using System.Linq;
using Anvil.API;
using Anvil.Services;

namespace DMUtils.VFXSpawner
{
    internal sealed class VFXSpawnerModel
    {
        private static readonly Dictionary<string, int> _vfxes = new();
        public static IReadOnlyDictionary<string,int> GetTemporaryVFXes() => _vfxes;
        private static readonly Dictionary<string, int> _persistentVfxes = new();
        public static IReadOnlyDictionary<string,int> GetPersistentVFXes() => _persistentVfxes;


        static VFXSpawnerModel()
        {
            foreach(var entry in NwGameTables.VisualEffectTable)
            {
                var label = entry.Label;
                var idx = entry.RowIndex;
                if(!string.IsNullOrEmpty(label) && !label.Contains('*'))
                { 
                    _vfxes.TryAdd(label,idx);
                }
            }

            foreach(var entry in NwGameTables.PersistentEffectTable)
            {
                var label = entry.Label;
                var idx = entry.RowIndex;
                if(!string.IsNullOrEmpty(label) && !label.Contains('*'))
                {
                    _persistentVfxes.TryAdd(label,idx);
                }
            }
        }

        private readonly NwPlayer _player;
        private readonly NwObject? _target;
        private Location? _location;
        public VFXSpawnerModel(NwObject target,NwPlayer player)
        {
            _player = player;
            _target = target;
        }
        public VFXSpawnerModel(Location location,NwPlayer player)
        {
            _player = player;
            _location = location;
        }

        public int SelectedVFXID {get;set;} = -1;
        public int DurationSeconds {get;set;} = -1;

        static void ClearOldEffect(NwObject target)
        {
            var e = NWN.Core.NWScript.GetFirstEffect(target.ObjectId);
            nint eId = default;
            while(NWN.Core.NWScript.GetIsEffectValid(e) > 0)
            {
                if(NWN.Core.NWScript.GetEffectTag(e) == "DM_CREATED_VFX")
                {
                    eId = e;
                    break;
                }
            }
            if(eId != default)
            {
                NWN.Core.NWScript.RemoveEffect(target.ObjectId, eId);
            }
        }

        public void SpawnVFX()
        {

            if(_location != null)
            {
                _player.SendServerMessage("Nakładanie VFX na obszar jest jeszcze niedostępne".ColorString(ColorConstants.Red));
                return;
                // if(SelectedVFXID < 0) return;
                
                // var effect = NWN.Core.NWScript.EffectVisualEffect(SelectedVFXID);
                // effect = NWN.Core.NWScript.TagEffect(effect, "DM_CREATED_VFX");
                
                // var loc = NWN.Core.NWScript.Location(_location.Area.ObjectId,_location.Position,_location.Rotation);

                // if(DurationSeconds == 0)
                // {
                //     NWN.Core.NWScript.ApplyEffectAtLocation(NWN.Core.NWScript.DURATION_TYPE_INSTANT,effect,loc,0);
                //     return;
                // }
                
                // if(DurationSeconds < 0)
                // {
                //     var plc = NwPlaceable.Create("x0_bread"/*or whatever*/, _location,false,"DM_PLC_VFX_HOLDER")!;
                //     plc.VisibilityOverride = VisibilityMode.DMOnly;
                //     plc.VisualTransform.Scale=0.01f;
                //     NWN.Core.NWScript.ApplyEffectToObject(NWN.Core.NWScript.DURATION_TYPE_PERMANENT,effect,plc.ObjectId);
                // }
                // else
                // {
                //     var plc = NwPlaceable.Create("x0_bread"/*or whatever*/, _location,false,"DM_PLC_VFX_HOLDER")!;
                //     plc.VisibilityOverride = VisibilityMode.DMOnly;
                //     plc.VisualTransform.Scale=0.01f;
                //     NWN.Core.NWScript.ApplyEffectToObject(NWN.Core.NWScript.DURATION_TYPE_PERMANENT,effect,plc.ObjectId);
                //     NWN.Core.NWScript.DestroyObject(plc.ObjectId, DurationSeconds);
                // }
            }
            else if(_target != null)
            {
                if(_target.Tag == "DM_PLC_VFX_HOLDER"){
                    var loc = NWN.Core.NWScript.GetLocation(_target.ObjectId);
                    NWN.Core.NWScript.DestroyObject(_target);
                    _location = loc;
                    SpawnVFX();
                    return;
                }
                else ClearOldEffect(_target);

                if(SelectedVFXID == -1)
                    return;
                
                var effect = NWN.Core.NWScript.EffectVisualEffect(SelectedVFXID);
                effect = NWN.Core.NWScript.TagEffect(effect, "DM_CREATED_VFX");

                NWN.Core.NWScript.ApplyEffectToObject(NWN.Core.NWScript.DURATION_TYPE_PERMANENT,effect,_target.ObjectId);

                if(DurationSeconds < 0) return;

                float dur = DurationSeconds;

                if(dur == 0) dur+=1;

                NWN.Core.NWScript.DelayCommand(dur,()=>ClearOldEffect(_target));
            }
        }
    }
}