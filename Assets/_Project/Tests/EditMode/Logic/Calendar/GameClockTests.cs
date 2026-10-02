using System;
using System.Collections.Generic;
using Enxada.Calendar;
using NUnit.Framework;

namespace Enxada.Tests.Logic.CalendarTests
{
    public class GameClockTests
    {
        private static GameClock NewClock() => new GameClock(new ClockSettings(), GameDate.Start);

        [Test]
        public void NewClock_StartsAtSixInTheMorning()
        {
            var clock = NewClock();

            Assert.AreEqual(6 * 60, clock.MinuteOfDay);
            Assert.AreEqual(6, clock.Hour);
            Assert.AreEqual(0, clock.Minute);
        }

        [Test]
        public void Tick_BelowOneBlock_DoesNotAdvance()
        {
            var clock = NewClock();

            clock.Tick(6.9f);

            Assert.AreEqual(6 * 60, clock.MinuteOfDay);
        }

        [Test]
        public void Tick_AdvancesTenMinutesPerSevenSeconds()
        {
            var clock = NewClock();

            clock.Tick(7f);
            Assert.AreEqual(6 * 60 + 10, clock.MinuteOfDay);

            clock.Tick(14f);
            Assert.AreEqual(6 * 60 + 30, clock.MinuteOfDay);
        }

        [Test]
        public void Tick_AccumulatesAcrossSmallFrames()
        {
            var clock = NewClock();

            for (var i = 0; i < 100; i++)
                clock.Tick(0.07f);

            Assert.AreEqual(6 * 60 + 10, clock.MinuteOfDay);
        }

        [TestCase(0f)]
        [TestCase(-5f)]
        [TestCase(float.NaN)]
        public void Tick_IgnoresInvalidDelta(float delta)
        {
            var clock = NewClock();

            clock.Tick(delta);

            Assert.AreEqual(6 * 60, clock.MinuteOfDay);
        }

        [Test]
        public void Changed_FiresOncePerBlock()
        {
            var clock = NewClock();
            var calls = 0;
            clock.Changed += () => calls++;

            clock.Tick(21f);

            Assert.AreEqual(3, calls);
        }

        [Test]
        public void LateWarning_FiresOnceAtMidnight()
        {
            var clock = NewClock();
            var warnings = 0;
            clock.LateWarning += () => warnings++;

            clock.Restore(GameDate.Start, 23 * 60 + 50);
            clock.Tick(7f);
            Assert.AreEqual(1, warnings);
            Assert.IsTrue(clock.IsAfterMidnight);

            clock.Tick(7f);
            Assert.AreEqual(1, warnings);
        }

        [Test]
        public void PassOutReached_FiresAtTwoAndClockStops()
        {
            var clock = NewClock();
            var passOuts = 0;
            clock.PassOutReached += () => passOuts++;

            clock.Restore(GameDate.Start, 25 * 60 + 50);
            clock.Tick(7f);

            Assert.AreEqual(1, passOuts);
            Assert.AreEqual(26 * 60, clock.MinuteOfDay);
            Assert.IsTrue(clock.IsWaitingForDayEnd);

            clock.Tick(100f);
            Assert.AreEqual(1, passOuts);
            Assert.AreEqual(26 * 60, clock.MinuteOfDay);
        }

        [Test]
        public void Tick_WithHugeDelta_StopsAtDayEnd()
        {
            var clock = NewClock();
            var passOuts = 0;
            clock.PassOutReached += () => passOuts++;

            clock.Tick(100000f);

            Assert.AreEqual(1, passOuts);
            Assert.AreEqual(26 * 60, clock.MinuteOfDay);
        }

        [Test]
        public void EndDay_AdvancesDateAndResetsTheClock()
        {
            var clock = NewClock();
            clock.Restore(GameDate.Start, 22 * 60);

            var info = clock.EndDay(DayEndReason.Slept);

            Assert.AreEqual(new GameDate(1, Season.Spring, 2), clock.Date);
            Assert.AreEqual(6 * 60, clock.MinuteOfDay);
            Assert.AreEqual(GameDate.Start, info.EndedDate);
            Assert.AreEqual(DayEndReason.Slept, info.Reason);
            Assert.IsFalse(info.SeasonChanged);
        }

