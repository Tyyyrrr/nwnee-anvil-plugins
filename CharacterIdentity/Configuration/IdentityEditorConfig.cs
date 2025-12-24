using EasyConfig;

namespace CharacterIdentity.Configuration
{
    [ConfigFile("Editor")]
    public sealed class IdentityEditorConfig : IConfig
    {
        public ushort MinimumNameCharacters { get; set; } = 3;
        public ushort MaximumNameCharacters { get; set; } = 32;
        public ushort MaximumDescriptionCharacters { get; set; } = ushort.MaxValue;

        public void Coerce(){}

        public bool IsValid(out string? error)
        {
            error = "";

            if (MinimumNameCharacters < 1 || MinimumNameCharacters > 3)
                error += $"{nameof(MinimumNameCharacters)} out of inclusive range 1-3\n";

            if (MaximumNameCharacters < MinimumNameCharacters || MaximumNameCharacters > 32)
                error += $"{nameof(MaximumNameCharacters)} out of inclusive range {MinimumNameCharacters}-32\n";

            error = error == string.Empty ? null : error;
            return error == null;
        }
    }
}