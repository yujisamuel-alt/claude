using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Enxada.Player
{
    /// <summary>
    /// Calcula e destaca o tile alvo: o tile à frente do jogador (teclado/gamepad)
    /// ou o tile sob o mouse, limitado ao alcance do config.
    /// Sistemas futuros (ferramentas, plantio) leem TargetCell e escutam TargetChanged.
    /// </summary>
    public sealed class TargetTileSelector : MonoBehaviour
    {
        // Movimento do mouse (em pixels) a partir do qual passamos a mirar com ele.
        private const float MouseMoveThresholdSqr = 4f;

        [SerializeField] private PlayerConfig config;
        [SerializeField] private PlayerController player;
        [SerializeField] private Grid grid;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private Transform highlight;

        private InputAction _point;
        private Camera _camera;
        private Vector2 _lastPointer;
        private bool _useMouse;

        public Vector3Int TargetCell { get; private set; }
        public event Action<Vector3Int> TargetChanged;

        private void Awake()
        {
            if (config == null || player == null || grid == null || inputActions == null)
            {
                Debug.LogError("[TargetTileSelector] Referências não atribuídas. " +
                               "Rode Enxada/Setup/Criar Mapa de Teste.", this);
                enabled = false;
                return;
            }

            _point = inputActions.FindAction("Gameplay/Point", true);
        }

        private void OnEnable()
        {
            if (inputActions != null)
                inputActions.FindActionMap("Gameplay", true).Enable();
        }

        // LateUpdate: depois do PlayerController.Update, para usar a direção já atualizada.
        private void LateUpdate()
        {
            var pointer = _point.ReadValue<Vector2>();
            if ((pointer - _lastPointer).sqrMagnitude > MouseMoveThresholdSqr)
            {
                _useMouse = true;
                _lastPointer = pointer;
            }

            if (player.IsMoving)
                _useMouse = false;

            var playerCell = grid.WorldToCell(player.transform.position);
            var mouseCell = _useMouse ? MouseToCell(pointer) : playerCell;

            var target = TileTargeting.ResolveTarget(
                new CellPosition(playerCell.x, playerCell.y), player.Facing, _useMouse,
                new CellPosition(mouseCell.x, mouseCell.y), config.MouseTargetRange);

            var targetCell = new Vector3Int(target.X, target.Y, 0);
            if (targetCell != TargetCell)
            {
                TargetCell = targetCell;
                TargetChanged?.Invoke(targetCell);
            }

            if (highlight != null)
                highlight.position = grid.GetCellCenterWorld(targetCell);
        }

        private Vector3Int MouseToCell(Vector2 screenPosition)
        {
            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null)
                return grid.WorldToCell(player.transform.position);

            var depth = Mathf.Abs(_camera.transform.position.z);
            var world = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
            return grid.WorldToCell(world);
        }
    }
}
