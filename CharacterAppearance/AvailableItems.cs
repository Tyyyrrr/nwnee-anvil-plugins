using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Anvil.API;

namespace CharacterAppearance
{
    internal static class AvailableItems
    {
        private static FrozenDictionary<int, List<int>>? _maleItems;
        private static FrozenDictionary<int, List<int>>? _femaleItems;

        public static IReadOnlyList<int> GetAvailableItemParts(int part, Gender gender)
        {
            if (_maleItems == null || _femaleItems == null) throw new InvalidOperationException("Requested list of available item parts, but they are not cached yet. Collecting them during server startup is mandatory.");
            if (part != -99 && !Enum.IsDefined(typeof(CreaturePart), part)) throw new IndexOutOfRangeException(nameof(part) + ": " + part.ToString());

            return gender == Gender.Male ? _maleItems[part] : _femaleItems[part];
        }

        private static Task<TimeSpan> CachePartsTask()
        {
            Stopwatch st = new();
            st.Start();

            var iap = ServerData.DataProviders.ItemAppearanceProvider;

            var maleDict = new Dictionary<int, List<int>>();
            var femaleDict = new Dictionary<int, List<int>>();

            List<int> maleList = new();
            List<int> femaleList = new();

            maleDict.Add((int)CreaturePart.Head, maleList);
            femaleDict.Add((int)CreaturePart.Head, femaleList);

            for (int i = 0; i < byte.MaxValue; i++)
            {
                if (iap.HelmIsValid(i, Gender.Male)) maleList.Add(i);
                if (iap.HelmIsValid(i, Gender.Female)) femaleList.Add(i);
            }

            maleList = new();
            femaleList = new();
            maleDict.Add((int)CreaturePart.Neck, maleList);
            femaleDict.Add((int)CreaturePart.Neck, femaleList);
            for (int i = 0; i < byte.MaxValue; i++)
            {
                if (iap.NeckIsValid(i, Gender.Male)) maleList.Add(i);
                if (iap.NeckIsValid(i, Gender.Female)) femaleList.Add(i);
            }

            maleList = new();
            femaleList = new();
            maleDict.Add((int)CreaturePart.Torso, maleList);
            femaleDict.Add((int)CreaturePart.Torso, femaleList);
            for (int i = 0; i < byte.MaxValue; i++)
            {
                if (iap.TorsoIsValid(i, Gender.Male)) maleList.Add(i);
                if (iap.TorsoIsValid(i, Gender.Female)) femaleList.Add(i);
            }

            maleList = new();
            femaleList = new();
            maleDict.Add((int)CreaturePart.Belt, maleList);
            femaleDict.Add((int)CreaturePart.Belt, femaleList);
            for (int i = 0; i < byte.MaxValue; i++)
            {
                if (iap.BeltIsValid(i, Gender.Male)) maleList.Add(i);
                if (iap.BeltIsValid(i, Gender.Female)) femaleList.Add(i);
            }


            maleList = new();
            femaleList = new();
            maleDict.Add((int)CreaturePart.Pelvis, maleList);
            femaleDict.Add((int)CreaturePart.Pelvis, femaleList);
            for (int i = 0; i < byte.MaxValue; i++)
            {
                if (iap.PelvisIsValid(i, Gender.Male)) maleList.Add(i);
                if (iap.PelvisIsValid(i, Gender.Female)) femaleList.Add(i);
            }


            maleList = new();
            femaleList = new();
            maleDict.Add((int)CreaturePart.Robe, maleList);
            femaleDict.Add((int)CreaturePart.Robe, femaleList);
            for (int i = 0; i < byte.MaxValue; i++)
            {
                if (iap.RobeIsValid(i, Gender.Male)) maleList.Add(i);
                if (iap.RobeIsValid(i, Gender.Female)) femaleList.Add(i);
            }


            maleList = new();
            femaleList = new();
            maleDict.Add((int)CreaturePart.RightShoulder, maleList);
            femaleDict.Add((int)CreaturePart.RightShoulder, femaleList);
            for (int i = 0; i < byte.MaxValue; i++)
            {
                if (iap.ShoulderIsValid(i, Gender.Male)) maleList.Add(i);
                if (iap.ShoulderIsValid(i, Gender.Female)) femaleList.Add(i);
            }


            maleList = new();
            femaleList = new();
            maleDict.Add((int)CreaturePart.RightBicep, maleList);
            femaleDict.Add((int)CreaturePart.RightBicep, femaleList);
            for (int i = 0; i < byte.MaxValue; i++)
            {
                if (iap.BicepIsValid(i, Gender.Male)) maleList.Add(i);
                if (iap.BicepIsValid(i, Gender.Female)) femaleList.Add(i);
            }


            maleList = new();
            femaleList = new();
            maleDict.Add((int)CreaturePart.RightForearm, maleList);
            femaleDict.Add((int)CreaturePart.RightForearm, femaleList);
            for (int i = 0; i < byte.MaxValue; i++)
            {
                if (iap.ForearmIsValid(i, Gender.Male)) maleList.Add(i);
                if (iap.ForearmIsValid(i, Gender.Female)) femaleList.Add(i);
            }


            maleList = new();
            femaleList = new();
            maleDict.Add((int)CreaturePart.RightHand, maleList);
            femaleDict.Add((int)CreaturePart.RightHand, femaleList);
            for (int i = 0; i < byte.MaxValue; i++)
            {
                if (iap.HandIsValid(i, Gender.Male)) maleList.Add(i);
                if (iap.HandIsValid(i, Gender.Female)) femaleList.Add(i);
            }


            maleList = new();
            femaleList = new();
            maleDict.Add((int)CreaturePart.RightThigh, maleList);
            femaleDict.Add((int)CreaturePart.RightThigh, femaleList);
            for (int i = 0; i < byte.MaxValue; i++)
            {
                if (iap.LegIsValid(i, Gender.Male)) maleList.Add(i);
                if (iap.LegIsValid(i, Gender.Female)) femaleList.Add(i);
            }


            maleList = new();
            femaleList = new();
            maleDict.Add((int)CreaturePart.RightShin, maleList);
            femaleDict.Add((int)CreaturePart.RightShin, femaleList);
            for (int i = 0; i < byte.MaxValue; i++)
            {
                if (iap.ShinIsValid(i, Gender.Male)) maleList.Add(i);
                if (iap.ShinIsValid(i, Gender.Female)) femaleList.Add(i);
            }

            maleList = new();
            femaleList = new();
            maleDict.Add((int)CreaturePart.RightFoot, maleList);
            femaleDict.Add((int)CreaturePart.RightFoot, femaleList);
            for (int i = 0; i < byte.MaxValue; i++)
            {
                if (iap.FootIsValid(i, Gender.Male)) maleList.Add(i);
                if (iap.FootIsValid(i, Gender.Female)) femaleList.Add(i);
            }


            maleList = new();
            femaleList = new();
            maleDict.Add(-99, maleList);
            femaleDict.Add(-99, femaleList);
            for (int i = 0; i < byte.MaxValue; i++)
            {
                if (iap.CloakIsValid(i, Gender.Male)) maleList.Add(i);
                if (iap.CloakIsValid(i, Gender.Female)) femaleList.Add(i);
            }


            _maleItems = maleDict.ToFrozenDictionary();
            _femaleItems = femaleDict.ToFrozenDictionary();

            st.Stop();

            return Task.FromResult(st.Elapsed);
        }

        public static async void CachePartsAsync()
        {
            if (_maleItems != null) return;

            var span = await NwTask.Run(CachePartsTask);

            NLog.LogManager.GetCurrentClassLogger().Info("\nAvailable item parts collected in " + span.TotalMilliseconds.ToString() + "ms");
        }


        private static int[] GetAllTorsoPartsMatchingAC(int AC)
        {
            var tab = NwGameTables.PartsChestTable;

            return tab.Where(e=>e.ACBonus == AC).Select(e=>e.RowIndex).ToArray();
        }

        public static IReadOnlyList<int>GetAvailableTorsoParts(NwItem item, Gender gender)
        {
            if(_maleItems == null || _femaleItems == null) return Array.Empty<int>();

            var allParts = GetAllTorsoPartsMatchingAC(item.BaseACValue);

            return allParts.Intersect(gender == Gender.Male ? _maleItems[(int)CreaturePart.Torso] : _femaleItems[(int)CreaturePart.Torso]).ToList();
        }
    }
}