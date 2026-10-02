using System.Collections;
using Enxada.Core;
using UnityEngine;

namespace Enxada.UI
{
    /// <summary>Painel preto em tela cheia que escurece e clareia. Registra-se como IScreenFader.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ScreenFader : MonoBehaviour, IScreenFader
    {
        [Min(0.01f)] [SerializeField] private float duration = 0.5f;

        private CanvasGroup _group;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            ServiceLocator.Replace<IScreenFader>(this);
        }

        private void OnDestroy() => ServiceLocator.Unregister<IScreenFader>(this);

        public IEnumerator FadeOut() => Fade(1f);
        public IEnumerator FadeIn() => Fade(0f);

        private IEnumerator Fade(float target)
        {
            // Bloqueia cliques enquanto a tela está escura ou escurecendo.
            _group.blocksRaycasts = true;

            var start = _group.alpha;
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                _group.alpha = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }

            _group.alpha = target;
            _group.blocksRaycasts = target > 0.5f;
        }
    }
}
