using Enxada.Calendar;
using Enxada.Core;
using TMPro;
using UnityEngine;

namespace Enxada.UI
{
    /// <summary>HUD: hora, data e estação, mais os avisos de hora tardia e desmaio.</summary>
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] private TMP_Text timeLabel;
        [SerializeField] private TMP_Text dateLabel;
        [SerializeField] private TMP_Text weatherLabel;
        [SerializeField] private ToastView toast;

        private GameClock _clock;
        private WeatherModel _weather;
        private ITextProvider _texts;

        private void Start()
        {
            _clock = ServiceLocator.Get<GameClock>();
            _texts = ServiceLocator.Get<ITextProvider>();

            if (ServiceLocator.TryGet(out _weather))
                _weather.Changed += Refresh;

            _clock.Changed += Refresh;
            _clock.LateWarning += OnLateWarning;
            _clock.DayEnded += OnDayEnded;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_weather != null)
                _weather.Changed -= Refresh;

            if (_clock == null)
                return;

            _clock.Changed -= Refresh;
            _clock.LateWarning -= OnLateWarning;
            _clock.DayEnded -= OnDayEnded;
        }

        private void Refresh()
        {
            timeLabel.text = ClockFormat.Format24h(_clock.MinuteOfDay);

            var date = _clock.Date;
            dateLabel.text = _texts.Format("hud.date",
                _texts.Get(TextKeys.WeekdayShort(date.Weekday)),
                date.Day,
                _texts.Get(TextKeys.Season(date.Season)));

            if (weatherLabel != null && _weather != null)
                weatherLabel.text = _texts.Get(_weather.IsRainingToday ? "weather.rainy" : "weather.sunny");
        }

        private void OnLateWarning() => toast.Show(_texts.Get("toast.late"));

        private void OnDayEnded(DayEndedInfo info)
        {
            if (info.Reason == DayEndReason.PassedOut)
                toast.Show(_texts.Get("toast.passout"));
        }
    }
}
