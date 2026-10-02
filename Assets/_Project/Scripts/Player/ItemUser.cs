using System.Collections;
using Enxada.Core;
using Enxada.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Enxada.Player
{
    /// <summary>
    /// Usa o item selecionado na barra rápida no tile alvo.
    /// Ferramenta: primeiro procura um objeto que reaja (pedra, galho, mato, poço), depois tenta o chão
    /// (arar, regar, colher); gasta energia se funcionou. Semente: planta na terra arada e gasta a semente.
    /// </summary>
    public sealed class ItemUser : MonoBehaviour
    {
        private const float SwingDegrees = 70f;
        private const float RejectedLockSeconds = 0.2f;
        private const float PlantLockSeconds = 0.25f;

        [SerializeField] private ToolConfig config;
        [SerializeField] private TargetTileSelector selector;
        [SerializeField] private PlayerController player;
        [SerializeField] private Grid grid;
        [SerializeField] private InputActionAsset inputActions;

        [Tooltip("Sprite que balança no tile alvo durante o golpe.")]
        [SerializeField] private SpriteRenderer swing;

        private readonly Collider2D[] _hits = new Collider2D[8];
        private ContactFilter2D _filter;
        private InputAction _use;
        private InventoryModel _inventory;
        private ItemDatabase _database;
        private EnergyModel _energy;
        private GameplayPause _pause;
        private ITextProvider _texts;
        private float _busyUntil;
        private Coroutine _swingRoutine;

        private void Awake()
        {
            if (config == null || selector == null || player == null || grid == null || inputActions == null
                || swing == null)
            {
                Debug.LogError("[ItemUser] Referências não atribuídas. Rode Enxada/Setup/Criar Mapa de Teste.", this);
                enabled = false;
                return;
            }

            _use = inputActions.FindAction("Gameplay/UseTool", true);
            _filter = new ContactFilter2D { useTriggers = true }; // sem filtro de camada; o mato é trigger
            swing.gameObject.SetActive(false);
        }

        private void Start()
        {
            _inventory = ServiceLocator.Get<InventoryModel>();
            _database = ServiceLocator.Get<ItemDatabase>();
            _energy = ServiceLocator.Get<EnergyModel>();
            _texts = ServiceLocator.Get<ITextProvider>();
            ServiceLocator.TryGet(out _pause);
        }

        private void Update()
        {
            if ((_pause != null && _pause.IsPaused) || Time.time < _busyUntil)
                return;

            if (_use.WasPressedThisFrame())
                UseSelectedItem();
        }

        private void UseSelectedItem()
        {
            var stack = _inventory.SelectedStack;
            if (stack.IsEmpty || !_database.TryGet(stack.ItemId, out var item))
                return;

            if (item.Category == ItemCategory.Seed)
                UseSeed(item);
            else if (item.Category == ItemCategory.Tool && item.ToolType != ToolType.None)
                UseTool(item);
        }

        // ------------------------------------------------------------------ sementes

        private void UseSeed(ItemDefinition seed)
        {
            if (!ServiceLocator.TryGet<ISeedPlanter>(out var planter))
                return;

            var cell = selector.TargetCell;
            var outcome = planter.TryPlant(seed.Id, new CellPosition(cell.x, cell.y));

            switch (outcome.Kind)
            {
                case ToolUseKind.Rejected:
                    Lock(RejectedLockSeconds);
                    ShowMessage(outcome.MessageKey);
                    break;

                case ToolUseKind.Used:
                case ToolUseKind.UsedFree:
                    _inventory.Take(_inventory.SelectedHotbarIndex, 1); // plantar não gasta energia, só a semente
                    Lock(PlantLockSeconds);
                    break;
            }
        }

        // ------------------------------------------------------------------ ferramentas

        private void UseTool(ItemDefinition item)
        {
            if (!config.TryGet(item.ToolType, out var entry))
                return;

            var cell = selector.TargetCell;
            var outcome = Resolve(item.ToolType, item.ToolTier, cell);

            switch (outcome.Kind)
            {
                case ToolUseKind.None:
                    return;

                case ToolUseKind.Rejected:
                    Lock(RejectedLockSeconds);
                    ShowMessage(outcome.MessageKey);
                    return;

                case ToolUseKind.Used:
                    _energy.Spend(ToolRules.EnergyCost(entry.energyCost, item.ToolTier,
                        config.EnergyReductionPerTier, config.MinimumEnergyCost));
                    break;
            }

            Lock(entry.useDuration);
            PlaySwing(item.Icon, cell, entry.useDuration);
        }

        private ToolUseOutcome Resolve(ToolType tool, int tier, Vector3Int cell)
        {
            var center = (Vector2)grid.GetCellCenterWorld(cell);

            var count = Physics2D.OverlapPoint(center, _filter, _hits);
            for (var i = 0; i < count; i++)
            {
                if (_hits[i].GetComponentInParent<IPlayerAnchor>() != null)
                    continue;

                var target = _hits[i].GetComponentInParent<IToolTarget>();
                if (target == null)
                    continue;

                var outcome = target.OnToolUsed(tool, tier);
                if (outcome.WasHandled)
                    return outcome;
            }

            return ServiceLocator.TryGet<ITileToolHandler>(out var handler)
                ? handler.TryUse(tool, tier, new CellPosition(cell.x, cell.y))
                : ToolUseOutcome.None;
        }

        // ------------------------------------------------------------------ feedback

        private void ShowMessage(string key)
        {
            if (ServiceLocator.TryGet<IToastService>(out var toast))
                toast.Show(_texts.Get(key));
        }

        private void Lock(float seconds)
        {
            _busyUntil = Time.time + seconds;
            player.LockMovementFor(seconds);
        }

        private void PlaySwing(Sprite icon, Vector3Int cell, float duration)
        {
            if (_swingRoutine != null)
                StopCoroutine(_swingRoutine);
            _swingRoutine = StartCoroutine(Swing(icon, cell, duration));
        }

        private IEnumerator Swing(Sprite icon, Vector3Int cell, float duration)
        {
            swing.sprite = icon;
            swing.transform.position = grid.GetCellCenterWorld(cell);
            swing.gameObject.SetActive(true);

            for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                var angle = Mathf.Lerp(SwingDegrees, -SwingDegrees, elapsed / duration);
                swing.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }

            swing.gameObject.SetActive(false);
            _swingRoutine = null;
        }
    }
}
