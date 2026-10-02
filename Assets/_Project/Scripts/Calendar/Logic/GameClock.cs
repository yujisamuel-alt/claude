using System;

namespace Enxada.Calendar
{
    public enum DayEndReason
    {
        Slept,
        PassedOut
    }

    /// <summary>Resumo de um dia que acabou. Quem processa o fim do dia (plantas, vendas, save) lê isto.</summary>
    public readonly struct DayEndedInfo
    {
        public readonly GameDate EndedDate;
        public readonly GameDate NewDate;
        public readonly DayEndReason Reason;

        public DayEndedInfo(GameDate endedDate, GameDate newDate, DayEndReason reason)
        {
            EndedDate = endedDate;
            NewDate = newDate;
            Reason = reason;
        }

        public bool SeasonChanged => EndedDate.Season != NewDate.Season;
        public bool YearChanged => EndedDate.Year != NewDate.Year;
    }

    /// <summary>
    /// Relógio e calendário do jogo. Avança em blocos (padrão: 10 minutos de jogo a cada 7 segundos reais).
    /// Não sabe nada de Unity nem de pausa: quem o alimenta (ClockDriver) decide quando chamar Tick.
    /// Ao chegar no fim do dia ele para e avisa (PassOutReached); o dia só vira quando alguém chama EndDay.
    /// </summary>
    public sealed class GameClock
    {
        private float _accumulatedSeconds;
        private bool _lateWarned;
        private bool _passOutRaised;

        public ClockSettings Settings { get; }
        public GameDate Date { get; private set; }

        /// <summary>Minutos desde 0h do dia de jogo. Vai de 360 (6h) até 1560 (2h da madrugada).</summary>
        public int MinuteOfDay { get; private set; }

        /// <summary>Qualquer mudança de hora ou data.</summary>
        public event Action Changed;

        /// <summary>Uma vez por dia, quando o relógio chega na hora do aviso (meia-noite).</summary>
        public event Action LateWarning;

        /// <summary>O relógio chegou no fim do dia e parou. Alguém precisa chamar EndDay(PassedOut).</summary>
        public event Action PassOutReached;

        public event Action<DayEndedInfo> DayEnded;

        public GameClock(ClockSettings settings, GameDate startDate)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Date = startDate;
            MinuteOfDay = settings.DayStartMinute;
        }

        public int Hour => MinuteOfDay / 60 % 24;
        public int Minute => MinuteOfDay % 60;
        public bool IsAfterMidnight => MinuteOfDay >= ClockSettings.MinutesPerDay;
        public bool IsWaitingForDayEnd => _passOutRaised;

        /// <summary>0 a 1 dentro do bloco atual (para animar a luz suavemente entre dois blocos).</summary>
        public float TickProgress => _passOutRaised ? 0f : Math.Min(_accumulatedSeconds / Settings.RealSecondsPerTick, 1f);

        public float FractionalMinute => MinuteOfDay + TickProgress * Settings.TickMinutes;

        /// <summary>0 no começo do dia (6h) e 1 no fim (2h). Alimenta o gradiente de luz.</summary>
        public float DayProgress
        {
            get
            {
                var span = Settings.DayEndMinute - Settings.DayStartMinute;
                var progress = (Math.Min(FractionalMinute, Settings.DayEndMinute) - Settings.DayStartMinute) / span;
                return Math.Max(0f, Math.Min(1f, progress));
            }
        }

        public void Tick(float deltaSeconds)
        {
            // "!(x > 0)" também descarta NaN.
            if (_passOutRaised || !(deltaSeconds > 0f))
                return;

            _accumulatedSeconds += deltaSeconds;
            while (_accumulatedSeconds >= Settings.RealSecondsPerTick && !_passOutRaised)
            {
                _accumulatedSeconds -= Settings.RealSecondsPerTick;
                AdvanceOneTick();
            }

            if (_passOutRaised)
                _accumulatedSeconds = 0f;
        }

        /// <summary>Encerra o dia: avança a data, volta o relógio para o início e avisa os sistemas.</summary>
        public DayEndedInfo EndDay(DayEndReason reason)
        {
            var endedDate = Date;
            Date = endedDate.NextDay();
            MinuteOfDay = Settings.DayStartMinute;
            _accumulatedSeconds = 0f;
            _lateWarned = false;
            _passOutRaised = false;

            var info = new DayEndedInfo(endedDate, Date, reason);
            DayEnded?.Invoke(info);
            Changed?.Invoke();
            return info;
        }

        /// <summary>Define data e hora diretamente (carregar save, ferramentas de teste). Não dispara avisos.</summary>
        public void Restore(GameDate date, int minuteOfDay)
        {
            Date = date;
            MinuteOfDay = Math.Max(Settings.DayStartMinute, Math.Min(minuteOfDay, Settings.DayEndMinute));
            _accumulatedSeconds = 0f;
            _lateWarned = MinuteOfDay >= Settings.LateWarningMinute;
            _passOutRaised = MinuteOfDay >= Settings.DayEndMinute;
            Changed?.Invoke();
        }

        private void AdvanceOneTick()
        {
            MinuteOfDay = Math.Min(MinuteOfDay + Settings.TickMinutes, Settings.DayEndMinute);
            Changed?.Invoke();

            if (!_lateWarned && MinuteOfDay >= Settings.LateWarningMinute)
            {
                _lateWarned = true;
                LateWarning?.Invoke();
            }

            if (MinuteOfDay >= Settings.DayEndMinute)
            {
                _passOutRaised = true;
                PassOutReached?.Invoke();
            }
        }
    }
}
