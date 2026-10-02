using System.Collections;
using Enxada.Core;
using UnityEngine;

namespace Enxada.Calendar
{
    /// <summary>
    /// Conduz a virada do dia: escurece a tela, encerra o dia no relógio, leva o jogador para a cama
    /// e clareia de novo. Serve tanto para dormir quanto para desmaiar às 2h.
    /// </summary>
    public sealed class DayTransitionController : MonoBehaviour
    {
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Transform wakePoint;

        [Tooltip("Disparado quando a Primavera termina (tela de fim da demo, na Etapa 6).")]
        [SerializeField] private VoidEventChannel demoEnded;

        [Tooltip("Pedido de desmaio por exaustão (energia abaixo do limite), vindo do módulo Player.")]
        [SerializeField] private VoidEventChannel passOutRequested;

        [Tooltip("Tempo com a tela escura entre apagar e acordar.")]
        [Min(0f)] [SerializeField] private float blackHoldSeconds = 0.6f;

        private GameClock _clock;
        private GameplayPause _pause;
        private bool _busy;

        public bool IsBusy => _busy;

        private void Start()
        {
            _clock = ServiceLocator.Get<GameClock>();
            _pause = ServiceLocator.Get<GameplayPause>();
            _clock.PassOutReached += OnPassOutReached;
            if (passOutRequested != null)
                passOutRequested.Subscribe(OnPassOutReached);
        }

        private void OnDestroy()
        {
            if (_clock != null)
                _clock.PassOutReached -= OnPassOutReached;
            if (passOutRequested != null)
                passOutRequested.Unsubscribe(OnPassOutReached);
        }

        /// <summary>Chamado pela cama depois que o jogador confirma.</summary>
        public void RequestSleep() => Begin(DayEndReason.Slept);

        private void OnPassOutReached() => Begin(DayEndReason.PassedOut);

        private void Begin(DayEndReason reason)
        {
            if (_busy)
                return;

            StartCoroutine(Sequence(reason));
        }

        private IEnumerator Sequence(DayEndReason reason)
        {
            _busy = true;
            _pause.Push(this);

            if (ServiceLocator.TryGet<IScreenFader>(out var fader))
                yield return fader.FadeOut();

            var info = _clock.EndDay(reason);
            MovePlayerToWakePoint();

            if (info.SeasonChanged && info.EndedDate.Season == Season.Spring && demoEnded != null)
                demoEnded.Raise();

            yield return new WaitForSecondsRealtime(blackHoldSeconds);

            if (fader != null)
                yield return fader.FadeIn();

            _pause.Pop(this);
            _busy = false;
        }

        private void MovePlayerToWakePoint()
        {
            if (playerTransform == null || wakePoint == null)
                return;

            playerTransform.position = wakePoint.position;
            if (playerTransform.TryGetComponent<Rigidbody2D>(out var body))
                body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
        }
    }
}
