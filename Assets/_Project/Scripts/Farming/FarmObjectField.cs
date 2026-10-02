using System;
using System.Collections.Generic;
using Enxada.Calendar;
using Enxada.Core;
using UnityEngine;

namespace Enxada.Farming
{
    /// <summary>
    /// Espalha mato, galhos, pedras e árvores pelo sítio ao começar e faz novos aparecerem aos poucos
    /// a cada dia, só em chão livre e fora da clareira inicial.
    /// </summary>
    public sealed class FarmObjectField : MonoBehaviour
    {
        // Abaixo do jogador (10) e dos itens no chão (3), junto dos obstáculos do mapa.
        private const int SortingOrder = 2;

        [SerializeField] private FarmObjectConfig config;
        [SerializeField] private FarmTilemapController farm;
        [SerializeField] private Grid grid;

        [Header("Área do sítio (tiles, inclusive)")]
        [SerializeField] private Vector2Int boundsMin;
        [SerializeField] private Vector2Int boundsMax;

        [Header("Clareira inicial, sempre livre (tiles, inclusive)")]
        [SerializeField] private Vector2Int clearingMin;
        [SerializeField] private Vector2Int clearingMax;

        [Tooltip("0 = aleatório a cada partida; outro valor = sempre o mesmo sítio (bom para testar).")]
        [SerializeField] private int seed;

        private readonly Dictionary<CellPosition, ResourceNode> _nodes = new Dictionary<CellPosition, ResourceNode>();
        private readonly List<CellPosition> _candidates = new List<CellPosition>();
        private readonly List<float> _weights = new List<float>();
        private System.Random _random;
        private GameClock _clock;

        public int ObjectCount => _nodes.Count;

        private void Start()
        {
            _random = seed == 0 ? new System.Random() : new System.Random(seed);
            _clock = ServiceLocator.Get<GameClock>();
            _clock.DayEnded += OnDayEnded;

            SpawnInitialObjects();
        }

        private void OnDestroy()
        {
            if (_clock != null)
                _clock.DayEnded -= OnDayEnded;
        }

        private void SpawnInitialObjects()
        {
            foreach (var entry in config.Entries)
            {
                if (entry.definition != null)
                    SpawnObjects(entry.initialCount, index => entry.definition);
            }
        }

        private void OnDayEnded(DayEndedInfo info)
        {
            var room = config.MaxObjects - _nodes.Count;
            var count = Math.Min(room, SpawnPlanner.RollCount(config.DailySpawnCount, _random.NextDouble));
            if (count <= 0)
                return;

            _weights.Clear();
            foreach (var entry in config.Entries)
                _weights.Add(entry.definition != null ? entry.weight : 0f);

            SpawnObjects(count, index =>
            {
                var chosen = SpawnPlanner.PickWeightedIndex(_weights, _random.NextDouble);
                return chosen < 0 ? null : config.Entries[chosen].definition;
            });
        }

        private void SpawnObjects(int count, Func<int, ResourceNodeDefinition> pickDefinition)
        {
            BuildCandidates();
            var cells = SpawnPlanner.PickCells(_candidates, count, _random.NextDouble);
            for (var i = 0; i < cells.Count; i++)
            {
                var definition = pickDefinition(i);
                if (definition != null)
                    Create(definition, cells[i]);
            }
        }

        private void BuildCandidates()
        {
            _candidates.Clear();
            for (var y = boundsMin.y; y <= boundsMax.y; y++)
            for (var x = boundsMin.x; x <= boundsMax.x; x++)
            {
                if (x >= clearingMin.x && x <= clearingMax.x && y >= clearingMin.y && y <= clearingMax.y)
                    continue;

                var cell = new CellPosition(x, y);
                if (!_nodes.ContainsKey(cell) && farm.IsFreeGround(cell))
                    _candidates.Add(cell);
            }
        }

        private void Create(ResourceNodeDefinition definition, CellPosition cell)
        {
            var go = new GameObject(definition.name);
            go.transform.SetParent(transform, false);
            go.transform.position = grid.GetCellCenterWorld(new Vector3Int(cell.X, cell.Y, 0));

            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = definition.Sprite;
            spriteRenderer.sortingOrder = SortingOrder;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.8f, 0.8f);
            box.isTrigger = !definition.BlocksMovement;

            var node = go.AddComponent<ResourceNode>();
            node.Initialize(definition, cell);
            node.Removed += OnNodeRemoved;
            _nodes[cell] = node;
        }

        private void OnNodeRemoved(ResourceNode node) => _nodes.Remove(node.Cell);
    }
}
