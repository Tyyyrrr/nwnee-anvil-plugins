using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Anvil.API;
using Anvil.Services;

// #if DEBUG
// using NLog;
// #endif

namespace CharacterIdentity
{
    [ServiceBinding(typeof(PortraitStorageService))]
    public sealed class PortraitStorageService
    {
        private readonly Tuple<ImmutableArray<string>, ImmutableArray<string>> _vanillaPortraits;
        private readonly ImmutableArray<string> _customContent;


        public PortraitStorageService()
        {
            var portraits2da = NwGameTables.PortraitTable;

            var vanillaPts = new Tuple<List<string>, List<string>>(new(), new());

            int count = 0;
            foreach (var pt in portraits2da)
            {
                if (string.IsNullOrEmpty(pt.BaseResRef))
                    continue;

                string resRef = "po_" + pt.BaseResRef;

                if (!(resRef + 'l').IsValidScriptName(false))
                    continue;

                if (pt.Gender == Gender.Male)
                    vanillaPts.Item1.Add(resRef);
                else if (pt.Gender == Gender.Female)
                    vanillaPts.Item2.Add(resRef);
                else continue;
                count++;
            }

            _vanillaPortraits = new(vanillaPts.Item1.ToImmutableArray(), vanillaPts.Item2.ToImmutableArray());

            HashSet<string> customPts = new();

            var dir = Path.Combine(NwServer.Instance.UserDirectory, "portraits");

            foreach (var file in Directory.GetFiles(dir))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                customPts.Add(fileName[..(fileName.Length - 1)]);
            }

            _customContent = customPts.ToImmutableArray();
// #if DEBUG
//             //Debug();
// #endif
        }

// #if DEBUG
//         private void Debug()
//         {
//             string dbg = $"Portraits list ({Count})";

//             string male = "";
//             string female = "";
//             string custom = "";


//             foreach (var s in _vanillaPortraits.Item1) male += $"\n{s}";
//             foreach (var s in _vanillaPortraits.Item2) female += $"\n{s}";
//             foreach (var s in _customContent) custom += $"\n{s}";

//             dbg += "\nMale portraits:" + male + "\nFemale portraits:" + female + "\nCustom portraits:" + custom;

//             _log.Info(dbg);
//         }
//         private static readonly Logger _log = LogManager.GetCurrentClassLogger();
// #endif


        internal IEnumerable<string> GetPortraitsForCreature(NwCreature creature, Gender overrideGender = Gender.None)
        {
            Gender gen = creature.Gender;
            if(gen != overrideGender && (overrideGender == Gender.Male || overrideGender == Gender.Female))
                gen = overrideGender;

            if (gen == Gender.Male)
                return _vanillaPortraits.Item1.Concat(_customContent);

            return _vanillaPortraits.Item2.Concat(_customContent);
        }
        

    }
}