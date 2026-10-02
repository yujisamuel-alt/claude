using Enxada.Player;
using NUnit.Framework;

namespace Enxada.Tests.Logic.Player
{
    public class MovementInputTests
    {
        private const float Deadzone = 0.2f;

        [Test]
        public void NoInput_StandsStillAndKeepsFacing()
        {
            var result = MovementInput.Resolve(0f, 0f, false, Deadzone, FacingDirection.Left);

            Assert.IsFalse(result.IsMoving);
            Assert.AreEqual(FacingDirection.Left, result.Facing);
        }

        [Test]
        public void InputInsideDeadzone_IsIgnored()
        {
            var result = MovementInput.Resolve(0.1f, 0.1f, false, Deadzone, FacingDirection.Up);

            Assert.IsFalse(result.IsMoving);
            Assert.AreEqual(FacingDirection.Up, result.Facing);
        }

        [TestCase(1f, 0f, FacingDirection.Right)]
        [TestCase(-1f, 0f, FacingDirection.Left)]
        [TestCase(0f, 1f, FacingDirection.Up)]
        [TestCase(0f, -1f, FacingDirection.Down)]
        public void FourDirections_FaceTheInputAxis(float x, float y, FacingDirection expected)
        {
            var result = MovementInput.Resolve(x, y, false, Deadzone, FacingDirection.Down);

            Assert.IsTrue(result.IsMoving);
            Assert.AreEqual(expected, result.Facing);
        }

        [Test]
        public void FourDirections_DominantAxisWins()
        {
            var result = MovementInput.Resolve(0.9f, 0.4f, false, Deadzone, FacingDirection.Down);

            Assert.AreEqual(0.9f, result.X, 1e-5f);
            Assert.AreEqual(0f, result.Y);
            Assert.AreEqual(FacingDirection.Right, result.Facing);
        }

        [Test]
        public void FourDirections_TieKeepsCurrentAxis()
        {
            var horizontal = MovementInput.Resolve(1f, 1f, false, Deadzone, FacingDirection.Right);
            var vertical = MovementInput.Resolve(1f, 1f, false, Deadzone, FacingDirection.Down);

            Assert.AreEqual(0f, horizontal.Y);
            Assert.AreEqual(1f, horizontal.X, 1e-5f);
            Assert.AreEqual(0f, vertical.X);
            Assert.AreEqual(1f, vertical.Y, 1e-5f);
        }

        [Test]
        public void FourDirections_NeverMovesDiagonally()
        {
            var result = MovementInput.Resolve(0.7f, 0.7f, false, Deadzone, FacingDirection.Down);

            Assert.IsTrue(result.X == 0f || result.Y == 0f);
        }

        [Test]
        public void Diagonal_IsNotFasterThanStraight()
        {
            var result = MovementInput.Resolve(1f, 1f, true, Deadzone, FacingDirection.Down);
            var length = (float)System.Math.Sqrt(result.X * result.X + result.Y * result.Y);

            Assert.AreEqual(1f, length, 1e-5f);
        }

        [Test]
        public void AnalogStick_KeepsPartialStrength()
        {
            var result = MovementInput.Resolve(0.5f, 0f, false, Deadzone, FacingDirection.Down);

            Assert.AreEqual(0.5f, result.X, 1e-5f);
        }

        [Test]
        public void OversizedInput_IsClampedToOne()
        {
            var result = MovementInput.Resolve(5f, 0f, false, Deadzone, FacingDirection.Down);

            Assert.AreEqual(1f, result.X, 1e-5f);
        }
    }
}
