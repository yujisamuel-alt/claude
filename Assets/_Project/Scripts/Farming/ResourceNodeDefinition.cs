using Enxada.Core;
using Enxada.Inventory;
using UnityEngine;

namespace Enxada.Farming
{
    /// <summary>Tipo de objeto do mundo que se quebra com ferramenta: mato, galho, pedra, árvore.</summary>
    [CreateAssetMenu(menuName = "Enxada/World/Resource Node", fileName = "NewResourceNode")]
    public sealed class ResourceNodeDefinition : ScriptableObject
    {
        [SerializeField] private Sprite sprite;
        [SerializeField] private ToolType requiredTool = ToolType.Axe;

        [Tooltip("Nível mínimo da ferramenta (0 = básica).")]
        [Min(0)] [SerializeField] private int minTier;

        [Tooltip("Quantos golpes de ferramenta básica são necessários.")]
        [Min(1)] [SerializeField] private int hitsToBreak = 1;

        [Tooltip("Item que cai ao quebrar. Vazio = não solta nada.")]
        [SerializeField] private ItemDefinition dropItem;

        [Min(0)] [SerializeField] private int dropMin = 1;
        [Min(0)] [SerializeField] private int dropMax = 1;

        [Tooltip("Desligado = o jogador passa por cima (ex.: mato).")]
        [SerializeField] private bool blocksMovement = true;

        public Sprite Sprite => sprite;
        public ToolType RequiredTool => requiredTool;
        public int MinTier => minTier;
        public int HitsToBreak => hitsToBreak;
        public ItemDefinition DropItem => dropItem;
        public int DropMin => dropMin;
        public int DropMax => Mathf.Max(dropMin, dropMax);
        public bool BlocksMovement => blocksMovement;

        /// <summary>Usado pelos scripts de setup do editor.</summary>
        public void Initialize(Sprite newSprite, ToolType tool, int hits, ItemDefinition drop, int min, int max,
            bool blocks)
        {
            sprite = newSprite;
            requiredTool = tool;
            hitsToBreak = hits;
            dropItem = drop;
            dropMin = min;
            dropMax = max;
            blocksMovement = blocks;
        }
    }
}
