using Enxada.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Enxada.EditorTools
{
    /// <summary>Monta a barra rápida (sempre visível) e a tela de inventário (E / Tab / Y) no Canvas.</summary>
    public static class InventoryUiBuilder
    {
        private const int Columns = 12;
        private const int Rows = 3; // 1 linha de barra rápida + 2 de mochila
        private const float SlotSize = 40f;
        private const float Gap = 4f;
        private const float RowGapAfterHotbar = 6f;

        private static readonly string[] HotkeyLabels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" };
        private static readonly Color SlotColor = new Color(0.6f, 0.45f, 0.3f, 1f);
        private static readonly Color PanelColor = new Color(0.15f, 0.11f, 0.07f, 0.96f);
        private static readonly Color FrameColor = new Color(1f, 0.9f, 0.3f, 1f);

        public static void Build(Transform root, InputActionAsset inputActions)
        {
            BuildHotbar(root);
            BuildInventoryScreen(root, inputActions);
        }

        // ------------------------------------------------------------------ barra rápida

        private static void BuildHotbar(Transform root)
        {
            var width = Columns * (SlotSize + Gap) + Gap;
            var bar = HudBuilder.CreatePanel(root, "Hotbar", new Vector2(0.5f, 0f), new Vector2(0f, 10f),
                new Vector2(width, SlotSize + Gap * 2f));

            var visuals = new SlotVisual[Columns];
            for (var i = 0; i < Columns; i++)
            {
                var slot = CreateSlot(bar, "Slot" + (i + 1), withBackground: true, interactive: false);
                var rect = slot.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(Gap + i * (SlotSize + Gap), 0f);

                var hotkey = HudBuilder.CreateText(rect, "Hotkey", 11f, TextAlignmentOptions.TopLeft, Vector2.zero,
                    Vector2.one);
                hotkey.text = HotkeyLabels[i];
                hotkey.color = new Color(1f, 1f, 1f, 0.75f);
                hotkey.rectTransform.offsetMin = new Vector2(3f, 0f);
                hotkey.rectTransform.offsetMax = new Vector2(0f, -1f);

                visuals[i] = slot.GetComponent<SlotVisual>();
            }

            var view = bar.gameObject.AddComponent<HotbarView>();
            HudBuilder.Wire(view, so => AssignArray(so.FindProperty("slots"), visuals));
        }

        // ------------------------------------------------------------------ tela de inventário

        private static void BuildInventoryScreen(Transform canvasRoot, InputActionAsset inputActions)
        {
            var holder = new GameObject("InventoryScreen", typeof(RectTransform));
            holder.transform.SetParent(canvasRoot, false);
            HudBuilder.Stretch(holder.GetComponent<RectTransform>());
            var screen = holder.AddComponent<InventoryScreen>();

            // Tudo que aparece/some junto fica sob "Root".
            var rootGo = new GameObject("Root", typeof(RectTransform));
            rootGo.transform.SetParent(holder.transform, false);
            HudBuilder.Stretch(rootGo.GetComponent<RectTransform>());

            // Fundo escuro: clicar nele com item na mão descarta o item no chão.
            var dropZone = HudBuilder.CreatePanel(rootGo.transform, "DropZone", new Vector2(0.5f, 0.5f), Vector2.zero,
                Vector2.zero);
            HudBuilder.Stretch(dropZone);
            var dropImage = dropZone.GetComponent<Image>();
            dropImage.color = new Color(0f, 0f, 0f, 0.5f);
            dropImage.raycastTarget = true;
            var zone = dropZone.gameObject.AddComponent<InventoryDropZone>();
            HudBuilder.Wire(zone, so => so.FindProperty("screen").objectReferenceValue = screen);

            const float panelWidth = Columns * (SlotSize + Gap) + 32f - Gap;
            const float panelHeight = 330f;
            var panel = HudBuilder.CreatePanel(rootGo.transform, "Panel", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(panelWidth, panelHeight));
            panel.pivot = new Vector2(0.5f, 0.5f);
            var panelImage = panel.GetComponent<Image>();
            panelImage.color = PanelColor;
            panelImage.raycastTarget = true; // bloqueia o clique de "descartar" do fundo

            var title = TopLeftText(panel, "Title", 22f, 16f, -8f, panelWidth - 32f, 28f, TextAlignmentOptions.Left);

            var slotViews = new InventorySlotView[Columns * Rows];
            var slotVisuals = new SlotVisual[Columns * Rows];
            for (var i = 0; i < slotViews.Length; i++)
            {
                var row = i / Columns;
                var column = i % Columns;
                var slot = CreateSlot(panel, "Slot" + i, withBackground: true, interactive: true);
                var rect = slot.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                var y = -44f - row * (SlotSize + Gap) - (row > 0 ? RowGapAfterHotbar : 0f);
                rect.anchoredPosition = new Vector2(16f + column * (SlotSize + Gap), y);

                slotViews[i] = slot.GetComponent<InventorySlotView>();
                slotVisuals[i] = slot.GetComponent<SlotVisual>();
            }

            LinkNavigation(slotViews);

            var itemName = TopLeftText(panel, "ItemName", 20f, 16f, -196f, panelWidth - 32f, 26f, TextAlignmentOptions.Left);
            var description = TopLeftText(panel, "ItemDescription", 15f, 16f, -224f, panelWidth - 32f, 44f,
                TextAlignmentOptions.TopLeft);
            var price = TopLeftText(panel, "ItemPrice", 15f, 16f, -270f, panelWidth - 32f, 22f, TextAlignmentOptions.Left);
            price.color = new Color(1f, 0.85f, 0.35f, 1f);
            var hint = TopLeftText(panel, "Hint", 13f, 16f, -300f, panelWidth - 32f, 22f, TextAlignmentOptions.Left);
            hint.color = new Color(1f, 1f, 1f, 0.6f);

            // Item "na mão": segue o mouse; por último para ficar por cima de tudo.
            var cursor = CreateSlot(rootGo.transform, "HeldItem", withBackground: false, interactive: false);
            var cursorRect = cursor.GetComponent<RectTransform>();
            cursorRect.anchorMin = cursorRect.anchorMax = cursorRect.pivot = new Vector2(0.5f, 0.5f);

            HudBuilder.Wire(screen, so =>
            {
                so.FindProperty("root").objectReferenceValue = rootGo;
                AssignArray(so.FindProperty("slotViews"), slotViews);
                AssignArray(so.FindProperty("slotVisuals"), slotVisuals);
                so.FindProperty("cursorRect").objectReferenceValue = cursorRect;
                so.FindProperty("cursorVisual").objectReferenceValue = cursor.GetComponent<SlotVisual>();
                so.FindProperty("titleLabel").objectReferenceValue = title;
                so.FindProperty("hintLabel").objectReferenceValue = hint;
                so.FindProperty("nameLabel").objectReferenceValue = itemName;
                so.FindProperty("descriptionLabel").objectReferenceValue = description;
                so.FindProperty("priceLabel").objectReferenceValue = price;
                so.FindProperty("inputActions").objectReferenceValue = inputActions;
            });
        }

        // ------------------------------------------------------------------ slot

        private static GameObject CreateSlot(Transform parent, string name, bool withBackground, bool interactive)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(SlotVisual));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(SlotSize, SlotSize);

            var background = go.GetComponent<Image>();
            background.color = SlotColor;
            background.enabled = withBackground;
            background.raycastTarget = interactive;

            var icon = CreateImage(rect, "Icon", new Vector2(4f, 4f), new Vector2(-4f, -4f), Color.white);
            icon.enabled = false;

            var mark = CreateImage(rect, "QualityMark", new Vector2(-12f, -12f), new Vector2(-2f, -2f), Color.white);
            var markRect = mark.rectTransform;
            markRect.anchorMin = new Vector2(1f, 1f);
            markRect.anchorMax = new Vector2(1f, 1f);
            markRect.pivot = new Vector2(1f, 1f);
            markRect.sizeDelta = new Vector2(8f, 8f);
            markRect.anchoredPosition = new Vector2(-3f, -3f);
            mark.enabled = false;

            var quantity = HudBuilder.CreateText(rect, "Quantity", 14f, TextAlignmentOptions.BottomRight, Vector2.zero,
                Vector2.one);
            quantity.rectTransform.offsetMin = new Vector2(0f, 1f);
            quantity.rectTransform.offsetMax = new Vector2(-3f, 0f);

            var frame = CreateFrame(rect);

            HudBuilder.Wire(go.GetComponent<SlotVisual>(), so =>
            {
                so.FindProperty("icon").objectReferenceValue = icon;
                so.FindProperty("quantityLabel").objectReferenceValue = quantity;
                so.FindProperty("qualityMark").objectReferenceValue = mark;
                so.FindProperty("selectionFrame").objectReferenceValue = frame;
            });

            if (interactive)
            {
                var view = go.AddComponent<InventorySlotView>();
                view.targetGraphic = background;
                view.transition = Selectable.Transition.ColorTint;
                var colors = view.colors;
                colors.normalColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                colors.highlightedColor = Color.white;
                colors.selectedColor = Color.white;
                colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
                view.colors = colors;
            }

            return go;
        }

        // Moldura de seleção: 4 faixas finas (um contorno sem precisar de sprite).
        private static GameObject CreateFrame(RectTransform slot)
        {
            var frame = new GameObject("SelectionFrame", typeof(RectTransform));
            frame.transform.SetParent(slot, false);
            HudBuilder.Stretch(frame.GetComponent<RectTransform>());

            const float t = 2f;
            Edge(frame.transform, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -t), Vector2.zero);
            Edge(frame.transform, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, t));
            Edge(frame.transform, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(t, 0f));
            Edge(frame.transform, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-t, 0f), Vector2.zero);

            frame.SetActive(false);
            return frame;
        }

        private static void Edge(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var image = CreateImage((RectTransform)parent, name, Vector2.zero, Vector2.zero, FrameColor);
            var rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        // ------------------------------------------------------------------ helpers

        private static Image CreateImage(RectTransform parent, string name, Vector2 offsetMin, Vector2 offsetMax,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI TopLeftText(Transform parent, string name, float size, float x, float y,
            float width, float height, TextAlignmentOptions alignment)
        {
            var text = HudBuilder.CreateText(parent, name, size, alignment, Vector2.zero, Vector2.one);
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            return text;
        }

        // Navegação explícita em grade: o gamepad anda em linhas e colunas sem pular slots.
        private static void LinkNavigation(InventorySlotView[] views)
        {
            for (var i = 0; i < views.Length; i++)
            {
                var row = i / Columns;
                var column = i % Columns;
                var navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = column > 0 ? views[i - 1] : null,
                    selectOnRight = column < Columns - 1 ? views[i + 1] : null,
                    selectOnUp = row > 0 ? views[i - Columns] : null,
                    selectOnDown = row < Rows - 1 ? views[i + Columns] : null
                };
                views[i].navigation = navigation;
            }
        }

        private static void AssignArray<T>(UnityEditor.SerializedProperty property, T[] items) where T : Object
        {
            property.arraySize = items.Length;
            for (var i = 0; i < items.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }
}
