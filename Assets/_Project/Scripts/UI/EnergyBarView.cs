using Enxada.Core;
using Enxada.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Enxada.UI
{
    /// <summary>Barra vertical de energia (canto inferior direito): verde cheia, amarela no meio, vermelha quase vazia.</summary>
    public sealed class EnergyBarView : MonoBehaviour
    {
        private static readonly Color Full = new Color(0.35f, 0.8f, 0.3f, 1f);
        private static readonly Color Half = new Color(0.95f, 0.8f, 0.2f, 1f);
        private static readonly Color Low = new Color(0.9f, 0.25f, 0.2f, 1f);

        [Tooltip("Retângulo que cresce de baixo para cima conforme a energia.")]
        [SerializeField] private RectTransform fill;
        [SerializeField] private Image fillImage;

        private EnergyModel _energy;

        private void Start()
        {
            _energy = ServiceLocator.Get<EnergyModel>();
            _energy.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_energy != null)
                _energy.Changed -= Refresh;
        }

        private void Refresh()
        {
            var fraction = _energy.Fraction;
            fill.anchorMax = new Vector2(1f, fraction);
            fillImage.color = _energy.IsDepleted ? Low
                : fraction > 0.5f ? Color.Lerp(Half, Full, (fraction - 0.5f) * 2f)
                : Color.Lerp(Low, Half, fraction * 2f);
        }
    }
}
