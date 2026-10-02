using System;
using System.Collections.Generic;
using System.Linq;
using Enxada.Core;
using Enxada.Farming;
using NUnit.Framework;

namespace Enxada.Tests.Logic.FarmingTests
{
    public class WateringCanAndSpawnTests
    {
        private static Func<double> Sequence(params double[] values)
        {
            var index = 0;
            return () => values[index++ % values.Length];
        }

        [Test]
        public void WateringCan_StartsFullAndEmptiesOneByOne()
        {
            var can = new WateringCanState(3);
            Assert.AreEqual(3, can.Level);

            Assert.IsTrue(can.TryUse());
            Assert.IsTrue(can.TryUse());
            Assert.IsTrue(can.TryUse());
            Assert.IsFalse(can.TryUse());
            Assert.IsFalse(can.HasWater);
        }

        [Test]
        public void WateringCan_RefillRestoresCapacity_AndNotifiesOnlyOnChange()
        {
            var can = new WateringCanState(40);
            var changes = 0;
            can.Changed += () => changes++;

            can.Refill();
            Assert.AreEqual(0, changes);

            can.TryUse();
            can.Refill();
            Assert.AreEqual(40, can.Level);
            Assert.AreEqual(2, changes);
        }

        [Test]
        public void WateringCan_SetLevelIsClamped()
        {
            var can = new WateringCanState(40);

            can.SetLevel(-5);
            Assert.AreEqual(0, can.Level);

            can.SetLevel(500);
            Assert.AreEqual(40, can.Level);
            Assert.Throws<ArgumentOutOfRangeException>(() => new WateringCanState(0));
        }

        [Test]
        public void PickCells_ReturnsDistinctCandidates()
        {
            var candidates = Enumerable.Range(0, 20).Select(i => new CellPosition(i, 0)).ToList();

            var picked = SpawnPlanner.PickCells(candidates, 10, Sequence(0.1, 0.9, 0.5, 0.3, 0.7));

            Assert.AreEqual(10, picked.Count);
            Assert.AreEqual(10, picked.Distinct().Count());
            CollectionAssert.IsSubsetOf(picked, candidates);
        }

        [Test]
        public void PickCells_NeverExceedsTheCandidates_AndHandlesEmptyInput()
        {
            var candidates = new List<CellPosition> { new CellPosition(1, 1), new CellPosition(2, 2) };

            Assert.AreEqual(2, SpawnPlanner.PickCells(candidates, 10, () => 0.999999).Count);
            Assert.AreEqual(0, SpawnPlanner.PickCells(new List<CellPosition>(), 3, () => 0.5).Count);
            Assert.AreEqual(0, SpawnPlanner.PickCells(candidates, 0, () => 0.5).Count);
            Assert.AreEqual(0, SpawnPlanner.PickCells(null, 3, () => 0.5).Count);
        }

        [Test]
        public void PickCells_DoesNotChangeTheInputList()
        {
            var candidates = Enumerable.Range(0, 5).Select(i => new CellPosition(i, 0)).ToList();
            var copy = new List<CellPosition>(candidates);

            SpawnPlanner.PickCells(candidates, 3, () => 0.0);

            CollectionAssert.AreEqual(copy, candidates);
        }

        [TestCase(0.0, 0)]
        [TestCase(0.24, 0)]
        [TestCase(0.26, 1)]
        [TestCase(0.99, 2)]
        public void PickWeightedIndex_FollowsTheWeights(double roll, int expected)
        {
            var weights = new[] { 1f, 1f, 2f };

            Assert.AreEqual(expected, SpawnPlanner.PickWeightedIndex(weights, () => roll));
        }

        [Test]
        public void PickWeightedIndex_SkipsZeroWeights_AndReportsNoOptions()
        {
            Assert.AreEqual(1, SpawnPlanner.PickWeightedIndex(new[] { 0f, 3f, 0f }, () => 0.99));
            Assert.AreEqual(-1, SpawnPlanner.PickWeightedIndex(new[] { 0f, 0f }, () => 0.5));
            Assert.AreEqual(-1, SpawnPlanner.PickWeightedIndex(new float[0], () => 0.5));
        }

        [Test]
        public void RollCount_UsesTheFractionAsAChance()
        {
            Assert.AreEqual(3, SpawnPlanner.RollCount(3f, () => 0.99));
            Assert.AreEqual(3, SpawnPlanner.RollCount(2.5f, () => 0.2));
            Assert.AreEqual(2, SpawnPlanner.RollCount(2.5f, () => 0.7));
            Assert.AreEqual(0, SpawnPlanner.RollCount(0f, () => 0.0));
            Assert.AreEqual(0, SpawnPlanner.RollCount(-4f, () => 0.0));
        }
    }
}
