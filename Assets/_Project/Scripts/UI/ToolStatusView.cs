using Enxada.Core;
using Enxada.Farming;
using Enxada.Inventory;
using TMPro;
using UnityEngine;

namespace Enxada.UI
{
    /// <summary>Mostra o nível de água acima da barra rápida quando o regador está selecionado.</summary>
    public sealed class ToolStatusView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        private InventoryModel _inventory;
        private ItemDatabase _database;
        private WateringCanState _can;
        private ITextProvider _texts;

        private void Start()
        {
            _inventory = ServiceLocator.Get<InventoryModel>();
            _database = ServiceLocator.Get<ItemDatabase>();
            _can = ServiceLocator.Get<WateringCanState>();
            _texts = ServiceLocator.Get<ITextProvider>();

            _inventory.Changed += Refresh;
            _inventory.SelectionChanged += Refresh;
            _can.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_inventory != null)
            {
                _inventory.Changed -= Refresh;
                _inventory.SelectionChanged -= Refresh;
            }

            if (_can != null)
                _can.Changed -= Refresh;
        }

        private void Refresh()
        {
            var stack = _inventory.SelectedStack;
            var isCan = !stack.IsEmpty && _database.TryGet(stack.ItemId, out var item)
                        && item.ToolType == ToolType.WateringCan;

            label.text = isCan ? _texts.Format("hud.water", _can.Level, _can.Capacity) : string.Empty;
        }
    }
}
