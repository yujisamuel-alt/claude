using Enxada.Core;
using Enxada.Player;
using NUnit.Framework;

namespace Enxada.Tests.Logic.Player
{
    public class TileTargetingTests
    {
        private static readonly CellPosition Origin = new CellPosition(10, 10);

        [TestCase(FacingDirection.Down, 10, 9)]
        [TestCase(FacingDirection.Up, 10, 11)]
        [TestCase(FacingDirection.Left, 9, 10)]
        [TestCase(FacingDirection.Right, 11, 10)]
        public void FacingCell_IsTheNeighbourInFront(FacingDirection facing, int x, int y)
        {
            Assert.AreEqual(new CellPosition(x, y), TileTargeting.FacingCell(Origin, facing));
        }

        [Test]
        public void WithoutMouse_TargetsTheFacingCell()
        {
            var target = TileTargeting.ResolveTarget(Origin, FacingDirection.Up, false, new CellPosition(0, 0), 1);

            Assert.AreEqual(new CellPosition(10, 11), target);
        }

        [Test]
        public void Mouse_OnAdjacentCell_TargetsThatCell()
        {
            var target = TileTargeting.ResolveTarget(Origin, FacingDirection.Up, true, new CellPosition(11, 9), 1);

            Assert.AreEqual(new CellPosition(11, 9), target);
        }

        [Test]
        public void Mouse_FarAway_IsClampedToRange()
        {
            var target = TileTargeting.ResolveTarget(Origin, FacingDirection.Up, true, new CellPosition(30, 5), 1);

            Assert.AreEqual(new CellPosition(11, 9), target);
        }

        [Test]
        public void Mouse_OnPlayerCell_FallsBackToFacingCell()
        {
            var target = TileTargeting.ResolveTarget(Origin, FacingDirection.Left, true, Origin, 1);

            Assert.AreEqual(new CellPosition(9, 10), target);
        }

        [Test]
        public void LargerRange_AllowsFurtherTargets()
        {
            var target = TileTargeting.ResolveTarget(Origin, FacingDirection.Up, true, new CellPosition(12, 10), 2);

            Assert.AreEqual(new CellPosition(12, 10), target);
        }

        [Test]
        public void InvalidRange_IsTreatedAsOne()
        {
            var target = TileTargeting.ResolveTarget(Origin, FacingDirection.Up, true, new CellPosition(14, 10), 0);

            Assert.AreEqual(new CellPosition(11, 10), target);
        }
    }
}
