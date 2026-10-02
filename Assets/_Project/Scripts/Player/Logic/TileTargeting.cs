using System;

namespace Enxada.Player
{
    /// <summary>Posição de um tile na grade (sem depender de Vector3Int da Unity).</summary>
    public readonly struct CellPosition : IEquatable<CellPosition>
    {
        public readonly int X;
        public readonly int Y;

        public CellPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(CellPosition other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is CellPosition other && Equals(other);
        public override int GetHashCode() => unchecked(X * 397) ^ Y;
        public override string ToString() => $"({X}, {Y})";
    }

    /// <summary>Decide qual tile é o "alvo" das ações do jogador.</summary>
    public static class TileTargeting
    {
        public static CellPosition FacingCell(CellPosition origin, FacingDirection facing)
        {
            switch (facing)
            {
                case FacingDirection.Up: return new CellPosition(origin.X, origin.Y + 1);
                case FacingDirection.Left: return new CellPosition(origin.X - 1, origin.Y);
                case FacingDirection.Right: return new CellPosition(origin.X + 1, origin.Y);
                default: return new CellPosition(origin.X, origin.Y - 1);
            }
        }

        /// <summary>
        /// Sem mouse: o tile à frente do jogador. Com mouse: o tile sob o cursor, limitado a
        /// <paramref name="maxRange"/> tiles de distância (o cursor longe "gruda" na borda do alcance).
        /// Se o cursor estiver sobre o tile do próprio jogador, usa o tile à frente.
        /// </summary>
        public static CellPosition ResolveTarget(CellPosition player, FacingDirection facing, bool useMouse,
            CellPosition mouseCell, int maxRange)
        {
            var facingCell = FacingCell(player, facing);
            if (!useMouse)
                return facingCell;

            var range = Math.Max(1, maxRange);
            var dx = Math.Max(-range, Math.Min(range, mouseCell.X - player.X));
            var dy = Math.Max(-range, Math.Min(range, mouseCell.Y - player.Y));
            var clamped = new CellPosition(player.X + dx, player.Y + dy);

            return clamped.Equals(player) ? facingCell : clamped;
        }
    }
}
