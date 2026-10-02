using Enxada.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Enxada.EditorTools
{
    /// <summary>
    /// Monta o Canvas do jogo por código: HUD (hora e data), aviso, caixa Sim/Não e fade de tela,
    /// mais o EventSystem com o módulo do novo Input System.
    /// </summary>
    public static class HudBuilder
    {
        private static readonly Vector2 ReferenceResolution = new Vector2(960f, 540f);
        private static readonly Color PanelColor = new Color(0.08f, 0.06f, 0.04f, 0.65f);
        private static readonly Color ButtonColor = new Color(0.35f, 0.5f, 0.25f, 1f);

        public static void Build(InputActionAsset inputActions)
        {
            BuildEventSystem();

            var canvasGo = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasGo.transform;

            // HUD (canto superior direito)
            var hudPanel = CreatePanel(root, "HudPanel", new Vector2(1f, 1f), new Vector2(-12f, -12f),
                new Vector2(210f, 64f));
            var timeLabel = CreateText(hudPanel, "TimeLabel", 30f, TextAlignmentOptions.Center,
                new Vector2(0f, 0.45f), new Vector2(1f, 1f));
            var dateLabel = CreateText(hudPanel, "DateLabel", 17f, TextAlignmentOptions.Center,
                new Vector2(0f, 0f), new Vector2(1f, 0.5f));

            // Aviso (parte de baixo, centralizado)
            var toastPanel = CreatePanel(root, "Toast", new Vector2(0.5f, 0f), new Vector2(0f, 70f),
                new Vector2(620f, 44f));
            toastPanel.gameObject.AddComponent<CanvasGroup>();
            var toastLabel = CreateText(toastPanel, "Label", 20f, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one);
            var toast = toastPanel.gameObject.AddComponent<ToastView>();
            Wire(toast, so => so.FindProperty("label").objectReferenceValue = toastLabel);

            BuildEnergyBar(root);
            BuildToolStatus(root);

            var hud = canvasGo.AddComponent<HudController>();
            Wire(hud, so =>
            {
                so.FindProperty("timeLabel").objectReferenceValue = timeLabel;
                so.FindProperty("dateLabel").objectReferenceValue = dateLabel;
                so.FindProperty("toast").objectReferenceValue = toast;
            });

            InventoryUiBuilder.Build(root, inputActions);
            BuildConfirmDialog(root, inputActions);
            BuildFader(root);
        }

        private static void BuildEnergyBar(Transform root)
        {
            // Canto inferior direito. O "Fill" cresce de baixo para cima (a view mexe no anchorMax.y).
            var bar = CreatePanel(root, "EnergyBar", new Vector2(1f, 0f), new Vector2(-14f, 14f), new Vector2(26f, 130f));

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(bar, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            var fillImage = fillGo.GetComponent<Image>();
            fillImage.raycastTarget = false;

            var view = bar.gameObject.AddComponent<EnergyBarView>();
            Wire(view, so =>
            {
                so.FindProperty("fill").objectReferenceValue = fillRect;
                so.FindProperty("fillImage").objectReferenceValue = fillImage;
            });
        }

        private static void BuildToolStatus(Transform root)
        {
            // Logo acima da barra rápida.
            var label = CreateText(root, "ToolStatus", 18f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 68f);
            rect.sizeDelta = new Vector2(300f, 26f);

            var view = label.gameObject.AddComponent<ToolStatusView>();
            Wire(view, so => so.FindProperty("label").objectReferenceValue = label);
        }

        private static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        private static void BuildConfirmDialog(Transform root, InputActionAsset inputActions)
        {
            var holder = new GameObject("ConfirmDialog", typeof(RectTransform));
            holder.transform.SetParent(root, false);
            Stretch(holder.GetComponent<RectTransform>());

            // Fundo escurecido que cobre a tela e bloqueia cliques no resto.
            var dim = CreatePanel(holder.transform, "Panel", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(dim);
            dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            dim.GetComponent<Image>().raycastTarget = true;

            var box = CreatePanel(dim, "Box", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 170f));
            box.GetComponent<Image>().color = new Color(0.15f, 0.11f, 0.07f, 0.95f);
            var question = CreateText(box, "Question", 22f, TextAlignmentOptions.Center,
                new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.95f));

            var yes = CreateButton(box, "YesButton", new Vector2(0.08f, 0.1f), new Vector2(0.46f, 0.36f), out var yesLabel);
            var no = CreateButton(box, "NoButton", new Vector2(0.54f, 0.1f), new Vector2(0.92f, 0.36f), out var noLabel);

            var dialog = holder.AddComponent<ConfirmDialog>();
            Wire(dialog, so =>
            {
                so.FindProperty("panel").objectReferenceValue = dim.gameObject;
                so.FindProperty("questionLabel").objectReferenceValue = question;
                so.FindProperty("yesButton").objectReferenceValue = yes;
                so.FindProperty("noButton").objectReferenceValue = no;
                so.FindProperty("yesLabel").objectReferenceValue = yesLabel;
                so.FindProperty("noLabel").objectReferenceValue = noLabel;
                so.FindProperty("inputActions").objectReferenceValue = inputActions;
            });
        }

        private static void BuildFader(Transform root)
        {
            // Por último na hierarquia = desenhado por cima de tudo.
            var go = new GameObject("ScreenFader", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(root, false);
            Stretch(go.GetComponent<RectTransform>());
            var image = go.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = true;
            go.AddComponent<ScreenFader>();
        }

        // ------------------------------------------------------------------ helpers

        internal static RectTransform CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 position,
            Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = PanelColor;
            image.raycastTarget = false;
            return rect;
        }

        internal static TextMeshProUGUI CreateText(Transform parent, string name, float size,
            TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        internal static Button CreateButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            out TextMeshProUGUI label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = ButtonColor;

            label = CreateText(rect, "Label", 20f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            return go.GetComponent<Button>();
        }

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        internal static void Wire(Object target, System.Action<SerializedObject> assign)
        {
            var so = new SerializedObject(target);
            assign(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
