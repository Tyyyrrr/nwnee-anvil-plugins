using System;
using System.Linq;
using Anvil.API;

using ExtensionsPlugin;


namespace CharacterIdentity.UI.Model
{
    internal sealed class IdentityEditor
    {   
        public readonly int MinimumAge;
        public readonly int MaximumAge;

        public int FullNameCharacters => FirstName.Length + LastName.Length + (LastName.Length == 0 ? 0 : 1);

        public bool IsEverythingProvided => ValidateState() && !string.IsNullOrEmpty(LastName) && !string.IsNullOrEmpty(Description);
        public bool IsDataValid => ValidateState();

        public readonly bool IsCreatingNew;

        private readonly bool _subraceCanChangeGender;
        public bool CanChangeGender => BluffRemainingForGenderChange <= 0 && SubraceCanChangeGender;
        public bool SubraceCanChangeGender => _subraceCanChangeGender;
        public readonly int BluffRemainingForGenderChange;

        private string _firstName = string.Empty;
        public string FirstName
        {
            get => _firstName;
            set
            {
                string val = value.TrimStart();

                if (val.Any(IsInvalidFirstNameCharacter)) return;

                val = val.Replace("  ", " ");

                if (val.Length == 0 || val.Length <= _firstName.Length || val.Length - _firstName.Length + FullNameCharacters <= CharacterIdentityService.IdentityEditorConfig.MaximumNameCharacters)
                {
                    _firstName = val;
                }
            }
        }


        private string _lastName = string.Empty;
        public string LastName
        {
            get => _lastName;
            set
            {
                string val = value.TrimStart();

                if (val.Any(IsInvalidLastNameCharacter)) return;
                
                val = val.Replace("  ", " ");

                if (val.Length == 0 || val.Length <= _lastName.Length || val.Length - (_lastName.Length > 0 ? _lastName.Length + 1 : 0) + FullNameCharacters <= CharacterIdentityService.IdentityEditorConfig.MaximumNameCharacters - 1)
                {
                    _lastName = val;
                }

            }
        }
        




        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set
            {
                string val = value.TrimStart();

                val = val.Replace("  ", " ");

                _description = val;
            }
        }


        private int _age;
        public int Age { get => _age; set => _age = Math.Clamp(value, MinimumAge, MaximumAge); }

        public Gender Gender {get;set;}

        private string _portrait;
        private readonly string _defaultPortrait;
        public string Portrait { get => _portrait; set => _portrait = IsValidPortrait(value) ? value : _defaultPortrait; } 


        public IdentityEditor(NwCreature pc, Identity initialData)
        {
            if (initialData.FirstName == null)
                initialData = Identity.Empty;

            var subRace = pc.SubRace.ToLower();
            _subraceCanChangeGender = subRace != "nocnica" && subRace != "rusałka" && subRace != "rusalka";

            IsCreatingNew = initialData.IsEmpty;

            MinimumAge = pc.GetMinimumAge();
            MaximumAge = pc.GetMaximumAge();

            BluffRemainingForGenderChange = Math.Max(0, 15 - pc.GetSkillRank(NwSkill.FromSkillType(Skill.Bluff)!, true)); // todo: move to .cfg

            _defaultPortrait = pc.GetDefaultPortraitResRef_Large(overrideGender: initialData.Gender);

            FirstName = initialData.FirstName;
            LastName = initialData.LastName;
            Description = initialData.Description;
            Gender = initialData.IsEmpty ? pc.Gender : initialData.Gender;

            _age = Math.Clamp(initialData.Age, MinimumAge, MaximumAge);
            _portrait = initialData.Portrait + 'l';
            if(!IsValidPortrait(_portrait)) 
                _portrait = _defaultPortrait;
        }

        private bool ValidateState()
        {
            return IsValidFirstName(FirstName)
                && IsValidLastName(LastName)
                && Age >= MinimumAge && Age <= MaximumAge
                && Portrait.IsValidScriptName(false)
                && Description.Length <= CharacterIdentityService.IdentityEditorConfig.MaximumDescriptionCharacters;
        }

        private static bool IsInvalidFirstNameCharacter(char c) => !(char.IsLetter(c) || char.IsAsciiLetter(c) || char.IsWhiteSpace(c) || c == '\'' || c == '`');
        private static bool IsValidFirstName(string? name)
        {
            if (
                string.IsNullOrEmpty(name)
                || name.Any(IsInvalidFirstNameCharacter)
            ) return false;
            
            return name.Length > 3 || (name.Length == 3 && !name.EndsWith(' '));
        }

        private static bool IsInvalidLastNameCharacter(char c) => !(char.IsLetter(c) || char.IsAsciiLetter(c) || char.IsWhiteSpace(c) || c == '\'' || c == '`');
        private static bool IsValidLastName(string? name)
        {
            if (string.IsNullOrEmpty(name)) return true;
            if (name.Any(IsInvalidLastNameCharacter)) return false;
            return true;
        }

        private static bool IsValidPortrait(string? portrait)
        {
            if (string.IsNullOrEmpty(portrait)) return false;

            if (!portrait.StartsWith("po_") || !portrait.EndsWith('l')) return false;

            return portrait.IsValidScriptName(false);
        }

        public Identity GetIdentity()
        {
            if (ValidateState())
            {
                _firstName = _firstName.Trim();
                _lastName = _lastName.Trim();

                _firstName = _firstName[0].ToString().ToUpper() + _firstName[1.._firstName.Length];

                return new(FirstName, LastName,  Description.Trim(), Age, Portrait.Trim()[..(Portrait.Length-1)], Gender);
            }
            return Identity.Empty;
        }
    }
}