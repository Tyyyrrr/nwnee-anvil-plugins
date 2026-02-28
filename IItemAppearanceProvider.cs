using System.Collections.Generic;
using Anvil.API;

namespace ServerData
{
    public interface IItemAppearanceProvider
    {
        bool HelmIsValid(int model, Gender gender);
        bool NeckIsValid(int model, Gender gender);
        bool TorsoIsValid(int model, Gender gender);
        bool BeltIsValid(int model, Gender gender);
        bool PelvisIsValid(int model, Gender gender);
        bool RobeIsValid(int model, Gender gender);
        bool ShoulderIsValid(int model, Gender gender);
        bool BicepIsValid(int model, Gender gender);
        bool ForearmIsValid(int model, Gender gender);
        bool HandIsValid(int model, Gender gender);
        bool LegIsValid(int model, Gender gender);
        bool ShinIsValid(int model, Gender gender);
        bool FootIsValid(int model, Gender gender);
        bool CloakIsValid(int model, Gender gender);

        public Dictionary<BaseItemType, IReadOnlyList<int>> CollectShieldModels();

        public Dictionary<int, int[]>[] CollectWeaponModels(int itemType);
    }


}