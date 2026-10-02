using System;
using System.Reflection;
using Enxada.Core;
using UnityEngine;

namespace Enxada.Calendar
{
    /// <summary>
    /// Muda a cor da Global Light 2D do amanhecer ao anoitecer, seguindo o gradiente do DayNightConfig.
    /// A cor é definida por reflexão (propriedade "color" da Light2D) porque o assembly da Light2D muda
    /// entre versões do URP; assim este módulo compila em qualquer uma. Só reaplica quando a cor muda
    /// de forma visível, então quase não gera lixo.
    /// </summary>
    public sealed class DayNightLighting : MonoBehaviour
    {
        // Variação mínima do progresso do dia (0–1) para reaplicar a cor. 0,002 ≈ 2,4 minutos de jogo.
        private const float MinProgressStep = 0.002f;

        [SerializeField] private DayNightConfig config;
        [Tooltip("O componente Light2D (tipo Global) da cena.")]
        [SerializeField] private Component globalLight;

        private GameClock _clock;
        private PropertyInfo _colorProperty;
        private float _lastProgress = -1f;

        private void Start()
        {
            if (config == null || globalLight == null)
            {
                Debug.LogError("[DayNightLighting] Config ou Light2D não atribuídos.", this);
                enabled = false;
                return;
            }

            _colorProperty = globalLight.GetType().GetProperty("color", BindingFlags.Public | BindingFlags.Instance);
            if (_colorProperty == null || _colorProperty.PropertyType != typeof(Color))
            {
                Debug.LogError("[DayNightLighting] O componente não tem a propriedade 'color'. Era uma Light2D?", this);
                enabled = false;
                return;
            }

            _clock = ServiceLocator.Get<GameClock>();
            Apply(_clock.DayProgress);
        }

        private void Update()
        {
            var progress = _clock.DayProgress;

            // Voltou para o início (novo dia) ou avançou o bastante.
            if (progress < _lastProgress || progress - _lastProgress >= MinProgressStep)
                Apply(progress);
        }

        private void Apply(float progress)
        {
            _lastProgress = progress;
            _colorProperty.SetValue(globalLight, config.Evaluate(progress));
        }
    }
}
