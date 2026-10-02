using Enxada.Core;
using UnityEngine;

namespace Enxada.Calendar
{
    /// <summary>Alimenta o GameClock com o tempo real, parando quando o jogo está pausado.</summary>
    public sealed class ClockDriver : MonoBehaviour
    {
        // Evita que um travamento longo (ex.: arrastar a janela) pule horas do dia de uma vez.
        private const float MaxDeltaSeconds = 0.25f;

        private GameClock _clock;
        private GameplayPause _pause;

        /// <summary>Multiplicador de velocidade (só para testes; o normal é 1).</summary>
        public float TimeScale { get; set; } = 1f;

        private void Start()
        {
            _clock = ServiceLocator.Get<GameClock>();
            _pause = ServiceLocator.Get<GameplayPause>();
        }

        private void Update()
        {
            if (_pause.IsPaused)
                return;

            _clock.Tick(Mathf.Min(Time.deltaTime, MaxDeltaSeconds) * TimeScale);
        }
    }
}
