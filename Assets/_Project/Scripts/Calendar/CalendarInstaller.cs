using Enxada.Core;
using UnityEngine;

namespace Enxada.Calendar
{
    /// <summary>Cria o GameClock e o registra como serviço.</summary>
    public sealed class CalendarInstaller : ServiceInstaller
    {
        [SerializeField] private ClockConfig clockConfig;

        public override void Install()
        {
            if (clockConfig == null)
            {
                Debug.LogError("[CalendarInstaller] ClockConfig não atribuído.", this);
                return;
            }

            ServiceLocator.Register(new GameClock(clockConfig.ToSettings(), GameDate.Start));
        }
    }
}
