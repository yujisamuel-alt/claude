using System;
using System.Collections;
using Enxada.Core;
using Enxada.Inventory;
using UnityEngine;

namespace Enxada.Farming
{
    /// <summary>Uma pedra, um galho, um tufo de mato ou uma árvore. Aguenta golpes da ferramenta certa e solta itens ao quebrar.</summary>
    public sealed class ResourceNode : MonoBehaviour, IToolTarget
    {
        private const float ShakeSeconds = 0.15f;
        private const float ShakeAmount = 0.06f;
        private const float DropScatterSpeed = 2.5f;
        private const float DropPickupDelay = 0.35f;

        private int _hitsLeft;
        private Vector3 _restPosition;

        public ResourceNodeDefinition Definition { get; private set; }
        public CellPosition Cell { get; private set; }

        /// <summary>Disparado ao quebrar, antes de o objeto sumir.</summary>
        public event Action<ResourceNode> Removed;

        public void Initialize(ResourceNodeDefinition definition, CellPosition cell)
        {
            Definition = definition;
            Cell = cell;
            _hitsLeft = definition.HitsToBreak;
            _restPosition = transform.position;
        }

        public ToolUseOutcome OnToolUsed(ToolType tool, int tier)
        {
            if (Definition == null || tool != Definition.RequiredTool)
                return ToolUseOutcome.None;

            if (!ToolRules.MeetsTier(Definition.MinTier, tier))
                return ToolUseOutcome.Rejected("toast.tool_weak");

            _hitsLeft -= ToolRules.HitDamage(tier);
            if (_hitsLeft <= 0)
                Break();
            else
                StartCoroutine(Shake());

            return ToolUseOutcome.Used;
        }

        private void Break()
        {
            DropItems();
            Removed?.Invoke(this);
            Destroy(gameObject);
        }

        private void DropItems()
        {
            var item = Definition.DropItem;
            if (item == null || !ServiceLocator.TryGet<WorldItemSpawner>(out var spawner))
                return;

            var amount = UnityEngine.Random.Range(Definition.DropMin, Definition.DropMax + 1);
            if (amount <= 0)
                return;

            spawner.Spawn(new ItemStack(item.Id, amount), transform.position,
                UnityEngine.Random.insideUnitCircle.normalized * DropScatterSpeed, DropPickupDelay);
        }

        private IEnumerator Shake()
        {
            for (var elapsed = 0f; elapsed < ShakeSeconds; elapsed += Time.deltaTime)
            {
                var offset = UnityEngine.Random.insideUnitCircle * ShakeAmount;
                transform.position = _restPosition + new Vector3(offset.x, 0f, 0f);
                yield return null;
            }

            transform.position = _restPosition;
        }
    }
}
