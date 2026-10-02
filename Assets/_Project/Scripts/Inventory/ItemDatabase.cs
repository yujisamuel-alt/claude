using System.Collections.Generic;
using UnityEngine;

namespace Enxada.Inventory
{
    /// <summary>Lista de todos os itens do jogo. É o catálogo que o inventário consulta.</summary>
    [CreateAssetMenu(menuName = "Enxada/Items/Item Database", fileName = "ItemDatabase")]
    public sealed class ItemDatabase : ScriptableObject, IItemCatalog
    {
        [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();

        private Dictionary<string, ItemDefinition> _byId;

        public IReadOnlyList<ItemDefinition> Items => items;

        public bool TryGet(string itemId, out ItemDefinition definition)
        {
            EnsureIndex();
            return _byId.TryGetValue(itemId ?? string.Empty, out definition);
        }

        public bool TryGetMaxStack(string itemId, out int maxStack)
        {
            if (TryGet(itemId, out var definition))
            {
                maxStack = definition.MaxStack;
                return true;
            }

            maxStack = 1;
            return false;
        }

        private void OnEnable() => _byId = null;
        private void OnValidate() => _byId = null;

        private void EnsureIndex()
        {
            if (_byId != null)
                return;

            _byId = new Dictionary<string, ItemDefinition>();
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.Id))
                    continue;

                if (!_byId.TryAdd(item.Id, item))
                    Debug.LogError($"[ItemDatabase] Id duplicado: '{item.Id}'.", this);
            }
        }
    }
}
