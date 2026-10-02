using System;

namespace Enxada.Inventory
{
    /// <summary>Regras de quando um item no chão é atraído e coletado pelo jogador.</summary>
    public static class PickupRules
    {
        /// <summary>O item só é atraído depois do atraso (para itens descartados não voltarem na hora).</summary>
        public static bool ShouldAttract(float distance, float attractRadius, float age, float pickupDelay) =>
            age >= pickupDelay && distance <= attractRadius;

        public static bool ShouldCollect(float distance, float collectRadius) => distance <= collectRadius;

        /// <summary>Velocidade de atração: devagar na borda do raio, rápida perto do jogador.</summary>
        public static float AttractSpeed(float distance, float attractRadius, float minSpeed, float maxSpeed)
        {
            if (attractRadius <= 0f)
                return maxSpeed;

            var closeness = 1f - Math.Max(0f, Math.Min(1f, distance / attractRadius));
            return minSpeed + (maxSpeed - minSpeed) * closeness;
        }
    }
}
