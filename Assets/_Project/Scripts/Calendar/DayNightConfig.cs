using UnityEngine;

namespace Enxada.Calendar
{
    /// <summary>Cor da luz global ao longo do dia: 0 = 6h (amanhecer), 1 = 2h da madrugada.</summary>
    [CreateAssetMenu(menuName = "Enxada/Config/Day Night Config", fileName = "DayNightConfig")]
    public sealed class DayNightConfig : ScriptableObject
    {
        [SerializeField] private Gradient lightColor;

        public Color Evaluate(float dayProgress) =>
            lightColor == null ? Color.white : lightColor.Evaluate(Mathf.Clamp01(dayProgress));

        public void SetGradient(Gradient gradient) => lightColor = gradient;
    }
}
