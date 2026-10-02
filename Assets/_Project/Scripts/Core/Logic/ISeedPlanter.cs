namespace Enxada.Core
{
    /// <summary>Quem sabe plantar uma semente em um tile. A fazenda implementa.</summary>
    public interface ISeedPlanter
    {
        /// <summary>Used = plantou (o chamador gasta a semente); None = não dá aqui, sem aviso; Rejected = não dá, com aviso.</summary>
        ToolUseOutcome TryPlant(string seedItemId, CellPosition cell);
    }
}
