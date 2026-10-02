using System;

namespace Enxada.Calendar
{
    public enum Weather
    {
        Sunny,
        Rainy
    }

    /// <summary>Chance de chuva por estação (índices na ordem do enum Season). Neve e tempestade ficam para depois.</summary>
    public sealed class WeatherSettings
    {
        private readonly float[] _rainChanceBySeason;

        public WeatherSettings(float spring = 0.20f, float summer = 0.15f, float autumn = 0.20f, float winter = 0f)
        {
            _rainChanceBySeason = new[] { spring, summer, autumn, winter };
            foreach (var chance in _rainChanceBySeason)
            {
                if (chance < 0f || chance > 1f)
                    throw new ArgumentException("A chance de chuva é uma fração entre 0 e 1.");
            }
        }

        public float RainChance(Season season) => _rainChanceBySeason[(int)season];
    }

    /// <summary>
    /// Clima de hoje e de amanhã (a previsão). Ao virar o dia, o amanhã vira hoje e um novo amanhã é sorteado.
    /// O jogo sempre começa com sol.
    /// </summary>
    public sealed class WeatherModel
    {
        public WeatherModel(Weather today = Weather.Sunny, Weather tomorrow = Weather.Sunny)
        {
            Today = today;
            Tomorrow = tomorrow;
        }

        public Weather Today { get; private set; }
        public Weather Tomorrow { get; private set; }

        public bool IsRainingToday => Today == Weather.Rainy;

        public event Action Changed;

        /// <summary>Sorteia só a previsão de amanhã (usado ao criar um jogo novo).</summary>
        public void RollTomorrow(Season season, WeatherSettings settings, Func<double> random)
        {
            Tomorrow = Roll(season, settings, random);
            Changed?.Invoke();
        }

        /// <summary>Vira o dia: o clima de amanhã vira o de hoje e sorteia um novo amanhã.</summary>
        public void AdvanceDay(Season newSeason, WeatherSettings settings, Func<double> random)
        {
            Today = Tomorrow;
            Tomorrow = Roll(newSeason, settings, random);
            Changed?.Invoke();
        }

        /// <summary>Define o clima direto (carregar save e ferramentas de teste).</summary>
        public void Set(Weather today, Weather tomorrow)
        {
            Today = today;
            Tomorrow = tomorrow;
            Changed?.Invoke();
        }

        private static Weather Roll(Season season, WeatherSettings settings, Func<double> random) =>
            random() < settings.RainChance(season) ? Weather.Rainy : Weather.Sunny;
    }
}
