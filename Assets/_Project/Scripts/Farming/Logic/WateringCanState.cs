using System;

namespace Enxada.Farming
{
    /// <summary>Quanta água o regador tem. Gasta 1 por tile regado; enche no poço ou no rio.</summary>
    public sealed class WateringCanState
    {
        public WateringCanState(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            Capacity = capacity;
            Level = capacity;
        }

        public int Capacity { get; }
        public int Level { get; private set; }
        public bool HasWater => Level > 0;

        public event Action Changed;

        public bool TryUse()
        {
            if (Level <= 0)
                return false;

            Level--;
            Changed?.Invoke();
            return true;
        }

        public void Refill()
        {
            if (Level == Capacity)
                return;

            Level = Capacity;
            Changed?.Invoke();
        }

        /// <summary>Define o nível direto (carregar save).</summary>
        public void SetLevel(int level)
        {
            Level = Math.Max(0, Math.Min(Capacity, level));
            Changed?.Invoke();
        }
    }
}