        [Test]
        public void EndDay_RaisesDayEndedWithTheNewStateAlreadyApplied()
        {
            var clock = NewClock();
            DayEndedInfo? received = null;
            GameDate dateSeenByListener = default;
            clock.DayEnded += info =>
            {
                received = info;
                dateSeenByListener = clock.Date;
            };

            clock.EndDay(DayEndReason.PassedOut);

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(DayEndReason.PassedOut, received.Value.Reason);
            Assert.AreEqual(received.Value.NewDate, dateSeenByListener);
        }

        [Test]
        public void EndDay_OnLastDayOfSpring_ReportsSeasonChange()
        {
            var clock = new GameClock(new ClockSettings(), new GameDate(1, Season.Spring, 28));

            var info = clock.EndDay(DayEndReason.Slept);

            Assert.IsTrue(info.SeasonChanged);
            Assert.AreEqual(Season.Spring, info.EndedDate.Season);
            Assert.AreEqual(Season.Summer, info.NewDate.Season);
        }

        [Test]
        public void EndDay_AfterPassOut_LetsTheClockRunAgainAndWarnAgain()
        {
            var clock = NewClock();
            var warnings = 0;
            clock.LateWarning += () => warnings++;
            clock.Restore(GameDate.Start, 26 * 60);

            clock.EndDay(DayEndReason.PassedOut);
            Assert.IsFalse(clock.IsWaitingForDayEnd);

            clock.Tick(7f);
            Assert.AreEqual(6 * 60 + 10, clock.MinuteOfDay);

            clock.Restore(clock.Date, 23 * 60 + 50);
            clock.Tick(7f);
            Assert.AreEqual(1, warnings);
        }

        [Test]
        public void Restore_ClampsToTheDayRange()
        {
            var clock = NewClock();

            clock.Restore(GameDate.Start, 0);
            Assert.AreEqual(6 * 60, clock.MinuteOfDay);

            clock.Restore(GameDate.Start, 99999);
            Assert.AreEqual(26 * 60, clock.MinuteOfDay);
            Assert.IsTrue(clock.IsWaitingForDayEnd);
        }

        [Test]
        public void DayProgress_GoesFromZeroToOne()
        {
            var clock = NewClock();
            Assert.AreEqual(0f, clock.DayProgress, 1e-5f);

            clock.Restore(GameDate.Start, 16 * 60);
            Assert.AreEqual(0.5f, clock.DayProgress, 1e-5f);

            clock.Restore(GameDate.Start, 26 * 60);
            Assert.AreEqual(1f, clock.DayProgress, 1e-5f);
        }

        [Test]
        public void DayProgress_MovesSmoothlyBetweenBlocks()
        {
            var clock = NewClock();
            var before = clock.DayProgress;

            clock.Tick(3.5f);

            Assert.Greater(clock.DayProgress, before);
            Assert.AreEqual(6 * 60, clock.MinuteOfDay);
        }

        [Test]
        public void FullSeason_TakesTwentyEightSleeps()
        {
            var clock = NewClock();
            var seasonsChanged = new List<Season>();
            clock.DayEnded += info =>
            {
                if (info.SeasonChanged) seasonsChanged.Add(info.EndedDate.Season);
            };

            for (var i = 0; i < 28; i++)
                clock.EndDay(DayEndReason.Slept);

            Assert.AreEqual(new GameDate(1, Season.Summer, 1), clock.Date);
            CollectionAssert.AreEqual(new[] { Season.Spring }, seasonsChanged);
        }

        [Test]
        public void Constructor_RejectsNullSettings()
        {
            Assert.Throws<ArgumentNullException>(() => new GameClock(null, GameDate.Start));
        }
    }
}
