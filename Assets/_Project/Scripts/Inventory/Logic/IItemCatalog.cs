namespace Enxada.Inventory
{
    /// <summary>O que o inventário precisa saber sobre cada item. O ItemDatabase (Unity) implementa isto.</summary>
    public interface IItemCatalog
    {
        /// <summary>Tamanho máximo da pilha (1 para ferramentas). Falso se o item não existe.</summary>
        bool TryGetMaxStack(string itemId, out int maxStack);
    }
}
