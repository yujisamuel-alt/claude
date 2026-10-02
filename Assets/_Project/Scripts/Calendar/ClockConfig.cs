using UnityEngine;

namespace Enxada.Calendar
{
    /// <summary>Configuração do relógio (balanceamento). Horas acima de 24 contam como madrugada: 26 = 2h.</summary>
    [CreateAssetMenu(menuName = "Enxada/Config/Clock Config", fileName = "ClockConfig")]
    public sealed class ClockConfig : ScriptableObject
    {
        [Tooltip("Hora em que o dia começa.")]
        [Range(0, 12)] [SerializeField] private int dayStartHour = 6;

        [Tooltip("Hora em que o jogador desmaia. 26 = 2h da madrugada.")]
        [Range(13, 30)] [SerializeField] private int dayEndHour = 26;

        [Tooltip("Hora do aviso 'tá ficando tarde'. 24 = meia-noite.")]
        [Range(13, 30)] [SerializeField] private int lateWarningHour = 24;

        [Tooltip("Minutos de jogo que passam a cada bloco.")]
        [Min(1)] [SerializeField] private int tickMinutes = 10;

        [Tooltip("Segundos reais que cada bloco dura.")]
        [Min(0.1f)] [SerializeField] private float realSecondsPerTick = 7f;

        [Tooltip("Fração do dinheiro perdida ao desmaiar (0,10 = 10%).")]
        [Range(0f, 1f)] [SerializeField] private float passOutMoneyPercent = 0.10f;

        [Tooltip("Teto da perda ao desmaiar, em Tostões.")]
        [Min(0)] [SerializeField] private int passOutMoneyCap = 1000;

        public ClockSettings ToSettings() => new ClockSettings(
            dayStartHour * 60, dayEndHour * 60, lateWarningHour * 60,
            tickMinutes, realSecondsPerTick, passOutMoneyPercent, passOutMoneyCap);
    }
}
