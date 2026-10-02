using System;
using System.Collections.Generic;
using Enxada.Inventory;
using NUnit.Framework;

namespace Enxada.Tests.Logic.InventoryTests
{
    public class InventoryModelTests
    {
        private class FakeCatalog : IItemCatalog
        {
            private readonly Dictionary<string, int> _maxStacks = new Dictionary<string, int>
            {
                { "seed", 999 }, { "wood", 999 }, { "hoe", 1 }, { "small", 5 }
            };

            public bool TryGetMaxStack(string itemId, out int maxStack) => _maxStacks.TryGetValue(itemId, out maxStack);
        }

        private static InventoryModel New(int hotbar = 3, int backpack = 3) =>
            new InventoryModel(new FakeCatalog(), hotbar, backpack);

        private static ItemStack S(string id, int quantity, ItemQuality quality = ItemQuality.Normal) =>
            new ItemStack(id, quantity, quality);

        // ------------------------------------------------------------------ ItemStack

        [Test]
        public void ItemStack_WithZeroQuantityOrNoId_IsEmpty()
        {
            Assert.IsTrue(new ItemStack("seed", 0).IsEmpty);
            Assert.IsTrue(new ItemStack(null, 5).IsEmpty);
            Assert.IsTrue(new ItemStack("", 5).IsEmpty);
            Assert.IsTrue(ItemStack.Empty.IsEmpty);
            Assert.AreEqual(ItemStack.Empty, new ItemStack("seed", -3));
        }

        [Test]
        public void ItemStack_QualityKeepsStacksApart()
        {
            Assert.IsTrue(S("seed", 1).CanStackWith(S("seed", 9)));
            Assert.IsFalse(S("seed", 1).CanStackWith(S("seed", 1, ItemQuality.Gold)));
            Assert.IsFalse(S("seed", 1).CanStackWith(S("wood", 1)));
            Assert.IsFalse(ItemStack.Empty.CanStackWith(ItemStack.Empty));
        }

        // ------------------------------------------------------------------ TryAdd

        [Test]
        public void TryAdd_FillsTheFirstEmptySlot_HotbarFirst()
        {
            var inventory = New();

            var leftover = inventory.TryAdd(S("seed", 10));

            Assert.IsTrue(leftover.IsEmpty);
            Assert.AreEqual(S("seed", 10), inventory[0]);
        }

        [Test]
        public void TryAdd_CompletesExistingStackBeforeUsingNewSlot()
        {
            var inventory = New();
            inventory.TryAdd(S("seed", 990));

            inventory.TryAdd(S("seed", 20));

            Assert.AreEqual(999, inventory[0].Quantity);
            Assert.AreEqual(S("seed", 11), inventory[1]);
        }

        [Test]
        public void TryAdd_ToolsNeverStack()
        {
            var inventory = New();

            inventory.TryAdd(S("hoe", 1));
            inventory.TryAdd(S("hoe", 1));

            Assert.AreEqual(S("hoe", 1), inventory[0]);
            Assert.AreEqual(S("hoe", 1), inventory[1]);
        }

        [Test]
        public void TryAdd_DifferentQualityUsesAnotherSlot()
        {
            var inventory = New();

            inventory.TryAdd(S("seed", 5));
            inventory.TryAdd(S("seed", 5, ItemQuality.Gold));

            Assert.AreEqual(S("seed", 5), inventory[0]);
            Assert.AreEqual(S("seed", 5, ItemQuality.Gold), inventory[1]);
        }

        [Test]
        public void TryAdd_WhenFull_ReturnsTheLeftover()
        {
            var inventory = New(1, 1); // 2 slots de "small" (máx. 5) = 10 unidades

            var leftover = inventory.TryAdd(S("small", 13));

            Assert.AreEqual(S("small", 3), leftover);
            Assert.AreEqual(S("small", 5), inventory[0]);
            Assert.AreEqual(S("small", 5), inventory[1]);
        }

        [Test]
        public void TryAdd_UnknownItem_Throws()
        {
            Assert.Throws<ArgumentException>(() => New().TryAdd(S("fantasma", 1)));
        }

        [Test]
        public void TryAdd_EmptyStack_DoesNothing()
        {
            var inventory = New();
            var changes = 0;
            inventory.Changed += () => changes++;

            Assert.IsTrue(inventory.TryAdd(ItemStack.Empty).IsEmpty);
            Assert.AreEqual(0, changes);
        }

        // ------------------------------------------------------------------ RoomFor / Count / Remove

        [Test]
        public void RoomFor_CountsPartialStacksAndEmptySlots()
        {
            var inventory = New(1, 1);
            inventory.TryAdd(S("small", 3));

            Assert.AreEqual(2 + 5, inventory.RoomFor(S("small", 100)));
            Assert.AreEqual(5, inventory.RoomFor(S("small", 1, ItemQuality.Silver)));
            Assert.IsTrue(inventory.CanAdd(S("small", 1)));
        }

