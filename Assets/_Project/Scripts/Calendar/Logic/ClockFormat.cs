namespace Enxada.Calendar
{
    public static class ClockFormat
    {
        /// <summary>"06:00". Depois da meia-noite o relógio mostra 00:00, 01:00...</summary>
        public static string Format24h(int minuteOfDay)
        {
            var hour = minuteOfDay / 60 % 24;
            var minute = minuteOfDay % 60;
            return $"{hour:D2}:{minute:D2}";
        }
    }
}
