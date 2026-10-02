using Enxada.Inventory;
using NUnit.Framework;

namespace Enxada.Tests.Logic.InventoryTests
{
    public class PickupAndKeysTests
    {
        [Test]
        public void ShouldAttract_WaitsForTheDelayAndRespectsTheRadius()
        {
            Assert.IsFalse(PickupRules.ShouldAttract(1f, 2f, age: 0.5f, pickupDelay: 1.5f));
            Assert.IsTrue(PickupRules.ShouldAttract(1f, 2f, age: 1.5f, pickupDelay: 1.5f));
            Assert.IsFalse(PickupRules.ShouldAttract(2.5f, 2f, age: 9f, pickupDelay: 1.5f));
        }

        [Test]
        public void ShouldCollect_OnlyWhenVeryClose()
        {
            Assert.IsTrue(PickupRules.ShouldCollect(0.2f, 0.3f));
            Assert.IsFalse(PickupRules.ShouldCollect(0.5f, 0.3f));
        }

        [Test]
        public void AttractSpeed_GrowsAsTheItemGetsCloser()
        {
            var far = PickupRules.AttractSpeed(2f, 2f, 2f, 10f);
            var middle = PickupRules.AttractSpeed(1f, 2f, 2f, 10f);
            var near = PickupRules.AttractSpeed(0f, 2f, 2f, 10f);

            Assert.AreEqual(2f, far, 1e-5f);
            Assert.AreEqual(6f, middle, 1e-5f);
            Assert.AreEqual(10f, near, 1e-5f);
            Assert.AreEqual(10f, PickupRules.AttractSpeed(5f, 0f, 2f, 10f));
        }

        [Test]
        public void ItemKeys_FollowTheConvention()
        {
            Assert.AreEqual("item.hoe.name", ItemKeys.Name("hoe"));
            Assert.AreEqual("item.hoe.desc", ItemKeys.Description("hoe"));
            Assert.AreEqual("quality.gold", ItemKeys.Quality(ItemQuality.Gold));
            Assert.AreEqual("category.seed", ItemKeys.Category(ItemCategory.Seed));
        }
    }
}
