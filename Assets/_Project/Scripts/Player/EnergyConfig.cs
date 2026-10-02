using UnityEngine;

namespace Enxada.Player
{
    /// <summary>Números da energia do jogador (balanceamento).</summary>
    [CreateAssetMenu(menuName = "Enxada/Config/Energy Config", fileName = "EnergyConfig")]
    public sealed class EnergyConfig : ScriptableObject
    {
        [Min(1)] [SerializeField] private int maxEnergy = 270;

        [Tooltip("Abaixo deste valor o jogador desmaia.")]
        [Range(-100, 0)] [SerializeField] private int passOutThreshold = -15;

        [Tooltip("Velocidade do jogador com energia 0 ou menos (0,5 = metade).")]
        [Range(0.1f, 1f)] [SerializeField] private float exhaustedSpeedMultiplier = 0.5f;

        [Tooltip("Fração da energia máxima com que acorda depois de desmaiar. Dormir recupera tudo.")]
        [Range(0f, 1f)] [SerializeField] private float passOutRecoveryFraction = 0.5f;

        public EnergySettings ToSettings() =>
            new EnergySettings(maxEnergy, passOutThreshold, exhaustedSpeedMultiplier, passOutRecoveryFraction);
    }
}
