using System.Runtime.CompilerServices;

namespace ExtensionsPlugin
{
    public static class ColorIndexExtensions
    {
        /// <summary>
        /// Based on 16/11 row-major GUI color chart grid: https://nwnlexicon.com/index.php?title=Color_Charts
        /// </summary>
        /// <param name="index2d">Color chart coordinates</param>
        /// <returns>Engine color ID</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Flatten(this (int, int) index2d) => (index2d.Item2 << 4) + index2d.Item1;

        /// <param name="index1d">Engine color ID</param>
        /// <returns>Color chart coordinates</returns>
        /// <inheritdoc cref="Flatten"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int, int) Inflate(this int index1d) => (index1d % 16, index1d / 16);
    }
}