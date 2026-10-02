using Enxada.Core;
using UnityEngine;

namespace Enxada.Calendar
{
    /// <summary>
    /// Cria o relógio e o clima e os registra como serviços. O clima vira o dia junto com o relógio:
    /// como este instalador se inscreve primeiro, quando qualquer outro sistema recebe "o dia acabou"
    /// o clima do novo dia já está valendo (a fazenda precisa saber se vai chover).
    /// </summary>
    public sealed class CalendarInstaller : ServiceInstaller
    {
        [SerializeField] private ClockConfig clockConfig;
        [SerializeField] private WeatherConfig weatherConfig;

        private GameClock _clock;
        private WeatherModel _weather;
        private WeatherSettings _weatherSettings;

        public override void Install()
        {
            if (clockConfig == null)
            {
                Debug.LogError("[CalendarInstaller] ClockConfig não atribuído.", this);
                return;
            }

            _clock = new GameClock(clockConfig.ToSettings(), GameDate.Start);
            ServiceLocator.Register(_clock);

            if (weatherConfig == null)
                Debug.LogWarning("[CalendarInstaller] WeatherConfig não atribuído; usando as chances padrão.", this);
            _weatherSettings = weatherConfig != null ? weatherConfig.ToSettings() : new WeatherSettings();

            _weather = new WeatherModel();
            _weather.RollTomorrow(_clock.Date.Season, _weatherSettings, NextRandom);
            ServiceLocator.Register(_weather);

            _clock.DayEnded += OnDayEnded;
        }

        private void OnDestroy()
        {
            if (_clock != null)
                _clock.DayEnded -= OnDayEnded;
        }

        private void OnDayEnded(DayEndedInfo info) =>
            _weather.AdvanceDay(info.NewDate.Season, _weatherSettings, NextRandom);

        private static double NextRandom() => Random.value;
    }
}
