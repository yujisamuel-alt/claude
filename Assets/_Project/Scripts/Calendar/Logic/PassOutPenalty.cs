using System;

namespace Enxada.Calendar
{
    public static class PassOutPenalty
    {
        /// <summary>Dinheiro perdido ao desmaiar: uma porcentagem do que o jogador tem, limitada pelo teto.</summary>
        public static int Calculate(int money, ClockSettings settings)
        {
            if (money <= 0)
                return 0;

            var loss = (int)Math.Floor(money * (double)settings.PassOutMoneyPercent);
            return Math.Max(0, Math.Min(Math.Min(loss, settings.PassOutMoneyCap), money));
        }
    }
}
