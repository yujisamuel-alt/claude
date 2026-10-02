namespace Enxada.Core
{
    /// <summary>Quem sabe o que uma ferramenta faz em um tile do chão (arar, regar). A fazenda implementa.</summary>
    public interface ITileToolHandler
    {
        ToolUseOutcome TryUse(ToolType tool, int tier, CellPosition cell);
    }
}
