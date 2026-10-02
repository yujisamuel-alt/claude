using System;

namespace Enxada.Calendar
{
    public enum Season
    {
        Spring = 0,
        Summer = 1,
        Autumn = 2,
        Winter = 3
    }

    /// <summary>Dia da semana. Como a estação tem 28 dias (4 semanas), o dia 1 é sempre segunda-feira.</summary>
    public enum GameWeekday
    {
        Monday = 0,
        Tuesday = 1,
        Wednesday = 2,
        Thursday = 3,
        Friday = 4,
        Saturday = 5,
        Sunday = 6
    }

    /// <summary>Data do jogo: ano (a partir de 1), estação e dia (1–28).</summary>
    public readonly struct GameDate : IEquatable<GameDate>, IComparable<GameDate>
    {
        public const int DaysPerSeason = 28;
        public const int SeasonsPerYear = 4;
        public const int DaysPerWeek = 7;

        public readonly int Year;
        public readonly Season Season;
        public readonly int Day;

        public GameDate(int year, Season season, int day)
        {
            if (year < 1)
                throw new ArgumentOutOfRangeException(nameof(year), "O ano começa em 1.");
            if (day < 1 || day > DaysPerSeason)
                throw new ArgumentOutOfRangeException(nameof(day), $"O dia vai de 1 a {DaysPerSeason}.");
            if (season < Season.Spring || season > Season.Winter)
                throw new ArgumentOutOfRangeException(nameof(season));

            Year = year;
            Season = season;
            Day = day;
        }

        public static GameDate Start => new GameDate(1, Season.Spring, 1);

        public GameWeekday Weekday => (GameWeekday)((Day - 1) % DaysPerWeek);

        /// <summary>Dias desde o dia 1 da Primavera do ano 1 (que é o 0). Útil para save e aniversários.</summary>
        public int TotalDays => ((Year - 1) * SeasonsPerYear + (int)Season) * DaysPerSeason + (Day - 1);

        public static GameDate FromTotalDays(int totalDays)
        {
            if (totalDays < 0)
                throw new ArgumentOutOfRangeException(nameof(totalDays));

            var day = totalDays % DaysPerSeason + 1;
            var seasonIndex = totalDays / DaysPerSeason;
            return new GameDate(seasonIndex / SeasonsPerYear + 1, (Season)(seasonIndex % SeasonsPerYear), day);
        }

        public GameDate NextDay() => FromTotalDays(TotalDays + 1);

        public bool Equals(GameDate other) => TotalDays == other.TotalDays;
        public override bool Equals(object obj) => obj is GameDate other && Equals(other);
        public override int GetHashCode() => TotalDays;
        public int CompareTo(GameDate other) => TotalDays.CompareTo(other.TotalDays);
        public override string ToString() => $"Ano {Year}, {Season} {Day}";

        public static bool operator ==(GameDate a, GameDate b) => a.Equals(b);
        public static bool operator !=(GameDate a, GameDate b) => !a.Equals(b);
    }
}