        [Test]
        public void RoomFor_FullInventory_IsZero()
        {
            var inventory = New(1, 0);
            inventory.TryAdd(S("hoe", 1));

            Assert.AreEqual(0, inventory.RoomFor(S("hoe", 1)));
            Assert.IsFalse(inventory.CanAdd(S("hoe", 1)));
        }

        [Test]
        public void Count_SumsAllSlotsAndQualities()
        {
            var inventory = New();
            inventory.TryAdd(S("seed", 4));
            inventory.TryAdd(S("seed", 6, ItemQuality.Silver));
            inventory.TryAdd(S("wood", 9));

            Assert.AreEqual(10, inventory.Count("seed"));
            Assert.AreEqual(0, inventory.Count("hoe"));
        }

        [Test]
        public void Remove_TakesAcrossStacks()
        {
            var inventory = New();
            inventory.TryAdd(S("seed", 4));
            inventory.TryAdd(S("seed", 6, ItemQuality.Silver));

            Assert.IsTrue(inventory.Remove("seed", 7));

            Assert.AreEqual(3, inventory.Count("seed"));
            Assert.IsTrue(inventory[0].IsEmpty);
            Assert.AreEqual(3, inventory[1].Quantity);
        }

        [Test]
        public void Remove_WithNotEnough_ChangesNothing()
        {
            var inventory = New();
            inventory.TryAdd(S("seed", 4));

            Assert.IsFalse(inventory.Remove("seed", 5));
            Assert.AreEqual(4, inventory.Count("seed"));
            Assert.IsFalse(inventory.Remove("seed", 0));
        }

        // ------------------------------------------------------------------ Take / Place

        [Test]
        public void Take_Partial_LeavesTheRest()
        {
            var inventory = New();
            inventory.TryAdd(S("seed", 10));

            var taken = inventory.Take(0, 4);

            Assert.AreEqual(S("seed", 4), taken);
            Assert.AreEqual(S("seed", 6), inventory[0]);
        }

        [Test]
        public void Take_MoreThanAvailable_TakesEverything()
        {
            var inventory = New();
            inventory.TryAdd(S("seed", 10));

            var taken = inventory.Take(0);

            Assert.AreEqual(S("seed", 10), taken);
            Assert.IsTrue(inventory[0].IsEmpty);
        }

        [Test]
        public void Take_FromEmptySlot_ReturnsEmpty()
        {
            Assert.IsTrue(New().Take(2).IsEmpty);
        }

        [Test]
        public void Place_IntoEmptySlot_StoresIt()
        {
            var inventory = New();

            var inHand = inventory.Place(2, S("wood", 7));

            Assert.IsTrue(inHand.IsEmpty);
            Assert.AreEqual(S("wood", 7), inventory[2]);
        }

        [Test]
        public void Place_OntoSameItem_MergesUpToTheLimit()
        {
            var inventory = New();
            inventory.TryAdd(S("small", 4));

            var inHand = inventory.Place(0, S("small", 3));

            Assert.AreEqual(S("small", 5), inventory[0]);
            Assert.AreEqual(S("small", 2), inHand);
        }

        [Test]
        public void Place_OntoDifferentItem_Swaps()
        {
            var inventory = New();
            inventory.TryAdd(S("wood", 3));

            var inHand = inventory.Place(0, S("seed", 8));

            Assert.AreEqual(S("seed", 8), inventory[0]);
            Assert.AreEqual(S("wood", 3), inHand);
        }

        [Test]
        public void Place_SameItemDifferentQuality_Swaps()
        {
            var inventory = New();
            inventory.TryAdd(S("seed", 3));

            var inHand = inventory.Place(0, S("seed", 2, ItemQuality.Gold));

            Assert.AreEqual(S("seed", 2, ItemQuality.Gold), inventory[0]);
            Assert.AreEqual(S("seed", 3), inHand);
        }

        [Test]
        public void Place_OversizedStackOntoOtherItem_DoesNotSwap()
        {
            var inventory = New();
            inventory.TryAdd(S("wood", 3));

            var inHand = inventory.Place(0, S("small", 50));

            Assert.AreEqual(S("small", 50), inHand);
            Assert.AreEqual(S("wood", 3), inventory[0]);
        }

        [Test]
        public void TakeThenPlace_MovesAStackBetweenSlots()
        {
            var inventory = New();
            inventory.TryAdd(S("seed", 12));

            var inHand = inventory.Take(0);
            inHand = inventory.Place(4, inHand);

            Assert.IsTrue(inHand.IsEmpty);
            Assert.IsTrue(inventory[0].IsEmpty);
            Assert.AreEqual(S("seed", 12), inventory[4]);
        }

        [Test]
        public void SplitHalf_ThenPlaceElsewhere_KeepsTotals()
        {
            var inventory = New();
            inventory.TryAdd(S("seed", 11));

            var half = inventory.Take(0, (inventory[0].Quantity + 1) / 2);
            inventory.Place(3, half);

            Assert.AreEqual(5, inventory[0].Quantity);
            Assert.AreEqual(6, inventory[3].Quantity);
            Assert.AreEqual(11, inventory.Count("seed"));
        }

