using System;
using Anvil.API;

namespace CharacterIdentity.UI.Model
{
    internal sealed class IdentitySelector
    {
        public readonly IdentityInfo TrueIdentity;
        public IdentityInfo CurrentIdentity { get; private set; }
        public IdentityInfo SelectedIdentity => Identities[_selectedIndex];

        public bool CanCreate => HowManyIdentitiesCanBeCreated > 0;
        public bool CanEdit => CanPick;
        public bool CanPick => HasFalseIdentities && SelectedIdentity != CurrentIdentity;
        public bool CanRestore => CurrentIdentity != TrueIdentity;

        public bool HasFalseIdentities => Identities.Length > 0;
        public readonly int HowManyIdentitiesCanBeCreated;
        public readonly string WhyCreationIsDisabled;



        private int _selectedIndex;
        public int SelectedIndex => _selectedIndex;

        public IdentityInfo[] Identities;

        public void SelectFalseIdentity(int index)
        {
            if (Identities.Length == 0) throw new InvalidOperationException("Empty array");

            if (Identities.Length <= index) index = Identities.Length - 1;

            _selectedIndex = index;
            
        }
        
        public void PickIdentity() => CurrentIdentity = SelectedIdentity;
        public void RestoreOriginal() => CurrentIdentity = TrueIdentity;



        public IdentitySelector(NwPlayer player, IdentityInfo[] falseIdentities, IdentityInfo trueIdentity, int activeIdentityId)
        {
            if (activeIdentityId < 1)
                throw new ArgumentException("Invalid index of currently active identity", nameof(activeIdentityId));


            TrueIdentity = trueIdentity;

            Identities = falseIdentities;

            if (activeIdentityId == trueIdentity.ID)
            {
                _selectedIndex = 0;
                CurrentIdentity = TrueIdentity;
            }
            else
            {
                _selectedIndex = -1;
                for (int i = 0; i < falseIdentities.Length; i++)
                {

                    if (falseIdentities[i].ID == activeIdentityId)
                    {
                        _selectedIndex = i;
                        break;
                    }
                }
                if (_selectedIndex < 0) throw new InvalidOperationException("There is no active identity in false identities array");

                CurrentIdentity = SelectedIdentity;
            }




            var skillRanks = IdentityManager.GetIdentityRank(player.ControlledCreature!);

            var maxIdentities = CharacterIdentityService.ServiceConfig.MaximumFalseIdentities;

            
            if (maxIdentities <= Identities.Length)
            {
                WhyCreationIsDisabled = $"Osiągnięto limit fałszywych tożsamości ({maxIdentities})";
                HowManyIdentitiesCanBeCreated = 0;
            }
            else
            {
                int allowed = Math.Min(maxIdentities, skillRanks / CharacterIdentityService.ServiceConfig.BluffRanksPerIdentity);
                int available = allowed - Identities.Length;


                if (available > 0)
                {
                    WhyCreationIsDisabled = string.Empty;
                    HowManyIdentitiesCanBeCreated = available;
                }
                else
                {
                    int missingRanks = CharacterIdentityService.ServiceConfig.BluffRanksPerIdentity - (skillRanks % CharacterIdentityService.ServiceConfig.BluffRanksPerIdentity);
                    WhyCreationIsDisabled = $"Brakujące punkty blefu: {missingRanks}";
                    HowManyIdentitiesCanBeCreated = 0;
                }
            }



        }
    }
}