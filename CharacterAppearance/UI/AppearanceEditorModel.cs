using System.Collections.Generic;
using Anvil.API;
using ExtensionsPlugin;

namespace CharacterAppearance.UI
{
    internal abstract class AppearanceEditorModel
    {
        /// <summary>
        /// Number of available BodyParts or Items, or WeaponParts to select in "SlotCombo"
        /// </summary>
        public abstract int MainEntriesCount { get; }
        /// <summary>
        /// Ordered strings to show in the "SlotCombo"
        /// </summary>
        public abstract IEnumerable<string> MainEntries { get; }

        // Currently selected BodyPart or Item or WeaponPart
        public abstract int MainSelection { get; set; }

        /// <summary>
        /// Number of available model IDs to select in "ValueCombo"
        /// </summary>
        public abstract int SubEntriesCount { get; }

        /// <summary>
        /// Ordered strings to show in the "ValueCombo"
        /// </summary>
        public abstract IEnumerable<string> SubEntries { get; }

        /// <summary>
        /// Currently selected Value
        /// </summary>
        public abstract int SubSelection { get; set; }

        /// <summary>
        /// Should always be a number from 0 to 5, or -1 on invalid
        /// </summary>
        public abstract int SelectedColorChannel {get;set;}

        /// <summary>
        /// Should be either 255, or a number from 0 to 175
        /// </summary>
        public abstract int SelectedColorIndex {get;set;}

        /// <summary>
        /// True if currently editing left part, false if currently editing right or non-double part
        /// </summary>
        public abstract bool LeftSide { get; set; }

        /// <summary>
        /// Is the current part exactly the same as the opposite? (True if it's not a double-part)
        /// </summary>
        public abstract bool IsSymmetrical { get; }

        /// <summary>
        /// Can current part be mirrored?
        /// </summary>
        public abstract bool IsDoublePart { get; }

        /// <summary>
        /// Did user make any changes?
        /// </summary>
        public abstract bool IsDirty {get;}

        public abstract int AppearanceChangeCost {get;}

        public abstract void CopyToTheOtherSide();

        /// <returns>
        /// Left part if current part is right part<br/>
        /// Right part if current part is left part <br/>
        /// Current part if it does not have the opposite (i.e. head, torso, belt...)</returns>
        public static CreaturePart GetOppositePart(CreaturePart toPart)
        {
            return toPart switch
            {
                CreaturePart.Head or CreaturePart.Neck or CreaturePart.Torso or CreaturePart.Robe or CreaturePart.Belt or CreaturePart.Pelvis => toPart,
                CreaturePart.RightThigh => CreaturePart.LeftThigh,
                CreaturePart.LeftThigh => CreaturePart.RightThigh,
                _ => ((int)toPart % 2 != 0) ? toPart - 1 : toPart + 1,
            };
        }
        
        /// <summary>(-1,-1) if there is no color overrides for current item, weapon part or armor part, or there is no selected color channel</summary>
        public (int, int) CurrentColorCoords
        {
            get
            {
                var colID = SelectedColorIndex;

                if(colID < 0 || colID >= 11*16)
                    return (-1,-1);

                return colID.Inflate();
            }
        }


        public enum ColorChart
        {
            Skin, Hair, Tattoo, Cloth, Leather, Metal
        }

        /// <summary>
        /// Color chart for currently selected part
        /// </summary>
        public abstract ColorChart CurrentColorChart {get;}

        public abstract bool IsValidColor(int colorID);

        public abstract void ApplyChanges();
        public abstract void RevertChanges();
    }
}