        [Test]
        public void PlaceOne_DropsASingleUnitAndKeepsTheRestInHand()
        {
            var inventory = New();

            var inHand = inventory.PlaceOne(1, S("seed", 3));
            Assert.AreEqual(S("seed", 2), inHand);
            Assert.AreEqual(S("seed", 1), inventory[1]);

            inHand = inventory.PlaceOne(1, inHand);
            Assert.AreEqual(S("seed", 1), inHand);
            Assert.AreEqual(S("seed", 2), inventory[1]);
        }

        [Test]
        public void PlaceOne_OntoDifferentItem_ChangesNothing()
        {
            var inventory = New();
            inventory.TryAdd(S("wood", 3));

            var inHand = inventory.PlaceOne(0, S("seed", 3));

            Assert.AreEqual(S("seed", 3), inHand);
            Assert.AreEqual(S("wood", 3), inventory[0]);
        }

        [Test]
        public void PlaceOne_OntoFullStack_ChangesNothing()
        {
            var inventory = New();
            inventory.TryAdd(S("small", 5));

            Assert.AreEqual(S("small", 2), inventory.PlaceOne(0, S("small", 2)));
            Assert.AreEqual(5, inventory[0].Quantity);
        }

        [Test]
        public void Slots_RejectOutOfRangeIndexes()
        {
            var inventory = New();

            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = inventory[-1]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => inventory.Take(6));
            Assert.Throws<ArgumentOutOfRangeException>(() => inventory.Place(6, S("seed", 1)));
        }

        // ------------------------------------------------------------------ eventos

        [Test]
        public void Events_FireOncePerOperation_WithEachChangedSlot()
        {
            var inventory = New();
            inventory.TryAdd(S("small", 4));
            var slots = new List<int>();
            var changes = 0;
            inventory.SlotChanged += slots.Add;
            inventory.Changed += () => changes++;

            inventory.TryAdd(S("small", 4)); // completa o slot 0 e abre o slot 1

            CollectionAssert.AreEqual(new[] { 0, 1 }, slots);
            Assert.AreEqual(1, changes);
        }

        // ------------------------------------------------------------------ barra rápida

        [Test]
        public void SelectHotbar_ChangesSelectionAndNotifiesOnlyOnChange()
        {
            var inventory = New(4, 0);
            var notifications = 0;
            inventory.SelectionChanged += () => notifications++;

            inventory.SelectHotbar(2);
            inventory.SelectHotbar(2);
            inventory.SelectHotbar(9);
            inventory.SelectHotbar(-1);

            Assert.AreEqual(2, inventory.SelectedHotbarIndex);
            Assert.AreEqual(1, notifications);
        }

        [Test]
        public void ScrollHotbar_WrapsAroundBothWays()
        {
            var inventory = New(4, 0);

            inventory.ScrollHotbar(-1);
            Assert.AreEqual(3, inventory.SelectedHotbarIndex);

            inventory.ScrollHotbar(1);
            Assert.AreEqual(0, inventory.SelectedHotbarIndex);
        }

        [Test]
        public void SelectedStack_FollowsTheSelectedSlot()
        {
            var inventory = New();
            inventory.Place(1, S("wood", 2));

            inventory.SelectHotbar(1);

            Assert.AreEqual(S("wood", 2), inventory.SelectedStack);
        }

        [TestCase(0, 1, 12, 1)]
        [TestCase(11, 1, 12, 0)]
        [TestCase(0, -1, 12, 11)]
        [TestCase(5, 25, 12, 6)]
        [TestCase(5, -30, 12, 11)]
        public void HotbarMath_Wrap(int index, int delta, int size, int expected)
        {
            Assert.AreEqual(expected, HotbarMath.Wrap(index, delta, size));
        }

        // ------------------------------------------------------------------ save

        [Test]
        public void SnapshotAndRestore_RoundTrip()
        {
            var original = New();
            original.TryAdd(S("seed", 12));
            original.TryAdd(S("hoe", 1));
            original.Place(5, S("wood", 40, ItemQuality.Silver));

            var restored = New();
            restored.Restore(original.Snapshot());

            for (var i = 0; i < original.Capacity; i++)
                Assert.AreEqual(original[i], restored[i]);
        }

        [Test]
        public void Restore_ClampsOversizedStacksAndClearsMissingSlots()
        {
            var inventory = New();
            inventory.TryAdd(S("wood", 3));

            inventory.Restore(new[] { S("small", 50) });

            Assert.AreEqual(S("small", 5), inventory[0]);
            Assert.IsTrue(inventory[1].IsEmpty);
            Assert.AreEqual(0, inventory.Count("wood"));
        }

        [Test]
        public void Snapshot_IsACopy()
        {
            var inventory = New();
            var snapshot = inventory.Snapshot();

            snapshot[0] = S("seed", 1);

            Assert.IsTrue(inventory[0].IsEmpty);
        }
    }
}
