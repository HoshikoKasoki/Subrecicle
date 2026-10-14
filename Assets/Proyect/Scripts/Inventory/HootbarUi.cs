using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem
{
    public class ToolHotbarUI : MonoBehaviour
    {
        [Header("Dimensions")]
        [SerializeField] private float slotSize = 52f;
        [SerializeField] private float spacing = 6f;
        [SerializeField] private float bottomMargin = 24f;

        [Header("Colors")]
        [SerializeField]
        private Color barColor =
            new Color(0.03f, 0.03f, 0.03f, 0.88f);

        [SerializeField]
        private Color slotColor =
            new Color(0.12f, 0.12f, 0.12f, 0.95f);

        [SerializeField]
        private Color normalBorderColor =
            new Color(0.35f, 0.35f, 0.35f, 1f);

        [SerializeField]
        private Color selectedBorderColor =
            new Color(1f, 0.75f, 0.15f, 1f);

        private ToolHotbar hotbar;
        private Canvas canvas;
        private RectTransform hotbarRoot;

        private Image[] slotBackgrounds;
        private Image[] slotBorders;
        private Text[] slotNumbers;

        private void Awake()
        {
            hotbar = GetComponent<ToolHotbar>();

            if (hotbar == null)
                hotbar = gameObject.AddComponent<ToolHotbar>();

            CreateCanvas();
            CreateHotbar();
            RefreshVisuals();
        }

        private void OnEnable()
        {
            if (hotbar != null)
                hotbar.OnHotbarChanged += RefreshVisuals;
        }

        private void OnDisable()
        {
            if (hotbar != null)
                hotbar.OnHotbarChanged -= RefreshVisuals;
        }

        // =========================================================
        // CANVAS
        // =========================================================

        private void CreateCanvas()
        {
            GameObject canvasObject = new GameObject(
                "Tool Hotbar Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            scaler.referenceResolution =
                new Vector2(1920f, 1080f);

            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            scaler.matchWidthOrHeight = 0.5f;
        }

        // =========================================================
        // HOTBAR
        // =========================================================

        private void CreateHotbar()
        {
            float barWidth =
                (slotSize * ToolHotbar.SlotCount) +
                (spacing * (ToolHotbar.SlotCount - 1)) +
                16f;

            float barHeight = slotSize + 16f;

            GameObject root =
                new GameObject(
                    "Tool Hotbar",
                    typeof(RectTransform),
                    typeof(Image));

            hotbarRoot =
                root.GetComponent<RectTransform>();

            hotbarRoot.SetParent(canvas.transform, false);

            hotbarRoot.anchorMin = new Vector2(0.5f, 0f);
            hotbarRoot.anchorMax = new Vector2(0.5f, 0f);
            hotbarRoot.pivot = new Vector2(0.5f, 0f);

            hotbarRoot.anchoredPosition =
                new Vector2(0f, bottomMargin);

            hotbarRoot.sizeDelta =
                new Vector2(barWidth, barHeight);

            Image background =
                root.GetComponent<Image>();

            background.color = barColor;

            slotBackgrounds =
                new Image[ToolHotbar.SlotCount];

            slotBorders =
                new Image[ToolHotbar.SlotCount];

            slotNumbers =
                new Text[ToolHotbar.SlotCount];

            float totalSlotsWidth =
                (slotSize * ToolHotbar.SlotCount) +
                (spacing * (ToolHotbar.SlotCount - 1));

            float startX =
                -totalSlotsWidth / 2f +
                slotSize / 2f;

            for (int i = 0; i < ToolHotbar.SlotCount; i++)
            {
                CreateSlot(root.transform, i, startX + i * (slotSize + spacing));
            }
        }

        // =========================================================
        // SLOT
        // =========================================================

        private void CreateSlot(
            Transform parent,
            int index,
            float x)
        {
            // Borde.
            GameObject borderObject =
                new GameObject(
                    $"Tool Slot {index + 1} Border",
                    typeof(RectTransform),
                    typeof(Image));

            RectTransform borderRect =
                borderObject.GetComponent<RectTransform>();

            borderRect.SetParent(parent, false);

            borderRect.anchorMin =
                new Vector2(0.5f, 0.5f);

            borderRect.anchorMax =
                new Vector2(0.5f, 0.5f);

            borderRect.pivot =
                new Vector2(0.5f, 0.5f);

            borderRect.anchoredPosition =
                new Vector2(x, 0f);

            borderRect.sizeDelta =
                new Vector2(slotSize + 4f, slotSize + 4f);

            Image border =
                borderObject.GetComponent<Image>();

            border.color = normalBorderColor;

            slotBorders[index] = border;

            // Interior de la casilla.
            GameObject slotObject =
                new GameObject(
                    $"Tool Slot {index + 1}",
                    typeof(RectTransform),
                    typeof(Image));

            RectTransform slotRect =
                slotObject.GetComponent<RectTransform>();

            slotRect.SetParent(parent, false);

            slotRect.anchorMin =
                new Vector2(0.5f, 0.5f);

            slotRect.anchorMax =
                new Vector2(0.5f, 0.5f);

            slotRect.pivot =
                new Vector2(0.5f, 0.5f);

            slotRect.anchoredPosition =
                new Vector2(x, 0f);

            slotRect.sizeDelta =
                new Vector2(slotSize, slotSize);

            Image slotImage =
                slotObject.GetComponent<Image>();

            slotImage.color = slotColor;

            slotBackgrounds[index] = slotImage;

            // Número.
            GameObject numberObject =
                new GameObject(
                    $"Tool Slot {index + 1} Number",
                    typeof(RectTransform),
                    typeof(Text));

            RectTransform numberRect =
                numberObject.GetComponent<RectTransform>();

            numberRect.SetParent(slotObject.transform, false);

            numberRect.anchorMin =
                new Vector2(0f, 1f);

            numberRect.anchorMax =
                new Vector2(0f, 1f);

            numberRect.pivot =
                new Vector2(0f, 1f);

            numberRect.anchoredPosition =
                new Vector2(4f, -3f);

            numberRect.sizeDelta =
                new Vector2(24f, 24f);

            Text numberText =
                numberObject.GetComponent<Text>();

            numberText.font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");

            numberText.text =
                (index + 1).ToString();

            numberText.fontSize = 14;
            numberText.alignment = TextAnchor.UpperLeft;
            numberText.color = Color.white;

            slotNumbers[index] = numberText;
        }

        // =========================================================
        // UPDATE VISUALS
        // =========================================================

        private void RefreshVisuals()
        {
            if (hotbar == null)
                return;

            for (int i = 0; i < ToolHotbar.SlotCount; i++)
            {
                bool selected =
                    i == hotbar.SelectedSlot;

                slotBorders[i].color =
                    selected
                        ? selectedBorderColor
                        : normalBorderColor;

                // Por ahora no existen herramientas.
                // Cuando existan, aquí irá su icono.
                string tool =
                    hotbar.GetTool(i);

                slotBackgrounds[i].color =
                    string.IsNullOrEmpty(tool)
                        ? slotColor
                        : slotColor;
            }
        }
    }
}