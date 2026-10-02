using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Enxada.UI
{
    /// <summary>
    /// Um slot da tela de inventário. É um Selectable, então o gamepad e o teclado navegam entre eles.
    /// Mouse: clique esquerdo pega/solta, direito divide ou solta um, arrastar também funciona.
    /// </summary>
    public sealed class InventorySlotView : Selectable, IPointerClickHandler, ISubmitHandler,
        IBeginDragHandler, IDragHandler, IDropHandler, IEndDragHandler
    {
        private InventoryScreen _screen;
        private int _index;
        private bool _dragging;

        public void Bind(InventoryScreen screen, int index)
        {
            _screen = screen;
            _index = index;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                _screen.PrimaryAction(_index);
            else if (eventData.button == PointerEventData.InputButton.Right)
                _screen.SecondaryAction(_index);
        }

        public void OnSubmit(BaseEventData eventData) => _screen.PrimaryAction(_index);

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            _dragging = true;
            _screen.BeginDrag(_index);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // O item na mão já segue o mouse (InventoryScreen); nada a fazer aqui, mas o handler é
            // necessário para o EventSystem enviar OnBeginDrag e OnEndDrag.
        }

        public void OnDrop(PointerEventData eventData) => _screen.DropOnSlot(_index);

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging)
                return;

            _dragging = false;
            _screen.EndDrag(_index);
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            _screen.ShowInfo(_index);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            _screen.ClearInfo();
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            _screen.ShowInfo(_index);
        }
    }
}
