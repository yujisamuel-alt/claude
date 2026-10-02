using System;
using System.Collections.Generic;
using Enxada.Core;
using UnityEngine;

namespace Enxada.Inventory
{
    /// <summary>
    /// Cria, move e recolhe os itens no chão da cena. Reaproveita objetos (pool) e usa um único Update
    /// para todos os itens. Outros sistemas (quebrar pedra, colher) usam Spawn/Drop.
    /// </summary>
    public sealed class WorldItemSpawner : MonoBehaviour
    {
        [Serializable]
        public struct InitialDrop
        {
            public ItemDefinition item;
            [Min(1)] public int quantity;
            public ItemQuality quality;
            public Vector2 position;
        }

        // Os ícones têm 16 px; no chão ficam um pouco menores que um tile.
        private const float WorldScale = 0.65f;
        private const int SortingOrder = 3;

        [Tooltip("Itens que já começam no chão (para testes ou itens do cenário).")]
        [SerializeField] private InitialDrop[] initialDrops;

        private readonly List<WorldItem> _active = new List<WorldItem>();
        private readonly Stack<WorldItem> _pool = new Stack<WorldItem>();

        private InventoryModel _inventory;
        private ItemDatabase _database;
        private InventoryConfig _config;
        private IPlayerAnchor _player;

        private void Awake() => ServiceLocator.Replace(this);

        private void OnDestroy() => ServiceLocator.Unregister(this);

        private void Start()
        {
            _inventory = ServiceLocator.Get<InventoryModel>();
            _database = ServiceLocator.Get<ItemDatabase>();
            _config = ServiceLocator.Get<InventoryConfig>();

            if (initialDrops == null)
                return;

            foreach (var drop in initialDrops)
            {
                if (drop.item != null)
                    Spawn(new ItemStack(drop.item.Id, drop.quantity, drop.quality), drop.position, Vector2.zero, 0f);
            }
        }

        /// <summary>Joga o item no chão a partir de uma posição, arremessando na direção dada.</summary>
        public void Drop(ItemStack stack, Vector2 position, Vector2 direction) =>
            Spawn(stack, position, direction * _config.TossSpeed, _config.DropPickupDelay);

        public void Spawn(ItemStack stack, Vector2 position, Vector2 velocity, float pickupDelay)
        {
            if (stack.IsEmpty || !_database.TryGet(stack.ItemId, out var definition))
                return;

            var item = _pool.Count > 0 ? _pool.Pop() : CreateItem();
            item.Stack = stack;
            item.Age = 0f;
            item.PickupDelay = pickupDelay;
            item.Velocity = velocity;
            item.Renderer.sprite = definition.Icon;
            item.transform.position = position;
            item.gameObject.SetActive(true);
            _active.Add(item);
        }

        private WorldItem CreateItem()
        {
            var go = new GameObject("WorldItem", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * WorldScale;
            var item = go.AddComponent<WorldItem>();
            item.Renderer.sortingOrder = SortingOrder;
            return item;
        }

        private void Update()
        {
            if (_active.Count == 0)
                return;

            if (_player == null && !ServiceLocator.TryGet(out _player))
                return;

            var dt = Time.deltaTime;
            var playerPosition = (Vector2)_player.Transform.position;

            // De trás para frente, para poder remover itens durante o laço.
            for (var i = _active.Count - 1; i >= 0; i--)
            {
                var item = _active[i];
                item.Age += dt;
                Toss(item, dt);

                var position = (Vector2)item.transform.position;
                var toPlayer = playerPosition - position;
                var distance = toPlayer.magnitude;

                if (!PickupRules.ShouldAttract(distance, _config.AttractRadius, item.Age, item.PickupDelay)
                    || !_inventory.CanAdd(item.Stack))
                    continue;

                if (PickupRules.ShouldCollect(distance, _config.CollectRadius))
                {
                    Collect(item, i);
                    continue;
                }

                var speed = PickupRules.AttractSpeed(distance, _config.AttractRadius,
                    _config.MinAttractSpeed, _config.MaxAttractSpeed);
                item.Velocity = Vector2.zero;
                item.transform.position = position + toPlayer / distance * Mathf.Min(speed * dt, distance);
            }
        }

        private void Toss(WorldItem item, float dt)
        {
            if (item.Velocity == Vector2.zero)
                return;

            item.transform.position += (Vector3)(item.Velocity * dt);
            item.Velocity = Vector2.MoveTowards(item.Velocity, Vector2.zero, _config.TossFriction * dt);
        }

        private void Collect(WorldItem item, int activeIndex)
        {
            var leftover = _inventory.TryAdd(item.Stack);
            if (leftover.IsEmpty)
            {
                Despawn(item, activeIndex);
                return;
            }

            // Coube só parte: o resto continua no chão e espera um pouco antes de tentar de novo.
            item.Stack = leftover;
            item.Age = 0f;
            item.PickupDelay = 1f;
        }

        private void Despawn(WorldItem item, int activeIndex)
        {
            _active.RemoveAt(activeIndex);
            item.gameObject.SetActive(false);
            _pool.Push(item);
        }
    }
}
