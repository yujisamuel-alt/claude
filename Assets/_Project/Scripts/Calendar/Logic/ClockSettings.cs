using System;

namespace Enxada.Calendar
{
    /// <summary>
    /// Parâmetros do relógio, em minutos desde 0h do dia de jogo (2h da madrugada = 26h = 1560).
    /// Lógica pura: o ScriptableObject ClockConfig só converte para esta classe.
    /// </summary>
    public sealed class ClockSettings
    {
        public const int MinutesPerDay = 24 * 60;

        public int DayStartMinute { get; }
        public int DayEndMinute { get; }
        public int LateWarningMinute { get; }
        public int TickMinutes { get; }
        public float RealSecondsPerTick { get; }
        public float PassOutMoneyPercent { get; }
        public int PassOutMoneyCap { get; }

        public ClockSettings(int dayStartMinute = 6 * 60, int dayEndMinute = 26 * 60,
            int lateWarningMinute = 24 * 60, int tickMinutes = 10, float realSecondsPerTick = 7f,
            float passOutMoneyPercent = 0.10f, int passOutMoneyCap = 1000)
        {
            if (dayEndMinute <= dayStartMinute)
                throw new ArgumentException("O fim do dia precisa ser depois do início.");
            if (lateWarningMinute < dayStartMinute || lateWarningMinute > dayEndMinute)
                throw new ArgumentException("O aviso de hora tardia precisa estar entre o início e o fim do dia.");
            if (tickMinutes <= 0)
                throw new ArgumentException("O bloco de tempo precisa ser positivo.");
            if (!(realSecondsPerTick > 0f))
                throw new ArgumentException("Os segundos reais por bloco precisam ser positivos.");
            if (passOutMoneyPercent < 0f || passOutMoneyPercent > 1f)
                throw new ArgumentException("A perda por desmaio é uma fração entre 0 e 1.");
            if (passOutMoneyCap < 0)
                throw new ArgumentException("O teto da perda por desmaio não pode ser negativo.");

            DayStartMinute = dayStartMinute;
            DayEndMinute = dayEndMinute;
            LateWarningMinute = lateWarningMinute;
            TickMinutes = tickMinutes;
            RealSecondsPerTick = realSecondsPerTick;
            PassOutMoneyPercent = passOutMoneyPercent;
            PassOutMoneyCap = passOutMoneyCap;
        }
    }
}
