using UnityEngine;
using UnityEngine.EventSystems;

namespace Enxada.UI
{
    /// <summary>Fundo escuro atrás do inventário: clicar (ou soltar um arrasto) aqui, com item na mão, descarta no chão.</summary>
    public sealed class InventoryDropZone : MonoBehaviour, IPointerClickHandler, IDropHandler
    {
        [SerializeField] private InventoryScreen screen;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                screen.DiscardHeld(false);
            else if (eventData.button == PointerEventData.InputButton.Right)
                screen.DiscardHeld(true);
        }

        public void OnDrop(PointerEventData eventData) => screen.DiscardHeld(false);
    }
}
