using System;
using Enxada.Calendar;
using NUnit.Framework;

namespace Enxada.Tests.Logic.CalendarTests
{
    public class WeatherModelTests
    {
        [Test]
        public void StartsSunnyToday()
        {
            var weather = new WeatherModel();

            Assert.AreEqual(Weather.Sunny, weather.Today);
            Assert.IsFalse(weather.IsRainingToday);
        }

        [Test]
        public void AdvanceDay_TomorrowBecomesToday_AndANewForecastIsRolled()
        {
            var weather = new WeatherModel(Weather.Sunny, Weather.Rainy);
            var settings = new WeatherSettings(spring: 0.5f);

            weather.AdvanceDay(Season.Spring, settings, () => 0.1);

            Assert.AreEqual(Weather.Rainy, weather.Today);
            Assert.IsTrue(weather.IsRainingToday);
            Assert.AreEqual(Weather.Rainy, weather.Tomorrow); // 0,1 < 0,5
        }

        [Test]
        public void Roll_RainsOnlyBelowTheSeasonChance()
        {
            var settings = new WeatherSettings(spring: 0.2f, summer: 0.0f);
            var weather = new WeatherModel();

            weather.RollTomorrow(Season.Spring, settings, () => 0.19);
            Assert.AreEqual(Weather.Rainy, weather.Tomorrow);

            weather.RollTomorrow(Season.Spring, settings, () => 0.21);
            Assert.AreEqual(Weather.Sunny, weather.Tomorrow);

            weather.RollTomorrow(Season.Summer, settings, () => 0.0);
            Assert.AreEqual(Weather.Sunny, weather.Tomorrow); // chance 0 nunca chove
        }

        [Test]
        public void Changed_FiresOnAdvanceAndSet()
        {
            var weather = new WeatherModel();
            var changes = 0;
            weather.Changed += () => changes++;

            weather.AdvanceDay(Season.Spring, new WeatherSettings(), () => 0.9);
            weather.Set(Weather.Rainy, Weather.Sunny);

            Assert.AreEqual(2, changes);
            Assert.AreEqual(Weather.Rainy, weather.Today);
        }

        [Test]
        public void Settings_RejectInvalidChances()
        {
            Assert.Throws<ArgumentException>(() => new WeatherSettings(spring: 1.5f));
            Assert.Throws<ArgumentException>(() => new WeatherSettings(winter: -0.1f));
        }
    }
}
