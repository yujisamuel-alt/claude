using System;

namespace Enxada.Calendar
{
    /// <summary>Conjunto de estações (uma cultura pode crescer em várias).</summary>
    [Flags]
    public enum SeasonFlags
    {
        None = 0,
        Spring = 1,
        Summer = 2,
        Autumn = 4,
        Winter = 8,
        All = Spring | Summer | Autumn | Winter
    }

    public static class SeasonFlagsExtensions
    {
        public static bool Includes(this SeasonFlags flags, Season season) =>
            (flags & (SeasonFlags)(1 << (int)season)) != 0;
    }
}
