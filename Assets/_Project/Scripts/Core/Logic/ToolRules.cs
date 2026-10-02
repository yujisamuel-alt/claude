using System;

namespace Enxada.Core
{
    /// <summary>Regras de balanceamento das ferramentas, separadas para serem testadas.</summary>
    public static class ToolRules
    {
        /// <summary>Energia gasta: o custo base cai a cada nível de ferramenta, nunca abaixo do mínimo (custo 0 continua 0).</summary>
        public static int EnergyCost(int baseCost, int tier, int reductionPerTier, int minimumCost)
        {
            if (baseCost <= 0)
                return 0;

            return Math.Max(minimumCost, baseCost - Math.Max(0, tier) * reductionPerTier);
        }

        /// <summary>Quantos "pontos de vida" um golpe tira de pedras e árvores.</summary>
        public static int HitDamage(int tier) => 1 + Math.Max(0, tier);

        public static bool MeetsTier(int requiredTier, int toolTier) => toolTier >= requiredTier;
    }
}
