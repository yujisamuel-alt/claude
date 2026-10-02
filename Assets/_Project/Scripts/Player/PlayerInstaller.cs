using Enxada.Core;
using UnityEngine;

namespace Enxada.Player
{
    /// <summary>Registra a energia do jogador e a configuração das ferramentas.</summary>
    public sealed class PlayerInstaller : ServiceInstaller
    {
        [SerializeField] private EnergyConfig energyConfig;
        [SerializeField] private ToolConfig toolConfig;

        public override void Install()
        {
            if (energyConfig == null || toolConfig == null)
            {
                Debug.LogError("[PlayerInstaller] EnergyConfig ou ToolConfig não atribuídos.", this);
                return;
            }

            ServiceLocator.Register(new EnergyModel(energyConfig.ToSettings()));
            ServiceLocator.Register(toolConfig);
        }
    }
}
