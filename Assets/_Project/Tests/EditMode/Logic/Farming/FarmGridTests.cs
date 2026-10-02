using System;
using System.Collections.Generic;
using Enxada.Calendar;
using Enxada.Core;
using Enxada.Farming;
using NUnit.Framework;

namespace Enxada.Tests.Logic.FarmingTests
{
    public class FarmGridTests
    {
        private static readonly CellPosition A = new CellPosition(3, 4);
        private static readonly CellPosition B = new CellPosition(5, 4);

        private class FakeCatalog : ICropCatalog
        {
            public readonly CropSpec Lettuce = new CropSpec("lettuce", "lettuce_seed", "lettuce", new[] { 1, 1, 1, 1 }, 0,
                SeasonFlags.Spring);

            public bool TryGet(string cropId, out CropSpec spec)
            {
                spec = cropId == "lettuce" ? Lettuce : null;
                return spec != null;
            }

            public bool TryGetBySeed(string seedItemId, out CropSpec spec) => TryGet("lettuce", out spec);
        }

        private static readonly FakeCatalog Crops = new FakeCatalog();

        private static Func<double> Rolls(params double[] values)
        {
            var queue = new Queue<double>(values);
            return () => queue.Dequeue();
        }

        [Test]
        public void Till_MarksTheTile_OnlyOnce()
        {
            var grid = new FarmGrid();

            Assert.IsTrue(grid.Till(A));
            Assert.IsFalse(grid.Till(A));
            Assert.IsTrue(grid.IsTilled(A));
            Assert.IsFalse(grid.IsTilled(B));
            Assert.AreEqual(1, grid.TilledCount);
        }

        [Test]
        public void Water_RequiresTilledSoil_AndNotAlreadyWatered()
        {
            var grid = new FarmGrid();

            Assert.IsFalse(grid.Water(A));

            grid.Till(A);
            Assert.IsTrue(grid.Water(A));
            Assert.IsTrue(grid.IsWatered(A));
            Assert.IsFalse(grid.Water(A));
        }

        [Test]
        public void TileChanged_FiresOnTillAndWater()
        {
            var grid = new FarmGrid();
            var changed = new List<CellPosition>();
            grid.TileChanged += changed.Add;

            grid.Till(A);
            grid.Water(A);
            grid.Till(A); // sem efeito

            CollectionAssert.AreEqual(new[] { A, A }, changed);
        }

        [Test]
        public void AdvanceDay_DriesTheSoil()
        {
            var grid = new FarmGrid();
            grid.Till(A);
            grid.Water(A);

            var result = grid.AdvanceDay(Crops, Season.Spring, false, Rolls(0.99), 0.1);

            Assert.IsFalse(grid.IsWatered(A));
            Assert.IsTrue(grid.IsTilled(A));
            CollectionAssert.AreEqual(new[] { A }, result.Refreshed);
            Assert.AreEqual(0, result.Reverted.Count);
        }

        [Test]
        public void AdvanceDay_WhenItRained_EverythingIsWatered()
        {
            var grid = new FarmGrid();
            grid.Till(A);
            grid.Till(B);

            grid.AdvanceDay(Crops, Season.Spring, true, Rolls(0.99, 0.99), 0.1);

            Assert.IsTrue(grid.IsWatered(A));
            Assert.IsTrue(grid.IsWatered(B));
        }

        [Test]
        public void AdvanceDay_BareSoilCanRevertToGrass()
        {
            var grid = new FarmGrid();
            grid.Till(A);

            var result = grid.AdvanceDay(Crops, Season.Spring, false, Rolls(0.05), 0.1);

            Assert.IsFalse(grid.IsTilled(A));
            CollectionAssert.AreEqual(new[] { A }, result.Reverted);
        }

        [Test]
        public void AdvanceDay_SoilWithAPlantNeverReverts()
        {
            var grid = new FarmGrid();
            grid.Till(A);
            grid.TryPlant(A, Crops.Lettuce, Season.Spring);

            var result = grid.AdvanceDay(Crops, Season.Spring, false, () => 0.0, 1.0);

            Assert.IsTrue(grid.IsTilled(A));
            Assert.AreEqual(0, result.Reverted.Count);
        }

        [Test]
        public void AdvanceDay_ChanceZeroNeverReverts_ChanceOneAlwaysReverts()
        {
            var never = new FarmGrid();
            never.Till(A);
            never.AdvanceDay(Crops, Season.Spring, false, () => 0.0, 0.0);
            Assert.IsTrue(never.IsTilled(A));

            var always = new FarmGrid();
            always.Till(A);
            always.AdvanceDay(Crops, Season.Spring, false, () => 0.999999, 1.0);
            Assert.IsFalse(always.IsTilled(A));
        }

        [Test]
        public void Restore_BringsBackSavedTiles()
        {
            var grid = new FarmGrid();

            grid.Restore(A, new FarmTile { Watered = true });

            Assert.IsTrue(grid.IsTilled(A));
            Assert.IsTrue(grid.IsWatered(A));
        }
    }
}
