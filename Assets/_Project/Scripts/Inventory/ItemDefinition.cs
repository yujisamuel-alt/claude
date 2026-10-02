using UnityEngine;

namespace Enxada.Inventory
{
    /// <summary>
    /// Dados de um item. O nome e a descrição NÃO ficam aqui: vêm da tabela de textos pelas chaves
    /// item.&lt;id&gt;.name e item.&lt;id&gt;.desc (ver ItemKeys).
    /// </summary>
    [CreateAssetMenu(menuName = "Enxada/Items/Item Definition", fileName = "NewItem")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [Tooltip("Identificador único, em minúsculas e sem espaços (ex.: watering_can). Nunca mude depois de lançar: os saves usam isto.")]
        [SerializeField] private string id;

        [SerializeField] private ItemCategory category;
        [SerializeField] private Sprite icon;

        [Tooltip("Preço de venda em Tostões. 0 = não pode ser vendido.")]
        [Min(0)] [SerializeField] private int sellPrice;

        [Tooltip("Tamanho máximo da pilha. Ferramentas usam 1.")]
        [Range(1, 999)] [SerializeField] private int maxStack = 999;

        public string Id => id;
        public ItemCategory Category => category;
        public Sprite Icon => icon;
        public int SellPrice => sellPrice;
        public int MaxStack => maxStack;
        public bool IsStackable => maxStack > 1;

        /// <summary>Usado pelos scripts de setup do editor para criar itens por código.</summary>
        public void Initialize(string newId, ItemCategory newCategory, int newSellPrice, int newMaxStack, Sprite newIcon)
        {
            id = newId;
            category = newCategory;
            sellPrice = newSellPrice;
            maxStack = newMaxStack;
            icon = newIcon;
        }
    }
}
