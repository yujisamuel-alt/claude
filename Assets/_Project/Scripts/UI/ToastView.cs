using System.Collections;
using TMPro;
using UnityEngine;

namespace Enxada.UI
{
    /// <summary>Aviso curto que aparece, fica alguns segundos e some (ex.: "Tá ficando tarde...").</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ToastView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [Min(0.5f)] [SerializeField] private float visibleSeconds = 3.5f;
        [Min(0.05f)] [SerializeField] private float fadeSeconds = 0.4f;

        private CanvasGroup _group;
        private Coroutine _running;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
        }

        public void Show(string message)
        {
            label.text = message;
            if (_running != null)
                StopCoroutine(_running);
            _running = StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            _group.alpha = 1f;
            yield return new WaitForSecondsRealtime(visibleSeconds);

            for (var elapsed = 0f; elapsed < fadeSeconds; elapsed += Time.unscaledDeltaTime)
            {
                _group.alpha = 1f - elapsed / fadeSeconds;
                yield return null;
            }

            _group.alpha = 0f;
            _running = null;
        }
    }
}
