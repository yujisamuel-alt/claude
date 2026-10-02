namespace Enxada.Inventory
{
    /// <summary>Chaves da tabela de textos de cada item. O nome e a descrição seguem a convenção item.&lt;id&gt;.name / .desc.</summary>
    public static class ItemKeys
    {
        public static string Name(string itemId) => "item." + itemId + ".name";
        public static string Description(string itemId) => "item." + itemId + ".desc";
        public static string Quality(ItemQuality quality) => "quality." + quality.ToString().ToLowerInvariant();
        public static string Category(ItemCategory category) => "category." + category.ToString().ToLowerInvariant();
    }
}
