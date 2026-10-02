#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Enxada.Calendar;
using Enxada.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Enxada.Farming
{
    /// <summary>
    /// Atalhos de teste da fazenda (só no editor e em builds de desenvolvimento):
    /// F5 rega todas as terras aradas · F6 faz chover amanhã.
    /// </summary>
    public sealed class FarmDebugKeys : MonoBehaviour
    {
        [SerializeField] private FarmTilemapController farm;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.f5Key.wasPressedThisFrame && farm != null)
                farm.DebugWaterAll();

            if (keyboard.f6Key.wasPressedThisFrame && ServiceLocator.TryGet<WeatherModel>(out var weather))
            {
                weather.Set(weather.Today, Weather.Rainy);
                Debug.Log("[Debug] Vai chover amanhã.");
            }
        }
    }
}
#endif
