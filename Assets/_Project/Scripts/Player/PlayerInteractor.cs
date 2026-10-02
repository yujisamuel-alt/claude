using Enxada.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Enxada.Player
{
    /// <summary>
    /// Ao apertar "interagir", procura um IInteractable no tile alvo (o mesmo destacado em amarelo)
    /// e o aciona. Camas, baús, poços e moradores só precisam de IInteractable e de um Collider2D.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private TargetTileSelector selector;
        [SerializeField] private Grid grid;
        [SerializeField] private PlayerController player;
        [SerializeField] private InputActionAsset inputActions;

        private InputAction _interact;
        private GameplayPause _pause;

        private void Awake()
        {
            if (selector == null || grid == null || player == null || inputActions == null)
            {
                Debug.LogError("[PlayerInteractor] Referências não atribuídas. " +
                               "Rode Enxada/Setup/Criar Mapa de Teste.", this);
                enabled = false;
                return;
            }

            _interact = inputActions.FindAction("Gameplay/Interact", true);
        }

        private void Start() => ServiceLocator.TryGet(out _pause);

        private void Update()
        {
            if (_pause != null && _pause.IsPaused)
                return;

            if (_interact.WasPressedThisFrame())
                TryInteract();
        }

        private void TryInteract()
        {
            var center = (Vector2)grid.GetCellCenterWorld(selector.TargetCell);
            var hit = Physics2D.OverlapPoint(center);
            if (hit == null)
                return;

            var interactable = hit.GetComponentInParent<IInteractable>();
            interactable?.Interact(player.gameObject);
        }
    }
}
