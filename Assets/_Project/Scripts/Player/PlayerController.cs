using Enxada.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Enxada.Player
{
    /// <summary>
    /// Move o jogador em 4 (ou 8) direções lendo o Input System e troca o sprite conforme a direção.
    /// A decisão de movimento está em MovementInput (lógica pura e testada); aqui só fica a ponte com a Unity.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("Ordem: Baixo, Cima, Esquerda, Direita (igual ao enum FacingDirection).")]
        [SerializeField] private Sprite[] facingSprites;

        private Rigidbody2D _body;
        private InputAction _move;
        private Vector2 _velocity;
        private GameplayPause _pause;

        public FacingDirection Facing { get; private set; } = FacingDirection.Down;
        public bool IsMoving { get; private set; }

        /// <summary>Diálogos, menus e cutscenes desligam isso para travar o jogador.</summary>
        public bool InputEnabled { get; set; } = true;

        private void Awake()
        {
            if (config == null || inputActions == null)
            {
                Debug.LogError("[PlayerController] Config ou InputActions não atribuídos. " +
                               "Rode Enxada/Setup/Criar Mapa de Teste.", this);
                enabled = false;
                return;
            }

            _body = GetComponent<Rigidbody2D>();
            _body.gravityScale = 0f;
            _body.freezeRotation = true;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _move = inputActions.FindAction("Gameplay/Move", true);
            ApplyFacingSprite();
        }

        private void Start() => ServiceLocator.TryGet(out _pause);

        private void OnEnable()
        {
            if (inputActions != null)
                inputActions.FindActionMap("Gameplay", true).Enable();
        }

        private void OnDisable()
        {
            _velocity = Vector2.zero;
            IsMoving = false;
            if (_body != null)
                _body.linearVelocity = Vector2.zero;
        }

        private void Update()
        {
            var canMove = InputEnabled && (_pause == null || !_pause.IsPaused);
            var raw = canMove ? _move.ReadValue<Vector2>() : Vector2.zero;
            var result = MovementInput.Resolve(raw.x, raw.y, config.AllowDiagonal, config.Deadzone, Facing);

            _velocity = new Vector2(result.X, result.Y) * config.MoveSpeed;
            IsMoving = result.IsMoving;

            if (result.Facing != Facing)
            {
                Facing = result.Facing;
                ApplyFacingSprite();
            }
        }

        private void FixedUpdate()
        {
            _body.linearVelocity = _velocity;
        }

        private void ApplyFacingSprite()
        {
            var index = (int)Facing;
            if (spriteRenderer != null && facingSprites != null && index < facingSprites.Length)
                spriteRenderer.sprite = facingSprites[index];
        }
    }
}
