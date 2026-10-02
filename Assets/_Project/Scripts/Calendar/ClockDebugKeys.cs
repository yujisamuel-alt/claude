#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Enxada.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Enxada.Calendar
{
    /// <summary>
    /// Atalhos de teste (só no editor e em builds de desenvolvimento):
    /// F2 alterna velocidade 1x / 20x · F3 pula para 23:40 · F4 vai direto para o fim do dia (dormir).
    /// </summary>
    public sealed class ClockDebugKeys : MonoBehaviour
    {
        private const float FastScale = 20f;

        [SerializeField] private ClockDriver driver;
        [SerializeField] private DayTransitionController transition;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.f2Key.wasPressedThisFrame && driver != null)
            {
                driver.TimeScale = Mathf.Approximately(driver.TimeScale, 1f) ? FastScale : 1f;
                Debug.Log($"[Debug] Velocidade do relógio: {driver.TimeScale}x");
            }

            if (keyboard.f3Key.wasPressedThisFrame && ServiceLocator.TryGet<GameClock>(out var clock))
                clock.Restore(clock.Date, 23 * 60 + 40);

            if (keyboard.f4Key.wasPressedThisFrame && transition != null)
                transition.RequestSleep();
        }
    }
}
#endif
