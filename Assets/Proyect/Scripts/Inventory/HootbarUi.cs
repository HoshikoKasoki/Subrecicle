using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem
{
    /// <summary>
    /// Updates the hotbar authored in the scene. It never creates UI objects.
    /// </summary>
    public class ToolHotbarUI : MonoBehaviour
    {
        [Header("Scene UI")]
        [SerializeField] private GameObject hotbarRoot;
        [SerializeField] private Canvas hotbarCanvas;
        [SerializeField] private Button[] slotButtons;
        [SerializeField] private Image[] slotBackgrounds;
        [SerializeField] private Image[] slotIcons;
        [SerializeField] private Text[] slotAmounts;
        [SerializeField] private GameObject[] selectionObjects;
        [SerializeField] private GameObject crosshair;

        [Header("Decoración Maleta / Bolso")]
        [SerializeField] private Image backpackDecorationImage; // Componente Image de la maletita
        [SerializeField] private Sprite backpackClosedSprite;   // Sprite de la maleta cerrada
        [SerializeField] private Sprite backpackOpenSprite;     // Sprite de la maleta abierta

        [Header("Orden de dibujo")]
        [SerializeField] private int gameplaySortingOrder = 10;
        [SerializeField] private int inventorySortingOrder = 40;

        private ToolHotbar hotbar;
        private InventoryUIController inventoryUI;

        private void Awake()
        {
            hotbar = GetComponent<ToolHotbar>();
            inventoryUI = GetComponent<InventoryUIController>();

            if (hotbarCanvas == null && hotbarRoot != null)
                hotbarCanvas = hotbarRoot.GetComponent<Canvas>();

            BindSlotButtons();

            if (hotbarRoot == null || hotbarCanvas == null || slotButtons == null ||
                slotButtons.Length < ToolHotbar.SlotCount || slotBackgrounds == null ||
                slotBackgrounds.Length < ToolHotbar.SlotCount || slotIcons == null ||
                slotIcons.Length < ToolHotbar.SlotCount || slotAmounts == null ||
                slotAmounts.Length < ToolHotbar.SlotCount || selectionObjects == null ||
                selectionObjects.Length < ToolHotbar.SlotCount)
                Debug.LogWarning("ToolHotbarUI necesita referencias a la barra creada en la escena.", this);
        }

        private void OnEnable()
        {
            if (hotbar != null)
                hotbar.OnHotbarChanged += RefreshVisuals;
        }

        private void Start()
        {
            SetInventoryState(false, false);
            RefreshVisuals();
        }

        private void OnDisable()
        {
            if (hotbar != null)
                hotbar.OnHotbarChanged -= RefreshVisuals;
        }

        public void SetInventoryState(bool interfaceOpen, bool allowInventoryTransfer)
        {
            if (hotbarCanvas != null)
                hotbarCanvas.sortingOrder = allowInventoryTransfer
                    ? inventorySortingOrder
                    : gameplaySortingOrder;

            if (crosshair != null)
                crosshair.SetActive(!interfaceOpen);

            // --- DECORACIÓN VISUAL DE LA MALETA ---
            if (backpackDecorationImage != null)
            {
                if (interfaceOpen && backpackOpenSprite != null)
                {
                    backpackDecorationImage.sprite = backpackOpenSprite;
                }
                else if (!interfaceOpen && backpackClosedSprite != null)
                {
                    backpackDecorationImage.sprite = backpackClosedSprite;
                }
            }

            if (slotButtons == null)
                return;

            int count = Mathf.Min(ToolHotbar.SlotCount, slotButtons.Length);
            for (int i = 0; i < count; i++)
            {
                if (slotButtons[i] != null)
                    slotButtons[i].interactable = allowInventoryTransfer;
            }
        }

        private void BindSlotButtons()
        {
            if (slotButtons == null)
                return;

            for (int i = 0; i < slotButtons.Length; i++)
            {
                int slotIndex = i;
                if (slotButtons[i] != null)
                    slotButtons[i].onClick.AddListener(() => ClickSlot(slotIndex));
            }
        }

        private void ClickSlot(int slotIndex)
        {
            if (inventoryUI != null && inventoryUI.IsOpen)
                inventoryUI.ClickHotbarSlot(slotIndex);
        }

        private void RefreshVisuals()
        {
            if (hotbar == null)
                return;

            int count = ToolHotbar.SlotCount;
            for (int i = 0; i < count; i++)
            {
                if (selectionObjects != null && i < selectionObjects.Length && selectionObjects[i] != null)
                    selectionObjects[i].SetActive(i == hotbar.SelectedSlot);

                string itemId = hotbar.GetTool(i);
                int amount = hotbar.GetAmount(i);
                ItemDefinition definition = FindItemDefinition(itemId);

                if (slotBackgrounds != null && i < slotBackgrounds.Length && slotBackgrounds[i] != null)
                {
                    slotBackgrounds[i].color = definition != null && definition.Icon == null
                        ? definition.FallbackColor
                        : Color.white;
                }

                if (slotIcons != null && i < slotIcons.Length && slotIcons[i] != null)
                {
                    slotIcons[i].sprite = definition == null ? null : definition.Icon;
                    slotIcons[i].enabled = definition != null && definition.Icon != null;
                    slotIcons[i].color = Color.white;
                }

                if (slotAmounts != null && i < slotAmounts.Length && slotAmounts[i] != null)
                {
                    slotAmounts[i].text = amount > 1 ? "x" + amount : string.Empty;
                    slotAmounts[i].gameObject.SetActive(amount > 1);
                }
            }
        }

        private static ItemDefinition FindItemDefinition(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;

            foreach (ItemDefinition definition in Resources.FindObjectsOfTypeAll<ItemDefinition>())
            {
                if (definition != null && definition.ItemID == itemId)
                    return definition;
            }

            return null;
        }
    }
}