using System;

namespace Enxada.Inventory
{
    /// <summary>
    /// Inventário: barra rápida (primeiros slots) seguida da mochila. Lógica pura e testada.
    /// A interface "segura" pilhas na mão com Take/Place: quem pega tira do slot, quem solta devolve o que sobrou.
    /// </summary>
    public sealed class InventoryModel
    {
        public const int DefaultHotbarSize = 12;
        public const int DefaultBackpackSize = 24;

        private readonly IItemCatalog _catalog;
        private readonly ItemStack[] _slots;
        private readonly bool[] _pending;
        private bool _anyPending;

        public InventoryModel(IItemCatalog catalog, int hotbarSize = DefaultHotbarSize,
            int backpackSize = DefaultBackpackSize)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            if (hotbarSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(hotbarSize));
            if (backpackSize < 0)
                throw new ArgumentOutOfRangeException(nameof(backpackSize));

            HotbarSize = hotbarSize;
            _slots = new ItemStack[hotbarSize + backpackSize];
            _pending = new bool[_slots.Length];
        }

        public int HotbarSize { get; }
        public int Capacity => _slots.Length;
        public int SelectedHotbarIndex { get; private set; }

        public ItemStack this[int slot]
        {
            get
            {
                CheckSlot(slot);
                return _slots[slot];
            }
        }

        public ItemStack SelectedStack => _slots[SelectedHotbarIndex];

        /// <summary>Um slot mudou (índice do slot).</summary>
        public event Action<int> SlotChanged;

        /// <summary>Algo mudou no inventário (disparado uma vez por operação).</summary>
        public event Action Changed;

        public event Action SelectionChanged;

        // ------------------------------------------------------------------ consulta

        public int Count(string itemId)
        {
            var total = 0;
            foreach (var slot in _slots)
            {
                if (!slot.IsEmpty && slot.ItemId == itemId)
                    total += slot.Quantity;
            }

            return total;
        }

        /// <summary>Quantas unidades da pilha ainda cabem (somando as pilhas incompletas e os slots vazios).</summary>
        public int RoomFor(ItemStack stack)
        {
            if (stack.IsEmpty)
                return 0;

            var max = MaxStackOf(stack.ItemId);
            var room = 0;
            foreach (var slot in _slots)
            {
                if (slot.IsEmpty)
                    room += max;
                else if (slot.CanStackWith(stack))
                    room += Math.Max(0, max - slot.Quantity);
            }

            return room;
        }

        public bool CanAdd(ItemStack stack) => RoomFor(stack) > 0;

        // ------------------------------------------------------------------ adicionar e remover

        /// <summary>Guarda a pilha: completa pilhas existentes e depois usa slots vazios (barra primeiro). Devolve o que não coube.</summary>
        public ItemStack TryAdd(ItemStack stack)
        {
            if (stack.IsEmpty)
                return ItemStack.Empty;

            var max = MaxStackOf(stack.ItemId);
            var remaining = stack.Quantity;

            for (var i = 0; i < _slots.Length && remaining > 0; i++)
            {
                if (!_slots[i].CanStackWith(stack) || _slots[i].Quantity >= max)
                    continue;

                var add = Math.Min(max - _slots[i].Quantity, remaining);
                _slots[i] = _slots[i].WithQuantity(_slots[i].Quantity + add);
                remaining -= add;
                Mark(i);
            }

            for (var i = 0; i < _slots.Length && remaining > 0; i++)
            {
                if (!_slots[i].IsEmpty)
                    continue;

                var add = Math.Min(max, remaining);
                _slots[i] = new ItemStack(stack.ItemId, add, stack.Quality);
                remaining -= add;
                Mark(i);
            }

            Flush();
            return remaining > 0 ? stack.WithQuantity(remaining) : ItemStack.Empty;
        }

        /// <summary>Remove a quantidade (de qualquer qualidade). Se não houver o bastante, não remove nada.</summary>
        public bool Remove(string itemId, int quantity)
        {
            if (quantity <= 0 || Count(itemId) < quantity)
                return false;

            var remaining = quantity;
            for (var i = 0; i < _slots.Length && remaining > 0; i++)
            {
                if (_slots[i].IsEmpty || _slots[i].ItemId != itemId)
                    continue;

                var take = Math.Min(_slots[i].Quantity, remaining);
                _slots[i] = _slots[i].WithQuantity(_slots[i].Quantity - take);
                remaining -= take;
                Mark(i);
            }

            Flush();
            return true;
        }

        // ------------------------------------------------------------------ pilha na mão

