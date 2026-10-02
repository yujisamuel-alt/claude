using System;
using System.Collections;
using Enxada.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Enxada.UI
{
    /// <summary>Caixa "Sim / Não". Pausa o mundo enquanto aberta. Registra-se como IConfirmDialog.</summary>
    public sealed class ConfirmDialog : MonoBehaviour, IConfirmDialog
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text questionLabel;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;
        [SerializeField] private TMP_Text yesLabel;
        [SerializeField] private TMP_Text noLabel;
        [SerializeField] private InputActionAsset inputActions;

        private InputAction _cancel;
        private GameplayPause _pause;
        private Action _onYes;
        private Action _onNo;
        private bool _open;

        private void Awake()
        {
            _cancel = inputActions.FindAction("UI/Cancel", true);
            inputActions.FindActionMap("UI", true).Enable();

            yesButton.onClick.AddListener(() => Close(true));
            noButton.onClick.AddListener(() => Close(false));
            panel.SetActive(false);

            ServiceLocator.Replace<IConfirmDialog>(this);
        }

        private void Start()
        {
            _pause = ServiceLocator.Get<GameplayPause>();

            var texts = ServiceLocator.Get<ITextProvider>();
            yesLabel.text = texts.Get("ui.yes");
            noLabel.text = texts.Get("ui.no");
        }

        private void OnDestroy() => ServiceLocator.Unregister<IConfirmDialog>(this);

        private void Update()
        {
            if (_open && _cancel.WasPressedThisFrame())
                Close(false);
        }

        public void Show(string question, Action onYes, Action onNo = null)
        {
            if (_open)
                return;

            _open = true;
            _onYes = onYes;
            _onNo = onNo;
            questionLabel.text = question;
            panel.SetActive(true);
            _pause.Push(this);

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(yesButton.gameObject);
        }

        private void Close(bool accepted)
        {
            if (!_open)
                return;

            _open = false;
            panel.SetActive(false);
            StartCoroutine(ReleasePauseNextFrame());

            (accepted ? _onYes : _onNo)?.Invoke();
        }

        // O botão que confirmou (ex.: A do gamepad) também é "interagir"; soltar a pausa só no
        // próximo frame evita que o mesmo clique reabra o diálogo.
        private IEnumerator ReleasePauseNextFrame()
        {
            yield return null;
            _pause.Pop(this);
        }
    }
}
