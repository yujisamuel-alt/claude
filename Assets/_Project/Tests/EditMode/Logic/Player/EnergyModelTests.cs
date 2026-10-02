using System;
using Enxada.Player;
using NUnit.Framework;

namespace Enxada.Tests.Logic.Player
{
    public class EnergyModelTests
    {
        private static EnergyModel New() => new EnergyModel(new EnergySettings());

        [Test]
        public void Defaults_MatchTheDesignDocument()
        {
            var settings = new EnergySettings();

            Assert.AreEqual(270, settings.MaxEnergy);
            Assert.AreEqual(-15, settings.PassOutThreshold);
            Assert.AreEqual(270, New().Current);
        }

        [Test]
        public void Spend_ReducesEnergyAndNotifies()
        {
            var energy = New();
            var changes = 0;
            energy.Changed += () => changes++;

            energy.Spend(8);

            Assert.AreEqual(262, energy.Current);
            Assert.AreEqual(1, changes);
        }

        [TestCase(0)]
        [TestCase(-3)]
        public void Spend_IgnoresNonPositiveAmounts(int amount)
        {
            var energy = New();

            energy.Spend(amount);

            Assert.AreEqual(270, energy.Current);
        }

        [Test]
        public void IsDepleted_AtZeroOrLess_AndSpeedDrops()
        {
            var energy = New();
            Assert.IsFalse(energy.IsDepleted);
            Assert.AreEqual(1f, energy.SpeedMultiplier);

            energy.Spend(270);

            Assert.IsTrue(energy.IsDepleted);
            Assert.AreEqual(0.5f, energy.SpeedMultiplier);
            Assert.IsFalse(energy.IsPassedOut);
        }

        [Test]
        public void Exhausted_FiresOnlyBelowTheThreshold_AndOnlyOnce()
        {
            var energy = New();
            var fired = 0;
            energy.Exhausted += () => fired++;

            energy.Spend(270 + 15); // exatamente -15: ainda não desmaia
            Assert.AreEqual(0, fired);

            energy.Spend(1);
            Assert.AreEqual(1, fired);
            Assert.IsTrue(energy.IsPassedOut);

            energy.Spend(5);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void Fraction_IsClampedForTheBar()
        {
            var energy = New();
            Assert.AreEqual(1f, energy.Fraction, 1e-5f);

            energy.Spend(135);
            Assert.AreEqual(0.5f, energy.Fraction, 1e-5f);

            energy.Spend(200);
            Assert.AreEqual(0f, energy.Fraction);
        }

        [Test]
        public void Restore_NeverPassesTheMaximum()
        {
            var energy = New();
            energy.Spend(20);

            energy.Restore(100);

            Assert.AreEqual(270, energy.Current);
        }

        [Test]
        public void NewDay_AfterSleeping_RestoresEverything()
        {
            var energy = New();
            energy.Spend(200);

            energy.NewDay(passedOut: false);

            Assert.AreEqual(270, energy.Current);
        }

        [Test]
        public void NewDay_AfterPassingOut_RestoresHalf_AndCanExhaustAgain()
        {
            var energy = New();
            var fired = 0;
            energy.Exhausted += () => fired++;
            energy.Spend(300);
            Assert.AreEqual(1, fired);

            energy.NewDay(passedOut: true);
            Assert.AreEqual(135, energy.Current);

            energy.Spend(300);
            Assert.AreEqual(2, fired);
        }

        [Test]
        public void SetCurrent_ClampsToMax()
        {
            var energy = New();

            energy.SetCurrent(9999);

            Assert.AreEqual(270, energy.Current);
        }

        [Test]
        public void Settings_RejectInvalidValues()
        {
            Assert.Throws<ArgumentException>(() => new EnergySettings(maxEnergy: 0));
            Assert.Throws<ArgumentException>(() => new EnergySettings(passOutThreshold: 5));
            Assert.Throws<ArgumentException>(() => new EnergySettings(exhaustedSpeedMultiplier: 0f));
            Assert.Throws<ArgumentException>(() => new EnergySettings(passOutRecoveryFraction: 1.5f));
            Assert.Throws<ArgumentNullException>(() => new EnergyModel(null));
        }
    }
}