        /// <summary>Tira até <paramref name="amount"/> unidades do slot e devolve o que saiu.</summary>
        public ItemStack Take(int slot, int amount = int.MaxValue)
        {
            CheckSlot(slot);
            var current = _slots[slot];
            if (current.IsEmpty || amount <= 0)
                return ItemStack.Empty;

            var taken = Math.Min(amount, current.Quantity);
            _slots[slot] = current.WithQuantity(current.Quantity - taken);
            Mark(slot);
            Flush();
            return current.WithQuantity(taken);
        }

        /// <summary>
        /// Solta a pilha da mão no slot. Slot vazio: guarda. Mesma pilha: junta até o limite.
        /// Item diferente: troca. Devolve o que continua na mão (sobra ou o item que estava no slot).
        /// </summary>
        public ItemStack Place(int slot, ItemStack stack)
        {
            CheckSlot(slot);
            if (stack.IsEmpty)
                return ItemStack.Empty;

            var max = MaxStackOf(stack.ItemId);
            var current = _slots[slot];
            ItemStack leftover;

            if (current.IsEmpty)
            {
                var put = Math.Min(max, stack.Quantity);
                _slots[slot] = stack.WithQuantity(put);
                leftover = stack.WithQuantity(stack.Quantity - put);
            }
            else if (current.CanStackWith(stack))
            {
                var put = Math.Min(max - current.Quantity, stack.Quantity);
                _slots[slot] = current.WithQuantity(current.Quantity + put);
                leftover = stack.WithQuantity(stack.Quantity - put);
            }
            else
            {
                if (stack.Quantity > max)
                    return stack; // pilha maior que o limite não troca de lugar

                _slots[slot] = stack;
                leftover = current;
            }

            Mark(slot);
            Flush();
            return leftover;
        }

        /// <summary>Solta só uma unidade da pilha da mão. Se não der (item diferente ou slot cheio), nada muda.</summary>
        public ItemStack PlaceOne(int slot, ItemStack stack)
        {
            CheckSlot(slot);
            if (stack.IsEmpty)
                return ItemStack.Empty;

            var max = MaxStackOf(stack.ItemId);
            var current = _slots[slot];

            if (current.IsEmpty)
                _slots[slot] = stack.WithQuantity(1);
            else if (current.CanStackWith(stack) && current.Quantity < max)
                _slots[slot] = current.WithQuantity(current.Quantity + 1);
            else
                return stack;

            Mark(slot);
            Flush();
            return stack.WithQuantity(stack.Quantity - 1);
        }

        // ------------------------------------------------------------------ barra rápida

        public void SelectHotbar(int index)
        {
            if (index < 0 || index >= HotbarSize || index == SelectedHotbarIndex)
                return;

            SelectedHotbarIndex = index;
            SelectionChanged?.Invoke();
        }

        public void ScrollHotbar(int delta)
        {
            if (delta == 0)
                return;

            SelectHotbar(HotbarMath.Wrap(SelectedHotbarIndex, delta, HotbarSize));
        }

        // ------------------------------------------------------------------ save

        public ItemStack[] Snapshot()
        {
            var copy = new ItemStack[_slots.Length];
            Array.Copy(_slots, copy, _slots.Length);
            return copy;
        }

        /// <summary>Restaura os slots (carregar save). Pilhas acima do limite são cortadas; sobra de slots é ignorada.</summary>
        public void Restore(ItemStack[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            for (var i = 0; i < _slots.Length; i++)
            {
                var stack = i < data.Length ? data[i] : ItemStack.Empty;
                if (!stack.IsEmpty)
                    stack = stack.WithQuantity(Math.Min(stack.Quantity, MaxStackOf(stack.ItemId)));

                if (!_slots[i].Equals(stack))
                {
                    _slots[i] = stack;
                    Mark(i);
                }
            }

            Flush();
        }

        // ------------------------------------------------------------------ internos

        private int MaxStackOf(string itemId)
        {
            if (!_catalog.TryGetMaxStack(itemId, out var max))
                throw new ArgumentException($"Item desconhecido: '{itemId}'.", nameof(itemId));

            return Math.Max(1, max);
        }

        private void CheckSlot(int slot)
        {
            if (slot < 0 || slot >= _slots.Length)
                throw new ArgumentOutOfRangeException(nameof(slot));
        }

        private void Mark(int slot)
        {
            _pending[slot] = true;
            _anyPending = true;
        }

        private void Flush()
        {
            if (!_anyPending)
                return;

            _anyPending = false;
            for (var i = 0; i < _pending.Length; i++)
            {
                if (!_pending[i])
                    continue;

                _pending[i] = false;
                SlotChanged?.Invoke(i);
            }

            Changed?.Invoke();
        }
    }
}
