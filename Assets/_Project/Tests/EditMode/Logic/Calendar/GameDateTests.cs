using System;
using Enxada.Calendar;
using NUnit.Framework;

namespace Enxada.Tests.Logic.CalendarTests
{
    public class GameDateTests
    {
        [Test]
        public void Start_IsSpringDayOneOfYearOne_OnAMonday()
        {
            var start = GameDate.Start;

            Assert.AreEqual(1, start.Year);
            Assert.AreEqual(Season.Spring, start.Season);
            Assert.AreEqual(1, start.Day);
            Assert.AreEqual(GameWeekday.Monday, start.Weekday);
            Assert.AreEqual(0, start.TotalDays);
        }

        [Test]
        public void NextDay_WithinSeason_AdvancesTheDay()
        {
            var next = new GameDate(1, Season.Spring, 5).NextDay();

            Assert.AreEqual(new GameDate(1, Season.Spring, 6), next);
        }

        [Test]
        public void NextDay_OnLastDayOfSeason_StartsNextSeason()
        {
            var next = new GameDate(1, Season.Spring, 28).NextDay();

            Assert.AreEqual(new GameDate(1, Season.Summer, 1), next);
        }

        [Test]
        public void NextDay_OnLastDayOfWinter_StartsNextYear()
        {
            var next = new GameDate(1, Season.Winter, 28).NextDay();

            Assert.AreEqual(new GameDate(2, Season.Spring, 1), next);
        }

        [TestCase(1, GameWeekday.Monday)]
        [TestCase(7, GameWeekday.Sunday)]
        [TestCase(8, GameWeekday.Monday)]
        [TestCase(28, GameWeekday.Sunday)]
        public void Weekday_RepeatsEverySevenDays(int day, GameWeekday expected)
        {
            Assert.AreEqual(expected, new GameDate(1, Season.Summer, day).Weekday);
        }

        [Test]
        public void TotalDays_RoundTripsThroughFromTotalDays()
        {
            for (var total = 0; total < GameDate.DaysPerSeason * GameDate.SeasonsPerYear * 3; total++)
                Assert.AreEqual(total, GameDate.FromTotalDays(total).TotalDays);
        }

        [Test]
        public void Constructor_RejectsInvalidValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameDate(0, Season.Spring, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameDate(1, Season.Spring, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameDate(1, Season.Spring, 29));
        }

        [Test]
        public void CompareTo_OrdersChronologically()
        {
            Assert.Less(new GameDate(1, Season.Spring, 28).CompareTo(new GameDate(1, Season.Summer, 1)), 0);
            Assert.Greater(new GameDate(2, Season.Spring, 1).CompareTo(new GameDate(1, Season.Winter, 28)), 0);
        }
    }
}
