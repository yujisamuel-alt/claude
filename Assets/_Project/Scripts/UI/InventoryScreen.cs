using Enxada.Core;
using Enxada.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Enxada.UI
{
    /// <summary>
    /// Tela do inventário (E, Tab ou Y). Mostra a barra rápida e a mochila, deixa o jogador pegar, soltar,
    /// trocar, dividir e descartar pilhas, e mostra nome, descrição e preço do item sob o cursor.
    /// Enquanto aberta, o mundo fica pausado.
    /// </summary>
    public sealed class InventoryScreen : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private InventorySlotView[] slotViews;
        [SerializeField] private SlotVisual[] slotVisuals;
        [SerializeField] private RectTransform cursorRect;
        [SerializeField] private SlotVisual cursorVisual;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text hintLabel;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private TMP_Text priceLabel;
        [SerializeField] private InputActionAsset inputActions;

        private InventoryModel _inventory;
        private ItemDatabase _database;
        private ITextProvider _texts;
        private GameplayPause _pause;
        private InputAction _toggle;
        private InputAction _cancel;
        private InputAction _point;
        private ItemStack _held;
        private bool _open;
        private bool _dragTookItem;

        private void Awake()
        {
            _toggle = inputActions.FindAction("Gameplay/OpenInventory", true);
            _cancel = inputActions.FindAction("UI/Cancel", true);
            _point = inputActions.FindAction("Gameplay/Point", true);
            inputActions.FindActionMap("UI", true).Enable();

            for (var i = 0; i < slotViews.Length; i++)
                slotViews[i].Bind(this, i);

            root.SetActive(false);
        }

        private void Start()
        {
            _inventory = ServiceLocator.Get<InventoryModel>();
            _database = ServiceLocator.Get<ItemDatabase>();
            _texts = ServiceLocator.Get<ITextProvider>();
            _pause = ServiceLocator.Get<GameplayPause>();

            titleLabel.text = _texts.Get("inventory.title");
            hintLabel.text = _texts.Get("inventory.hint");
            _inventory.Changed += OnInventoryChanged;
        }

        private void OnDestroy()
        {
            if (_inventory != null)
                _inventory.Changed -= OnInventoryChanged;
        }

        private void Update()
        {
            if (!_open)
            {
                if (_toggle.WasPressedThisFrame() && !_pause.IsPaused)
                    Open();
                return;
            }

            if (_toggle.WasPressedThisFrame() || _cancel.WasPressedThisFrame())
            {
                Close();
                return;
            }

            if (!_held.IsEmpty)
                FollowPointer();
        }

        // ------------------------------------------------------------------ abrir e fechar

        private void Open()
        {
            _open = true;
            root.SetActive(true);
            _pause.Push(this);
            Refresh();
            ClearInfo();

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(slotViews[_inventory.SelectedHotbarIndex].gameObject);
        }

        private void Close()
        {
            ReturnHeldItem();
            _open = false;
            root.SetActive(false);
            _pause.Pop(this);
        }

        // Nada fica "perdido na mão": ao fechar, guarda no inventário e o que não couber vai ao chão.
        private void ReturnHeldItem()
        {
            if (_held.IsEmpty)
                return;

            var leftover = _inventory.TryAdd(_held);
            _held = ItemStack.Empty;
            DropToGround(leftover);
            RefreshCursor();
        }

        // ------------------------------------------------------------------ ações dos slots

        /// <summary>Clique esquerdo ou botão de confirmar: pega a pilha ou solta/troca a que está na mão.</summary>
        public void PrimaryAction(int slot)
        {
            _held = _held.IsEmpty ? _inventory.Take(slot) : _inventory.Place(slot, _held);
            RefreshCursor();
            ShowInfo(slot);
        }

        /// <summary>Clique direito: com a mão vazia pega metade da pilha; com item na mão, solta uma unidade.</summary>
        public void SecondaryAction(int slot)
        {
            if (_held.IsEmpty)
            {
                var quantity = _inventory[slot].Quantity;
                _held = _inventory.Take(slot, (quantity + 1) / 2);
            }
            else
            {
                _held = _inventory.PlaceOne(slot, _held);
            }

            RefreshCursor();
            ShowInfo(slot);
        }

        public void BeginDrag(int slot)
        {
            // Só "possui" o arrasto se ele mesmo pegou a pilha; se a mão já estava ocupada (clique), é só um clique longo.
            _dragTookItem = _held.IsEmpty;
            if (_dragTookItem)
                _held = _inventory.Take(slot);
            RefreshCursor();
        }

        public void DropOnSlot(int slot)
        {
            if (_held.IsEmpty)
                return;

            _held = _inventory.Place(slot, _held);
            RefreshCursor();
        }

        /// <summary>Soltou o arrasto fora de qualquer slot: sobre o fundo escuro descarta, sobre o painel devolve.</summary>
        public void EndDrag(int originSlot)
        {
            if (!_dragTookItem)
                return;

            _dragTookItem = false;
            if (_held.IsEmpty)
                return;

            // Soltou em um slot, mas voltou um item trocado: ele vai para o slot de onde saiu o arrasto.
            _held = _inventory.Place(originSlot, _held);
            RefreshCursor();
        }

        /// <summary>Clique ou soltar no fundo escuro fora do painel: descarta no chão.</summary>
        public void DiscardHeld(bool onlyOne)
        {
            if (_held.IsEmpty)
                return;

            var amount = onlyOne ? 1 : _held.Quantity;
            DropToGround(_held.WithQuantity(amount));
            _held = _held.WithQuantity(_held.Quantity - amount);
            RefreshCursor();
        }

        private void DropToGround(ItemStack stack)
        {
            if (stack.IsEmpty)
                return;

            if (!ServiceLocator.TryGet<WorldItemSpawner>(out var spawner)
                || !ServiceLocator.TryGet<IPlayerAnchor>(out var player))
            {
                // Sem cena para receber o item: devolve ao inventário para não perder nada.
                _inventory.TryAdd(stack);
                return;
            }

            var facing = player.FacingVector;
            spawner.Drop(stack, (Vector2)player.Transform.position + facing * 0.6f, facing);
        }

        // ------------------------------------------------------------------ visual

        public void ShowInfo(int slot)
        {
            var stack = _inventory[slot];
            if (stack.IsEmpty || !_database.TryGet(stack.ItemId, out var definition))
            {
                ClearInfo();
                return;
            }

            var itemName = _texts.Get(ItemKeys.Name(stack.ItemId));
            if (stack.Quality != ItemQuality.Normal)
                itemName += " (" + _texts.Get(ItemKeys.Quality(stack.Quality)) + ")";

            nameLabel.text = itemName;
            descriptionLabel.text = _texts.Get(ItemKeys.Description(stack.ItemId));
            priceLabel.text = definition.SellPrice > 0
                ? _texts.Format("tooltip.sell", definition.SellPrice, _texts.Get("currency.name"))
                : string.Empty;
        }

        public void ClearInfo()
        {
            nameLabel.text = string.Empty;
            descriptionLabel.text = string.Empty;
            priceLabel.text = string.Empty;
        }

        private void OnInventoryChanged()
        {
            if (_open)
                Refresh();
        }

        private void Refresh()
        {
            for (var i = 0; i < slotVisuals.Length && i < _inventory.Capacity; i++)
            {
                slotVisuals[i].SetStack(_inventory[i], _database);
                slotVisuals[i].SetSelected(i == _inventory.SelectedHotbarIndex);
            }

            RefreshCursor();
        }

        private void RefreshCursor()
        {
            cursorRect.gameObject.SetActive(!_held.IsEmpty);
            if (_held.IsEmpty)
                return;

            cursorVisual.SetStack(_held, _database);
            FollowPointer();
        }

        private void FollowPointer()
        {
            var parent = (RectTransform)cursorRect.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, _point.ReadValue<Vector2>(), null,
                    out var local))
                cursorRect.anchoredPosition = local;
        }
    }
}
