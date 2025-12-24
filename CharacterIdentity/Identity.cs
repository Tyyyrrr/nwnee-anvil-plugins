using Anvil.API;

namespace CharacterIdentity
{
    internal readonly struct Identity
    {
        public static readonly Identity Empty = new(string.Empty);
        public bool IsEmpty => FirstName == string.Empty;

        public Identity(string firstName, string? lastname = null, string? description = null, int age = -1, string? portrait = null, Gender gender = default)
        {
            FirstName = firstName;
            LastName = lastname ?? string.Empty;
            Age = age;
            Gender = gender == Gender.Male ? gender : Gender.Female;
            Description = description ?? string.Empty;
            Portrait = portrait ?? string.Empty;
        }

        public readonly string FirstName;
        public readonly string LastName;        
        public readonly int Age;
        public readonly Gender Gender;
        public readonly string Description;

        public readonly string Portrait;
    }
}