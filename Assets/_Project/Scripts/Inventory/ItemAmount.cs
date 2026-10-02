using System;
using UnityEngine;

namespace Enxada.Inventory
{
    /// <summary>Um item com quantidade e qualidade, para listas configuráveis no Inspector.</summary>
    [Serializable]
    public struct ItemAmount
    {
        public ItemDefinition item;
        [Min(1)] public int quantity;
        public ItemQuality quality;

        public ItemStack ToStack() => item == null ? ItemStack.Empty : new ItemStack(item.Id, quantity, quality);
    }
}
