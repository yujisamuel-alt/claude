using UnityEngine;

namespace Enxada.Calendar
{
    /// <summary>Chance de chuva por estação (balanceamento). O jogo sempre começa com sol.</summary>
    [CreateAssetMenu(menuName = "Enxada/Config/Weather Config", fileName = "WeatherConfig")]
    public sealed class WeatherConfig : ScriptableObject
    {
        [Range(0f, 1f)] [SerializeField] private float springRainChance = 0.20f;
        [Range(0f, 1f)] [SerializeField] private float summerRainChance = 0.15f;
        [Range(0f, 1f)] [SerializeField] private float autumnRainChance = 0.20f;
        [Range(0f, 1f)] [SerializeField] private float winterRainChance;

        public WeatherSettings ToSettings() =>
            new WeatherSettings(springRainChance, summerRainChance, autumnRainChance, winterRainChance);
    }
}
