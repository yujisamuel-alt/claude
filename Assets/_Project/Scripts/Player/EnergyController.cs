using Enxada.Calendar;
using Enxada.Core;
using UnityEngine;

namespace Enxada.Player
{
    /// <summary>
    /// Liga a energia ao resto do jogo: recupera ao virar o dia (tudo se dormiu, uma fração se desmaiou)
    /// e pede o desmaio quando a energia passa do limite.
    /// </summary>
    public sealed class EnergyController : MonoBehaviour
    {
        [Tooltip("Pedido de desmaio por exaustão; quem conduz a virada do dia escuta este canal.")]
        [SerializeField] private VoidEventChannel passOutRequested;

        private EnergyModel _energy;
        private GameClock _clock;

        private void Start()
        {
            _energy = ServiceLocator.Get<EnergyModel>();
            _clock = ServiceLocator.Get<GameClock>();

            _clock.DayEnded += OnDayEnded;
            _energy.Exhausted += OnExhausted;
        }

        private void OnDestroy()
        {
            if (_clock != null)
                _clock.DayEnded -= OnDayEnded;
            if (_energy != null)
                _energy.Exhausted -= OnExhausted;
        }

        private void OnDayEnded(DayEndedInfo info) => _energy.NewDay(info.Reason == DayEndReason.PassedOut);

        private void OnExhausted()
        {
            if (passOutRequested != null)
                passOutRequested.Raise();
        }
    }
}
