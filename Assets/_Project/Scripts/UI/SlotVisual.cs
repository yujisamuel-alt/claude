using Enxada.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Enxada.UI
{
    /// <summary>Desenha um slot: ícone, quantidade, marca de qualidade e moldura de seleção.</summary>
    public sealed class SlotVisual : MonoBehaviour
    {
        private static readonly Color SilverTint = new Color(0.85f, 0.88f, 0.95f, 1f);
        private static readonly Color GoldTint = new Color(1f, 0.82f, 0.25f, 1f);

        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text quantityLabel;
        [SerializeField] private Image qualityMark;
        [SerializeField] private GameObject selectionFrame;

        public void SetStack(ItemStack stack, ItemDatabase database)
        {
            if (stack.IsEmpty || !database.TryGet(stack.ItemId, out var definition))
            {
                icon.enabled = false;
                quantityLabel.text = string.Empty;
                qualityMark.enabled = false;
                return;
            }

            icon.sprite = definition.Icon;
            icon.enabled = definition.Icon != null;
            quantityLabel.text = stack.Quantity > 1 ? stack.Quantity.ToString() : string.Empty;

            qualityMark.enabled = stack.Quality != ItemQuality.Normal;
            qualityMark.color = stack.Quality == ItemQuality.Gold ? GoldTint : SilverTint;
        }

        public void SetSelected(bool selected)
        {
            if (selectionFrame != null)
                selectionFrame.SetActive(selected);
        }
    }
}
