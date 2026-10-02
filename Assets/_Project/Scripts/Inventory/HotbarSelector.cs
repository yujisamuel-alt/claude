using Enxada.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Enxada.Inventory
{
    /// <summary>Troca o slot selecionado da barra rápida: teclas 1–0, - e =, roda do mouse e ombros do gamepad.</summary>
    public sealed class HotbarSelector : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;

        private InputAction[] _slotActions;
        private InputAction _scroll;
        private InventoryModel _inventory;
        private GameplayPause _pause;
        private float _lastShoulderSign;

        private void Awake()
        {
            if (inputActions == null)
            {
                Debug.LogError("[HotbarSelector] InputActions não atribuído.", this);
                enabled = false;
                return;
            }

            _slotActions = new InputAction[InventoryModel.DefaultHotbarSize];
            for (var i = 0; i < _slotActions.Length; i++)
                _slotActions[i] = inputActions.FindAction($"Gameplay/HotbarSlot{i + 1}", true);
            _scroll = inputActions.FindAction("Gameplay/HotbarScroll", true);
        }

        private void OnEnable()
        {
            if (inputActions != null)
                inputActions.FindActionMap("Gameplay", true).Enable();
        }

        private void Start()
        {
            ServiceLocator.TryGet(out _inventory);
            ServiceLocator.TryGet(out _pause);
        }

        private void Update()
        {
            if (_inventory == null || (_pause != null && _pause.IsPaused))
                return;

            for (var i = 0; i < _slotActions.Length && i < _inventory.HotbarSize; i++)
            {
                if (_slotActions[i].WasPressedThisFrame())
                    _inventory.SelectHotbar(i);
            }

            ReadScroll();
        }

        private void ReadScroll()
        {
            var value = _scroll.ReadValue<float>();

            // Roda do mouse: um valor grande por "clique" (rolar para cima = slot anterior).
            if (Mathf.Abs(value) > 1f)
            {
                _inventory.ScrollHotbar(value > 0f ? -1 : 1);
                return;
            }

            // Ombros do gamepad: valor -1/0/+1 enquanto segurado; só conta ao apertar.
            var sign = Mathf.Abs(value) > 0.5f ? Mathf.Sign(value) : 0f;
            if (sign != 0f && !Mathf.Approximately(sign, _lastShoulderSign))
                _inventory.ScrollHotbar(sign > 0f ? 1 : -1);
            _lastShoulderSign = sign;
        }
    }
}
