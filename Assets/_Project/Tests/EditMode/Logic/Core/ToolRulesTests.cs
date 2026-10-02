using Enxada.Core;
using NUnit.Framework;

namespace Enxada.Tests.Logic.Core
{
    public class ToolRulesTests
    {
        [TestCase(5, 0, 1, 1, 5)]
        [TestCase(5, 1, 1, 1, 4)]
        [TestCase(5, 3, 1, 1, 2)]
        [TestCase(5, 9, 1, 1, 1)]
        [TestCase(0, 2, 1, 1, 0)]
        [TestCase(5, -1, 1, 1, 5)]
        public void EnergyCost_DropsPerTierButRespectsTheMinimum(int baseCost, int tier, int reduction, int minimum,
            int expected)
        {
            Assert.AreEqual(expected, ToolRules.EnergyCost(baseCost, tier, reduction, minimum));
        }

        [TestCase(0, 1)]
        [TestCase(2, 3)]
        [TestCase(-1, 1)]
        public void HitDamage_GrowsWithTier(int tier, int expected)
        {
            Assert.AreEqual(expected, ToolRules.HitDamage(tier));
        }

        [Test]
        public void MeetsTier_RequiresAtLeastTheMinimum()
        {
            Assert.IsTrue(ToolRules.MeetsTier(0, 0));
            Assert.IsTrue(ToolRules.MeetsTier(1, 2));
            Assert.IsFalse(ToolRules.MeetsTier(2, 1));
        }

        [Test]
        public void ToolUseOutcome_Kinds()
        {
            Assert.IsFalse(ToolUseOutcome.None.WasHandled);
            Assert.IsTrue(ToolUseOutcome.Used.WasHandled);
            Assert.IsTrue(ToolUseOutcome.UsedFree.WasHandled);

            var rejected = ToolUseOutcome.Rejected("toast.x");
            Assert.IsTrue(rejected.WasHandled);
            Assert.AreEqual(ToolUseKind.Rejected, rejected.Kind);
            Assert.AreEqual("toast.x", rejected.MessageKey);
        }
    }
}
