using System;
using System.Collections.Generic;
using Enxada.Calendar;
using Enxada.Core;
using Enxada.Farming;
using NUnit.Framework;

namespace Enxada.Tests.Logic.FarmingTests
{
    public class CropTests
    {
        private static readonly CellPosition Cell = new CellPosition(2, 2);

        private class Catalog : ICropCatalog
        {
            private readonly Dictionary<string, CropSpec> _crops = new Dictionary<string, CropSpec>();

            public Catalog(params CropSpec[] specs)
            {
                foreach (var spec in specs)
                    _crops[spec.Id] = spec;
            }

            public bool TryGet(string cropId, out CropSpec spec) => _crops.TryGetValue(cropId, out spec);

            public bool TryGetBySeed(string seedItemId, out CropSpec spec)
            {
                foreach (var candidate in _crops.Values)
                {
                    if (candidate.SeedItemId != seedItemId)
                        continue;

                    spec = candidate;
                    return true;
                }

                spec = null;
                return false;
            }
        }

        // Alface: 4 dias, colhe uma vez. Feijão: 10 dias, rebrota a cada 3. Os dois só na Primavera.
        private static readonly CropSpec Lettuce = new CropSpec("lettuce", "lettuce_seed", "lettuce",
            new[] { 1, 1, 1, 1 }, 0, SeasonFlags.Spring);

        private static readonly CropSpec Bean = new CropSpec("bean", "bean_seed", "bean",
            new[] { 2, 2, 3, 3 }, 3, SeasonFlags.Spring);

        private static readonly Catalog Crops = new Catalog(Lettuce, Bean);

        private static FarmGrid TilledGrid()
        {
            var grid = new FarmGrid();
            grid.Till(Cell);
            return grid;
        }

        private static void Night(FarmGrid grid, bool watered, Season season = Season.Spring)
        {
            if (watered)
                grid.Water(Cell);
            grid.AdvanceDay(Crops, season, false, () => 0.99, 0.0);
        }

