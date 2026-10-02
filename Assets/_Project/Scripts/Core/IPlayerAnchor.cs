using UnityEngine;

namespace Enxada.Core
{
    /// <summary>
    /// Onde o jogador está e para onde olha. O PlayerController se registra como serviço,
    /// assim itens no chão, moradores e outros sistemas não precisam conhecer o módulo Player.
    /// </summary>
    public interface IPlayerAnchor
    {
        Transform Transform { get; }

        /// <summary>Direção para onde o jogador olha, como vetor unitário.</summary>
        Vector2 FacingVector { get; }
    }
}
