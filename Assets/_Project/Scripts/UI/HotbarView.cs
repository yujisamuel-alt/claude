using Enxada.Core;
using Enxada.Inventory;
using UnityEngine;

namespace Enxada.UI
{
    /// <summary>Barra rápida sempre visível na parte de baixo da tela.</summary>
    public sealed class HotbarView : MonoBehaviour
    {
        [SerializeField] private SlotVisual[] slots;

        private InventoryModel _inventory;
        private ItemDatabase _database;

        private void Start()
        {
            _inventory = ServiceLocator.Get<InventoryModel>();
            _database = ServiceLocator.Get<ItemDatabase>();

            _inventory.Changed += Refresh;
            _inventory.SelectionChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_inventory == null)
                return;

            _inventory.Changed -= Refresh;
            _inventory.SelectionChanged -= Refresh;
        }

        private void Refresh()
        {
            for (var i = 0; i < slots.Length && i < _inventory.HotbarSize; i++)
            {
                slots[i].SetStack(_inventory[i], _database);
                slots[i].SetSelected(i == _inventory.SelectedHotbarIndex);
            }
        }
    }
}
