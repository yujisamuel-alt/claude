using UnityEngine;

namespace Enxada.Player
{
    /// <summary>Números de movimento e mira do jogador. Ajuste aqui, nunca no código.</summary>
    [CreateAssetMenu(menuName = "Enxada/Config/Player Config", fileName = "PlayerConfig")]
    public sealed class PlayerConfig : ScriptableObject
    {
        [Tooltip("Velocidade em tiles por segundo.")]
        [Min(0.1f)] [SerializeField] private float moveSpeed = 4f;

        [Tooltip("Desligado = 4 direções (como Stardew). Ligado = 8 direções.")]
        [SerializeField] private bool allowDiagonal;

        [Tooltip("Input analógico abaixo disso é ignorado.")]
        [Range(0f, 0.9f)] [SerializeField] private float deadzone = 0.2f;

        [Tooltip("Distância máxima (em tiles) do tile alvo quando se mira com o mouse.")]
        [Min(1)] [SerializeField] private int mouseTargetRange = 1;

        public float MoveSpeed => moveSpeed;
        public bool AllowDiagonal => allowDiagonal;
        public float Deadzone => deadzone;
        public int MouseTargetRange => mouseTargetRange;
    }
}
