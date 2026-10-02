namespace Enxada.Core
{
    /// <summary>
    /// Objeto do mundo que reage a ferramentas (pedra, galho, mato, poço). Precisa de um Collider2D
    /// cobrindo o tile; o ToolUser o encontra pelo tile alvo.
    /// </summary>
    public interface IToolTarget
    {
        ToolUseOutcome OnToolUsed(ToolType tool, int tier);
    }
}
