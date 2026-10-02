using System;

namespace Enxada.Core
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
}