        // ------------------------------------------------------------------ CropSpec

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(3, 3)]
        [TestCase(4, 4)]
        [TestCase(99, 4)]
        public void StageIndex_Lettuce_OneStagePerDay(int daysGrown, int expected)
        {
            Assert.AreEqual(expected, Lettuce.StageIndex(daysGrown));
        }

        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(2, 1)]
        [TestCase(4, 2)]
        [TestCase(7, 3)]
        [TestCase(10, 4)]
        public void StageIndex_Bean_FollowsTheStageLengths(int daysGrown, int expected)
        {
            Assert.AreEqual(expected, Bean.StageIndex(daysGrown));
        }

        [Test]
        public void Spec_ExposesTotals()
        {
            Assert.AreEqual(4, Lettuce.TotalDays);
            Assert.AreEqual(5, Lettuce.SpriteCount);
            Assert.AreEqual(10, Bean.TotalDays);
            Assert.IsTrue(Bean.Regrows);
            Assert.IsFalse(Lettuce.Regrows);
            Assert.IsTrue(Lettuce.IsMature(4));
            Assert.IsFalse(Lettuce.IsMature(3));
        }

        [Test]
        public void Spec_RejectsInvalidData()
        {
            Assert.Throws<ArgumentException>(() => new CropSpec("", "s", "h", new[] { 1 }, 0, SeasonFlags.Spring));
            Assert.Throws<ArgumentException>(() => new CropSpec("a", "s", "h", new int[0], 0, SeasonFlags.Spring));
            Assert.Throws<ArgumentException>(() => new CropSpec("a", "s", "h", new[] { 0 }, 0, SeasonFlags.Spring));
            Assert.Throws<ArgumentException>(() => new CropSpec("a", "s", "h", new[] { 2 }, 2, SeasonFlags.Spring));
            Assert.Throws<ArgumentException>(() => new CropSpec("a", "s", "h", new[] { 2 }, 0, SeasonFlags.None));
        }

        [Test]
        public void SeasonFlags_IncludesOnlyTheMarkedSeasons()
        {
            var flags = SeasonFlags.Spring | SeasonFlags.Autumn;

            Assert.IsTrue(flags.Includes(Season.Spring));
            Assert.IsFalse(flags.Includes(Season.Summer));
            Assert.IsTrue(flags.Includes(Season.Autumn));
            Assert.IsFalse(flags.Includes(Season.Winter));
            Assert.IsTrue(SeasonFlags.All.Includes(Season.Winter));
        }

        // ------------------------------------------------------------------ plantar

        [Test]
        public void Plant_OnTilledSoil_Works()
        {
            var grid = TilledGrid();

            Assert.AreEqual(PlantResult.Planted, grid.TryPlant(Cell, Lettuce, Season.Spring));
            Assert.AreEqual("lettuce", grid.GetCrop(Cell).CropId);
            Assert.IsTrue(grid.IsOccupied(Cell));
        }

        [Test]
        public void Plant_RejectsUntilledOccupiedAndWrongSeason()
        {
            var grid = new FarmGrid();
            Assert.AreEqual(PlantResult.NotTilled, grid.TryPlant(Cell, Lettuce, Season.Spring));

            grid.Till(Cell);
            Assert.AreEqual(PlantResult.WrongSeason, grid.TryPlant(Cell, Lettuce, Season.Summer));

            grid.TryPlant(Cell, Lettuce, Season.Spring);
            Assert.AreEqual(PlantResult.Occupied, grid.TryPlant(Cell, Bean, Season.Spring));
        }

        // ------------------------------------------------------------------ crescer

        [Test]
        public void Growth_OnlyHappensOnWateredDays()
        {
            var grid = TilledGrid();
            grid.TryPlant(Cell, Lettuce, Season.Spring);

            Night(grid, watered: false);
            Night(grid, watered: false);
            Assert.AreEqual(0, grid.GetCrop(Cell).DaysGrown);

            Night(grid, watered: true);
            Assert.AreEqual(1, grid.GetCrop(Cell).DaysGrown);
        }

        [Test]
        public void Lettuce_PlantedOnDayOne_IsReadyOnDayFive()
        {
            var grid = TilledGrid();
            grid.TryPlant(Cell, Lettuce, Season.Spring); // dia 1

            for (var night = 1; night <= 3; night++)
            {
                Night(grid, watered: true);
                Assert.IsFalse(Lettuce.IsMature(grid.GetCrop(Cell).DaysGrown), $"noite {night}");
            }

            Night(grid, watered: true); // 4ª noite: amanhece o dia 5
            Assert.IsTrue(Lettuce.IsMature(grid.GetCrop(Cell).DaysGrown));
        }

        [Test]
        public void Growth_StopsWhenMature()
        {
            var grid = TilledGrid();
            grid.TryPlant(Cell, Lettuce, Season.Spring);

            for (var i = 0; i < 8; i++)
                Night(grid, watered: true);

            Assert.AreEqual(4, grid.GetCrop(Cell).DaysGrown);
        }

        [Test]
        public void Rain_WateringTonightMeansGrowthTomorrowNight()
        {
            var grid = TilledGrid();
            grid.TryPlant(Cell, Lettuce, Season.Spring);

            grid.AdvanceDay(Crops, Season.Spring, true, () => 0.99, 0.0); // vai chover amanhã
            Assert.IsTrue(grid.IsWatered(Cell));

            grid.AdvanceDay(Crops, Season.Spring, false, () => 0.99, 0.0); // fim do dia chuvoso
            Assert.AreEqual(1, grid.GetCrop(Cell).DaysGrown);
        }

        [Test]
        public void Growth_ReportsWhichPlantsGrew()
        {
            var grid = TilledGrid();
            grid.TryPlant(Cell, Lettuce, Season.Spring);
            grid.Water(Cell);

            var result = grid.AdvanceDay(Crops, Season.Spring, false, () => 0.99, 0.0);

            CollectionAssert.AreEqual(new[] { Cell }, result.Grown);
        }

        // ------------------------------------------------------------------ colher

        [Test]
        public void Harvest_BeforeMaturity_Fails()
        {
            var grid = TilledGrid();
            grid.TryPlant(Cell, Lettuce, Season.Spring);

            Assert.IsFalse(grid.TryHarvest(Cell, Crops).Success);
            Assert.IsTrue(grid.IsOccupied(Cell));
        }

        [Test]
        public void Harvest_SingleHarvestCrop_RemovesThePlantButKeepsTheSoil()
        {
            var grid = TilledGrid();
            grid.TryPlant(Cell, Lettuce, Season.Spring);
            for (var i = 0; i < 4; i++)
                Night(grid, watered: true);

            var result = grid.TryHarvest(Cell, Crops);

            Assert.IsTrue(result.Success);
            Assert.AreEqual("lettuce", result.ItemId);
            Assert.AreEqual(1, result.Amount);
            Assert.IsTrue(result.CropRemoved);
            Assert.IsTrue(grid.IsTilled(Cell));
            Assert.IsFalse(grid.IsOccupied(Cell));
        }

        [Test]
        public void Harvest_RegrowingCrop_GoesBackAndMaturesAgainInRegrowDays()
        {
            var grid = TilledGrid();
            grid.TryPlant(Cell, Bean, Season.Spring);
            for (var i = 0; i < 10; i++)
                Night(grid, watered: true);

            var result = grid.TryHarvest(Cell, Crops);

            Assert.IsTrue(result.Success);
            Assert.IsFalse(result.CropRemoved);
            Assert.AreEqual(7, grid.GetCrop(Cell).DaysGrown);
            Assert.IsFalse(grid.TryHarvest(Cell, Crops).Success);

            for (var i = 0; i < 2; i++)
                Night(grid, watered: true);
            Assert.IsFalse(Bean.IsMature(grid.GetCrop(Cell).DaysGrown));

            Night(grid, watered: true); // 3º dia regado depois da colheita
            Assert.IsTrue(grid.TryHarvest(Cell, Crops).Success);
        }

        // ------------------------------------------------------------------ estações

        [Test]
        public void SeasonChange_KillsPlantsThatDoNotGrowInTheNewSeason()
        {
            var grid = TilledGrid();
            grid.TryPlant(Cell, Lettuce, Season.Spring);
            grid.Water(Cell);

            var result = grid.AdvanceDay(Crops, Season.Summer, false, () => 0.99, 0.0);

            Assert.IsFalse(grid.IsOccupied(Cell));
            Assert.IsTrue(grid.IsTilled(Cell));
            CollectionAssert.AreEqual(new[] { Cell }, result.Died);
            Assert.AreEqual(0, result.Grown.Count);
        }

        [Test]
        public void PlantThatJustDied_LeavesTheSoilForTheNightEvenIfRevertWouldHit()
        {
            var grid = TilledGrid();
            grid.TryPlant(Cell, Lettuce, Season.Spring);

            grid.AdvanceDay(Crops, Season.Summer, false, () => 0.0, 1.0);

            Assert.IsTrue(grid.IsTilled(Cell));
        }

        [Test]
        public void UnknownCrop_IsRemovedInsteadOfCrashing()
        {
            var grid = new FarmGrid();
            grid.Till(Cell);
            grid.Restore(Cell, new FarmTile { Crop = new CropState { CropId = "fantasma", DaysGrown = 2 } });

            var result = grid.AdvanceDay(Crops, Season.Spring, false, () => 0.99, 0.0);

            Assert.IsFalse(grid.IsOccupied(Cell));
            CollectionAssert.AreEqual(new[] { Cell }, result.Died);
        }
    }
}
