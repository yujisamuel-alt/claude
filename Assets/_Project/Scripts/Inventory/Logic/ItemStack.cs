using System;

namespace Enxada.Inventory
{
    /// <summary>
    /// Uma pilha de itens: qual item, quantos e de que qualidade. Imutável.
    /// Quantidade zero (ou id vazio) é a pilha vazia; use ItemStack.Empty ou default.
    /// </summary>
    public readonly struct ItemStack : IEquatable<ItemStack>
    {
        public readonly string ItemId;
        public readonly int Quantity;
        public readonly ItemQuality Quality;

        public ItemStack(string itemId, int quantity, ItemQuality quality = ItemQuality.Normal)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
            {
                ItemId = null;
                Quantity = 0;
                Quality = ItemQuality.Normal;
            }
            else
            {
                ItemId = itemId;
                Quantity = quantity;
                Quality = quality;
            }
        }

        public static ItemStack Empty => default;

        public bool IsEmpty => Quantity <= 0 || ItemId == null;

        /// <summary>Mesmo item e mesma qualidade (prata e ouro nunca se misturam com normal).</summary>
        public bool CanStackWith(ItemStack other) =>
            !IsEmpty && !other.IsEmpty && ItemId == other.ItemId && Quality == other.Quality;

        public ItemStack WithQuantity(int quantity) => new ItemStack(ItemId, quantity, Quality);

        public bool Equals(ItemStack other) =>
            ItemId == other.ItemId && Quantity == other.Quantity && Quality == other.Quality;

        public override bool Equals(object obj) => obj is ItemStack other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((ItemId != null ? ItemId.GetHashCode() : 0) * 397 ^ Quantity) * 397 ^ (int)Quality;
            }
        }

        public override string ToString() => IsEmpty ? "(vazio)" : $"{Quantity}x {ItemId} [{Quality}]";
    }
}
