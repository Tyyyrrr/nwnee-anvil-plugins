namespace CharacterAppearance.UI
{
    internal static partial class AppearanceEditorView
    {
        #region Element size
        public const int SingleColorSquareSize = 16;

        private const int ArrowBtnWidth = 20;
        private const int ArrowBtnHeight = ArrowBtnWidth;

        private const int GeneralButtonWidth = 105;
        private const int GeneralButtonHeight = 35;


        private const int ApplyCancelButtonWidth = 150;
        private const int ApplyCancelButtonHeight = (PhenotypeCheckBoxHeight * 2 + PhenotypeLabelHeight) / 2;

        private const int ColorImageHeight = 20;
        private const int ColorImageWidth = 20;

        private const int PhenotypeLabelWidth = ApplyCancelButtonWidth - 15;
        private const int PhenotypeLabelHeight = 20;

        private const int ValueComboWidth = 100;
        private const int ValueComboHeight = 20;
        private const int SlotComboWidth = 175;
        private const int SlotComboHeight = 25;

        private const int SymmetryButtonWidth = 80;
        private const int SymmetryButtonHeight = SlotComboHeight;

        private const int LRSideButtonWidth = 130;
        private const int LRSideButtonHeight = 25;

        private const int PhenotypeCheckBoxWidth = PhenotypeLabelWidth;
        private const int PhenotypeCheckBoxHeight = PhenotypeLabelHeight;

        private const int BodyHeightSliderWidth = PhenotypeLabelWidth + 30;
        private const int BodyHeightSliderHeight = PhenotypeLabelHeight;
        #endregion

        #region Equipment color
        // included in bs3_nui.hak
        public const string COLOR_PALETTE_CLOTH = "bs_pal_cloth";// "mvpal_cloth.bmp" from x2patch.bif in .tga format
        public const string COLOR_PALETTE_LEATHER = "bs_pal_leather"; //mvpal_leather.bmp" from x2patch.bif in .tga format
        public const string COLOR_PALETTE_METAL = "bs_pal_armor";//"mvpal_armor01.bmp" from x2patch.bif in .tga format

        public const string CLOTH1_TTIP = "Materiał I";
        public const string CLOTH2_TTIP = "Materiał II";
        public const string LEATHER1_TTIP = "Skóra I";
        public const string LEATHER2_TTIP = "Skóra II";
        public const string METAL1_TTIP = "Metal I";
        public const string METAL2_TTIP = "Metal II";

        public const string CLOTH1_PREFIX = "Cloth1";
        public const string CLOTH2_PREFIX = "Cloth2";
        public const string LEATHER1_PREFIX = "Leather1";
        public const string LEATHER2_PREFIX = "Leather2";
        public const string METAL1_PREFIX = "Metal1";
        public const string METAL2_PREFIX = "Metal2";

        public static readonly string[] EqColorTooltips = new string[6] { CLOTH1_TTIP,CLOTH2_TTIP,LEATHER1_TTIP,LEATHER2_TTIP,METAL1_TTIP,METAL2_TTIP };
        public static readonly string[] EqColorPrefixes = new string[6] { CLOTH1_PREFIX, CLOTH2_PREFIX, LEATHER1_PREFIX, LEATHER2_PREFIX, METAL1_PREFIX, METAL2_PREFIX };
        public static readonly string[] EqColorPalettes = new string[6] { COLOR_PALETTE_CLOTH, COLOR_PALETTE_CLOTH, COLOR_PALETTE_LEATHER, COLOR_PALETTE_LEATHER, COLOR_PALETTE_METAL, COLOR_PALETTE_METAL };
        #endregion

        #region Body color
        public const string COLOR_PALETTE_SKIN = "gui_pal_skin";
        public const string COLOR_PALETTE_HAIR = "gui_pal_hair01";
        public const string COLOR_PALETTE_TATTOO = "gui_pal_tattoo";
        #endregion
    }
}