using Enxada.Core;
using UnityEngine;

namespace Enxada.Farming
{
    /// <summary>Registra o estado da fazenda (terra arada e regador). Fica no Bootstrap, então sobrevive às trocas de cena.</summary>
    public sealed class FarmingInstaller : ServiceInstaller
    {
        [SerializeField] private FarmingConfig config;

        public override void Install()
        {
            if (config == null)
            {
                Debug.LogError("[FarmingInstaller] FarmingConfig não atribuído.", this);
                return;
            }

            ServiceLocator.Register(config);
            ServiceLocator.Register(new FarmGrid());
            ServiceLocator.Register(new WateringCanState(config.WateringCanCapacity));
        }
    }
}
