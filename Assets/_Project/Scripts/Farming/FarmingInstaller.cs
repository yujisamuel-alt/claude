using Enxada.Core;
using UnityEngine;

namespace Enxada.Farming
{
    /// <summary>Registra o estado da fazenda (terra, plantas e regador). Fica no Bootstrap, então sobrevive às trocas de cena.</summary>
    public sealed class FarmingInstaller : ServiceInstaller
    {
        [SerializeField] private FarmingConfig config;
        [SerializeField] private CropDatabase cropDatabase;

        public override void Install()
        {
            if (config == null || cropDatabase == null)
            {
                Debug.LogError("[FarmingInstaller] FarmingConfig ou CropDatabase não atribuídos.", this);
                return;
            }

            ServiceLocator.Register(config);
            ServiceLocator.Register(cropDatabase);
            ServiceLocator.Register(new FarmGrid());
            ServiceLocator.Register(new WateringCanState(config.WateringCanCapacity));
        }
    }
}
