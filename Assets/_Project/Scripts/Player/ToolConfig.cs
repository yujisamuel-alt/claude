using System;
using Enxada.Core;
using UnityEngine;

namespace Enxada.Player
{
    /// <summary>Custo de energia e duração do golpe de cada ferramenta.</summary>
    [CreateAssetMenu(menuName = "Enxada/Config/Tool Config", fileName = "ToolConfig")]
    public sealed class ToolConfig : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public ToolType tool;

            [Tooltip("Energia gasta a cada uso (2 a 8 no design).")]
            [Min(0)] public int energyCost;

            [Tooltip("Segundos que o jogador fica parado fazendo o golpe.")]
            [Min(0.05f)] public float useDuration;
        }

        [SerializeField] private Entry[] entries;

        [Tooltip("Quanto cada nível de ferramenta (cobre, ferro, ouro) economiza de energia.")]
        [Min(0)] [SerializeField] private int energyReductionPerTier = 1;

        [Tooltip("Custo mínimo de energia, não importa o nível.")]
        [Min(0)] [SerializeField] private int minimumEnergyCost = 1;

        public int EnergyReductionPerTier => energyReductionPerTier;
        public int MinimumEnergyCost => minimumEnergyCost;

        public bool TryGet(ToolType tool, out Entry entry)
        {
            if (entries != null)
            {
                foreach (var candidate in entries)
                {
                    if (candidate.tool != tool)
                        continue;

                    entry = candidate;
                    return true;
                }
            }

            entry = default;
            return false;
        }
    }
}
