using System;

namespace Enxada.Calendar
{
    /// <summary>Chaves da tabela de textos para estações e dias da semana.</summary>
    public static class TextKeys
    {
        public static string Season(Season season)
        {
            switch (season)
            {
                case Calendar.Season.Spring: return "season.spring";
                case Calendar.Season.Summer: return "season.summer";
                case Calendar.Season.Autumn: return "season.autumn";
                case Calendar.Season.Winter: return "season.winter";
                default: throw new ArgumentOutOfRangeException(nameof(season));
            }
        }

        public static string WeekdayShort(GameWeekday weekday) => "weekday." + weekday.ToString().ToLowerInvariant() + ".short";
        public static string Weekday(GameWeekday weekday) => "weekday." + weekday.ToString().ToLowerInvariant();
    }
}
