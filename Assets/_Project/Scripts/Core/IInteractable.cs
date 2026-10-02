using UnityEngine;

namespace Enxada.Core
{
    /// <summary>
    /// Qualquer coisa com que o jogador interage apontando o tile alvo (cama, baú, poço, moradores...).
    /// O objeto precisa ter um Collider2D cobrindo o tile.
    /// </summary>
    public interface IInteractable
    {
        void Interact(GameObject interactor);
    }
}
