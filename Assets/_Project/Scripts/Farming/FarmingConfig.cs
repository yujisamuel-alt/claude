using UnityEngine;

namespace Enxada.Farming
{
    /// <summary>Números da fazenda (balanceamento).</summary>
    [CreateAssetMenu(menuName = "Enxada/Config/Farming Config", fileName = "FarmingConfig")]
    public sealed class FarmingConfig : ScriptableObject
    {
        [Tooltip("Quantos tiles o regador rega antes de esvaziar.")]
        [Min(1)] [SerializeField] private int wateringCanCapacity = 40;

        [Tooltip("Chance por dia de uma terra arada, sem planta, voltar a ser grama.")]
        [Range(0f, 1f)] [SerializeField] private float tilledRevertChance = 0.1f;

        public int WateringCanCapacity => wateringCanCapacity;
        public float TilledRevertChance => tilledRevertChance;
    }
}
