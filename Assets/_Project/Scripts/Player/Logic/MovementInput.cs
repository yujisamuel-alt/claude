using System;

namespace Enxada.Player
{
    /// <summary>Resultado de interpretar o input: vetor de movimento (já com intensidade) e direção virada.</summary>
    public readonly struct MoveResult
    {
        public readonly float X;
        public readonly float Y;
        public readonly FacingDirection Facing;

        public MoveResult(float x, float y, FacingDirection facing)
        {
            X = x;
            Y = y;
            Facing = facing;
        }

        public bool IsMoving => X != 0f || Y != 0f;
    }

    /// <summary>
    /// Converte o input bruto (teclado ou analógico) em movimento top-down.
    /// Lógica pura, sem UnityEngine, para ser testada fora do editor.
    /// </summary>
    public static class MovementInput
    {
        public static MoveResult Resolve(float rawX, float rawY, bool allowDiagonal, float deadzone,
            FacingDirection currentFacing)
        {
            var magnitude = (float)Math.Sqrt(rawX * rawX + rawY * rawY);
            if (magnitude <= 0f || magnitude < deadzone)
                return new MoveResult(0f, 0f, currentFacing);

            if (allowDiagonal)
            {
                // 8 direções: mantém o analógico proporcional, mas nunca passa de 1 (diagonal não é mais rápida).
                var strength = Math.Min(magnitude, 1f);
                return new MoveResult(rawX / magnitude * strength, rawY / magnitude * strength,
                    FacingFromVector(rawX, rawY, currentFacing));
            }

            // 4 direções: o eixo dominante vence; no empate (duas teclas) mantém o eixo atual.
            var facing = FacingFromVector(rawX, rawY, currentFacing);
            var horizontal = facing == FacingDirection.Left || facing == FacingDirection.Right;
            var axisStrength = Math.Min(Math.Max(Math.Abs(rawX), Math.Abs(rawY)), 1f);

            return horizontal
                ? new MoveResult(rawX > 0f ? axisStrength : -axisStrength, 0f, facing)
                : new MoveResult(0f, rawY > 0f ? axisStrength : -axisStrength, facing);
        }

        public static FacingDirection FacingFromVector(float x, float y, FacingDirection currentFacing)
        {
            var absX = Math.Abs(x);
            var absY = Math.Abs(y);

            bool horizontal;
            if (absX > absY) horizontal = true;
            else if (absY > absX) horizontal = false;
            else horizontal = currentFacing == FacingDirection.Left || currentFacing == FacingDirection.Right;

            if (horizontal)
                return x >= 0f ? FacingDirection.Right : FacingDirection.Left;
            return y >= 0f ? FacingDirection.Up : FacingDirection.Down;
        }
    }
}
