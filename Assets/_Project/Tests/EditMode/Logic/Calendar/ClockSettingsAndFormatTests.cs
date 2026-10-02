using System;
using Enxada.Calendar;
using NUnit.Framework;

namespace Enxada.Tests.Logic.CalendarTests
{
    public class ClockSettingsAndFormatTests
    {
        [Test]
        public void Defaults_MatchTheDesignDocument()
        {
            var settings = new ClockSettings();

            Assert.AreEqual(6 * 60, settings.DayStartMinute);
            Assert.AreEqual(26 * 60, settings.DayEndMinute);
            Assert.AreEqual(24 * 60, settings.LateWarningMinute);
            Assert.AreEqual(10, settings.TickMinutes);
            Assert.AreEqual(7f, settings.RealSecondsPerTick);
            Assert.AreEqual(0.10f, settings.PassOutMoneyPercent);
            Assert.AreEqual(1000, settings.PassOutMoneyCap);
        }

        [Test]
        public void Constructor_RejectsInvalidSettings()
        {
            Assert.Throws<ArgumentException>(() => new ClockSettings(dayStartMinute: 600, dayEndMinute: 600));
            Assert.Throws<ArgumentException>(() => new ClockSettings(lateWarningMinute: 100));
            Assert.Throws<ArgumentException>(() => new ClockSettings(tickMinutes: 0));
            Assert.Throws<ArgumentException>(() => new ClockSettings(realSecondsPerTick: 0f));
            Assert.Throws<ArgumentException>(() => new ClockSettings(passOutMoneyPercent: 1.5f));
            Assert.Throws<ArgumentException>(() => new ClockSettings(passOutMoneyCap: -1));
        }

        [TestCase(6 * 60, "06:00")]
        [TestCase(13 * 60 + 40, "13:40")]
        [TestCase(24 * 60, "00:00")]
        [TestCase(25 * 60 + 30, "01:30")]
        [TestCase(26 * 60, "02:00")]
        public void Format24h_WrapsAfterMidnight(int minute, string expected)
        {
            Assert.AreEqual(expected, ClockFormat.Format24h(minute));
        }

        [TestCase(0, 0)]
        [TestCase(-50, 0)]
        [TestCase(500, 50)]
        [TestCase(5000, 500)]
        [TestCase(10000, 1000)]
        [TestCase(1000000, 1000)]
        public void PassOutPenalty_IsTenPercentCappedAtOneThousand(int money, int expected)
        {
            Assert.AreEqual(expected, PassOutPenalty.Calculate(money, new ClockSettings()));
        }

        [Test]
        public void PassOutPenalty_NeverTakesMoreThanThePlayerHas()
        {
            var settings = new ClockSettings(passOutMoneyPercent: 1f, passOutMoneyCap: 5000);

            Assert.AreEqual(300, PassOutPenalty.Calculate(300, settings));
        }

        [Test]
        public void TextKeys_AreStableAndLowercase()
        {
            Assert.AreEqual("season.spring", TextKeys.Season(Season.Spring));
            Assert.AreEqual("weekday.monday", TextKeys.Weekday(GameWeekday.Monday));
            Assert.AreEqual("weekday.saturday.short", TextKeys.WeekdayShort(GameWeekday.Saturday));
        }
    }
}
