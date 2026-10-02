using Enxada.Core;
using UnityEngine;

namespace Enxada.Farming
{
    /// <summary>Poço (ou qualquer fonte de água): usar o regador aqui enche o regador, sem gastar energia.</summary>
    public sealed class WaterSource : MonoBehaviour, IToolTarget
    {
        public ToolUseOutcome OnToolUsed(ToolType tool, int tier)
        {
            if (tool != ToolType.WateringCan || !ServiceLocator.TryGet<WateringCanState>(out var can))
                return ToolUseOutcome.None;

            can.Refill();
            return ToolUseOutcome.UsedFree;
        }
    }
}
