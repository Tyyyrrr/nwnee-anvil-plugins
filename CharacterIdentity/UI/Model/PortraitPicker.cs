using System;
using System.Collections.Generic;
using System.Linq;
using Anvil.API;

namespace CharacterIdentity.UI.Model
{
    internal sealed class PortraitPicker
    {
        public readonly IReadOnlyList<string> Resrefs;

        private readonly int _columnCount;
        public readonly int RowCount;

        public PortraitPicker(IEnumerable<string> resRefs, int columnCount)
        {
            Resrefs = resRefs.Select(r => r + 'm').ToList();//.ToImmutableArray();

            _columnCount = columnCount;
            RowCount = (Resrefs.Count + columnCount - 1) / columnCount;
        }
        internal IEnumerable<string> GetColumn(int i) => Resrefs.Where((r, index) => (index % _columnCount) == i);

        
        public void SelectPortrait(int column, int row)
        {
            var index = row * View.PortraitPicker.ColumnCount + column;
            var resRef = Resrefs[Math.Min(index, Resrefs.Count - 1)];
            SelectedPortrait = resRef[..(resRef.Length - 1)] + 'h';
        }

        public void SelectPortrait(string newPortrait)
        {
            SelectedPortrait = newPortrait[..(newPortrait.Length - 1)] + 'h';
        }

        public string? SelectedPortrait { get; private set; } = null;
    }
}