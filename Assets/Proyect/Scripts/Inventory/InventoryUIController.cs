using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace InventorySystem
{
    public class InventoryUIController : MonoBehaviour
    {
        [Header("Hotbar UI Integration")]
        [SerializeField] private ToolHotbar hotbar;
        [SerializeField] private Button[] hotbarCells;
        [SerializeField] private Image[] hotbarCellImages;
        [SerializeField] private TMP_Text[] hotbarItemLabels;

        [Header("Data")]
        [SerializeField] private Inventory inventory;

        [Header("Scene UI")]
        [SerializeField] private GameObject shadePanel;
        [SerializeField] private GameObject casePanel;
        [SerializeField] private GameObject recipePanelObject;
        [SerializeField] private RectTransform grid;
        [SerializeField] private RectTransform detailsPanelRect;

        [Header("Scene text")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text modeText;
        [SerializeField] private Text detailsText;
        [SerializeField] private Text infoTitle;
        [SerializeField] private Text interactionText;
        [SerializeField] private Text quantityText;

        [Header("Scene buttons")]
        [SerializeField] private Button actionButton;
        [SerializeField] private Button minusButton;
        [SerializeField] private Button plusButton;
        [SerializeField] private Button[] inventoryCells;
        [SerializeField] private Image[] inventoryCellImages;
        [SerializeField] private Button[] recipeButtons;
        [SerializeField] private Text[] recipeButtonLabels;

        [Header("Scene item visuals")]
        [SerializeField] private RectTransform[] itemVisuals;
        [SerializeField] private Image[] itemImages;
        [SerializeField] private Image[] itemIcons;
        [SerializeField] private Outline[] itemOutlines;
        [SerializeField] private Text[] itemLabels;
        [SerializeField] private Text[] itemCounts;

        [Header("Layout")]
        [SerializeField] private float cellSize = 92f;
        [SerializeField] private float cellGap = 5f;
        [SerializeField] private Vector2 normalGridPosition = new Vector2(145f, -125f);
        [SerializeField] private Vector2 recycleGridPosition = new Vector2(30f, -125f);
        [SerializeField] private Vector2 normalDetailsPosition = new Vector2(-145f, -125f);
        [SerializeField] private Vector2 recycleDetailsPosition = new Vector2(-30f, -125f);
        [SerializeField] private Vector2 detailsSize = new Vector2(350f, 450f);

        private ItemInstance selectedItem;
        private PlayerInventoryController playerInventory;
        private FirstPersonController movement;
        private Wallet wallet;
        private SellTable activeSellTable;
        private RecyclingMachine activeRecyclingMachine;
        private int selectedRecipeIndex;
        private int craftAmount = 1;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (inventory == null)
                inventory = GetComponent<Inventory>();

            playerInventory = GetComponent<PlayerInventoryController>();
            movement = GetComponent<FirstPersonController>();
            wallet = GetComponent<Wallet>();

            BindSceneButtons();
            ValidateSceneUi();
        }

        private void Start()
        {
            if (wallet == null)
                wallet = GetComponent<Wallet>();

            CloseInventory();
        }

        private void OnEnable()
        {
            if (inventory != null)
                inventory.OnInventoryChanged += RefreshInventory;
        }

        private void OnDisable()
        {
            if (inventory != null)
                inventory.OnInventoryChanged -= RefreshInventory;
        }

        private void Update()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.tabKey.wasPressedThisFrame ||
                (Keyboard.current.escapeKey.wasPressedThisFrame && IsOpen))
            {
                ToggleInventory();
            }

            // Si el inventario está cerrado, escuchamos la tecla "Q" para soltar el objeto activo del hotbar
            if (!IsOpen && Keyboard.current.qKey.wasPressedThisFrame)
            {
                DropActiveHotbarItemToGround();
            }

            if (IsOpen && Keyboard.current.rKey.wasPressedThisFrame)
                RotateSelected();

            if (!IsOpen && interactionText != null)
            {
                interactionText.text = playerInventory == null
                    ? string.Empty
                    : playerInventory.InteractionMessage;
            }

            if (IsOpen && activeRecyclingMachine != null)
                RefreshRecipeDetails();
        }

        // Método para soltar el objeto del hotbar seleccionado directamente a la arena
       private void DropActiveHotbarItemToGround()
        {
            if (hotbar == null)
                return;

            // SOLO permitimos botar el objeto si está actualmente visible/equipado en la mano
            if (!hotbar.HasItemInHand)
            {
                return;
            }

            int activeSlot = hotbar.SelectedSlot;
            string currentTool = hotbar.GetTool(activeSlot);
            int amountToDrop = hotbar.GetAmount(activeSlot);

            if (!string.IsNullOrEmpty(currentTool) && amountToDrop > 0)
            {
                ItemDefinition defToDrop = FindItemDefinitionSafe(currentTool);
                
                // Restamos 1 unidad del hotbar
                hotbar.RemoveAmount(activeSlot, 1);
                RefreshHotbarVisuals();

                // Si aún quedan más unidades en ese slot, actualizamos la mano para mantener la vista de la lata
                if (hotbar.GetAmount(activeSlot) > 0)
                {
                    hotbar.UpdateEquippedHand();
                }

                if (defToDrop != null && defToDrop.WorldPrefab != null)
                {
                    // Instanciamos 1 unidad frente al jugador en la arena
                    Vector3 spawnPos = transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
                    GameObject droppedItem = Instantiate(defToDrop.WorldPrefab, spawnPos, Quaternion.identity);

                    Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                        rb.AddForce(transform.forward * 2f + Vector3.up * 2f, ForceMode.Impulse);
                    }

                    Collider col = droppedItem.GetComponent<Collider>();
                    if (col != null)
                    {
                        col.enabled = true;
                    }

                    Debug.Log("1 unidad de " + defToDrop.ItemName + " soltada a la arena.");
                }
            }
        }

        public void ToggleInventory()
        {
            if (IsOpen)
                CloseInventory();
            else
                OpenInventory();
        }

        public void OpenInventory()
        {
            if (inventory == null || casePanel == null)
                return;

            IsOpen = true;
            if (shadePanel != null)
                shadePanel.SetActive(true);
            casePanel.SetActive(true);
            UpdateLayoutForCurrentMode();

            if (interactionText != null)
                interactionText.gameObject.SetActive(false);
            if (movement != null)
                movement.SetInputLocked(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            RefreshInventory();
        }

        public void CloseInventory()
        {
            IsOpen = false;
            activeSellTable = null;
            activeRecyclingMachine = null;
            selectedItem = null;
            selectedRecipeIndex = 0;
            craftAmount = 1;

            if (recipePanelObject != null)
                recipePanelObject.SetActive(false);
            if (shadePanel != null)
                shadePanel.SetActive(false);
            if (casePanel != null)
                casePanel.SetActive(false);
            if (interactionText != null)
                interactionText.gameObject.SetActive(true);
            if (movement != null)
                movement.SetInputLocked(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void OpenSellMode(SellTable sellTable)
        {
            if (sellTable == null)
                return;

            activeSellTable = sellTable;
            activeRecyclingMachine = null;
            selectedItem = null;
            craftAmount = 1;
            OpenInventory();
        }

        public void OpenRecycleMode(RecyclingMachine machine)
        {
            if (machine == null)
                return;

            activeSellTable = null;
            activeRecyclingMachine = machine;
            selectedItem = null;
            selectedRecipeIndex = machine.RecipeCount > 0 ? 0 : -1;
            craftAmount = 1;
            OpenInventory();
        }

        private void BindSceneButtons()
        {
            if (hotbarCells != null)
            {
                for (int i = 0; i < hotbarCells.Length; i++)
                {
                    int hotbarIndex = i;
                    if (hotbarCells[i] != null)
                        hotbarCells[i].onClick.AddListener(() => ClickHotbarCell(hotbarIndex));
                }
            }

            if (minusButton != null)
                minusButton.onClick.AddListener(DecreaseCraftAmount);
            if (plusButton != null)
                plusButton.onClick.AddListener(IncreaseCraftAmount);
            if (actionButton != null)
                actionButton.onClick.AddListener(ActionSelected);

            if (inventoryCells != null)
            {
                for (int i = 0; i < inventoryCells.Length; i++)
                {
                    int cellIndex = i;
                    if (inventoryCells[i] != null)
                        inventoryCells[i].onClick.AddListener(() => ClickCell(cellIndex));
                }
            }

            if (recipeButtons != null)
            {
                for (int i = 0; i < recipeButtons.Length; i++)
                {
                    int recipeIndex = i;
                    if (recipeButtons[i] != null)
                        recipeButtons[i].onClick.AddListener(() => SelectRecipe(recipeIndex));
                }
            }
        }

        private void ValidateSceneUi()
        {
            if (inventoryCells == null || inventoryCells.Length < inventory.Capacity)
                Debug.LogWarning("InventoryUIController necesita las 16 celdas físicas de la escena.", this);
            if (grid == null || casePanel == null || detailsText == null)
                Debug.LogWarning("InventoryUIController tiene referencias de UI de escena incompletas.", this);
        }

        private void UpdateLayoutForCurrentMode()
        {
            bool recycleMode = activeRecyclingMachine != null;
            if (grid != null)
                grid.anchoredPosition = recycleMode ? recycleGridPosition : normalGridPosition;

            if (detailsPanelRect != null)
            {
                detailsPanelRect.anchoredPosition = recycleMode
                    ? recycleDetailsPosition
                    : normalDetailsPosition;
                detailsPanelRect.sizeDelta = detailsSize;
            }

            if (recipePanelObject != null)
                recipePanelObject.SetActive(recycleMode);
        }

        private void RefreshInventory()
        {
            if (!IsOpen || inventory == null || grid == null)
                return;

            UpdateLayoutForCurrentMode();
            if (!ContainsItem(selectedItem))
                selectedItem = null;

            RefreshCells();
            RefreshItemVisuals();
            RefreshHotbarVisuals();

            if (titleText != null)
            {
                int balance = wallet == null ? 0 : wallet.Balance;
                titleText.text = "INVENTARIO  <color=#69c475>" + inventory.UsedCells + "/" +
                    inventory.Capacity + "</color>    <color=#f2d66a>¢ " + balance + "</color>";
            }

            if (activeRecyclingMachine != null)
            {
                if (modeText != null)
                    modeText.text = activeRecyclingMachine.StationName.ToUpperInvariant() + "  //  SELECCIONA UNA RECETA";
                RefreshRecipeList();
                RefreshRecipeDetails();
                return;
            }

            if (activeSellTable != null)
            {
                if (modeText != null)
                    modeText.text = activeSellTable.StationName.ToUpperInvariant() + "  //  SELECCIONA Y VENDE UN LOTE";
                RefreshSellUI();
                return;
            }

            if (modeText != null)
                modeText.text = "MALETÍN DE RECUPERACIÓN  // TAB PARA CERRAR";
            if (recipePanelObject != null)
                recipePanelObject.SetActive(false);
            if (actionButton != null)
                actionButton.gameObject.SetActive(false);
            RefreshDetails();
        }

        private void RefreshCells()
        {
            if (inventoryCells == null)
                return;

            for (int i = 0; i < inventoryCells.Length; i++)
            {
                if (inventoryCells[i] == null)
                    continue;

                int x = i % inventory.Width;
                int y = i / inventory.Width;
                bool occupied = inventory.IsCellOccupied(new Vector2Int(x, y));
                if (inventoryCellImages != null && i < inventoryCellImages.Length && inventoryCellImages[i] != null)
                {
                    inventoryCellImages[i].color = occupied
                        ? new Color(0.10f, 0.13f, 0.12f, 1f)
                        : new Color(0.16f, 0.19f, 0.18f, 1f);
                }
            }
        }

        private void RefreshItemVisuals()
        {
            if (itemVisuals == null)
                return;

            for (int i = 0; i < itemVisuals.Length; i++)
            {
                if (itemVisuals[i] != null)
                    itemVisuals[i].gameObject.SetActive(false);
            }

            int visualIndex = 0;
            foreach (ItemInstance item in inventory.Items)
            {
                if (visualIndex >= itemVisuals.Length)
                    break;
                if (item == null || item.Definition == null || itemVisuals[visualIndex] == null)
                    continue;

                RectTransform visual = itemVisuals[visualIndex];
                visual.gameObject.SetActive(true);
                visual.anchoredPosition = new Vector2(
                    item.Position.x * (cellSize + cellGap),
                    -item.Position.y * (cellSize + cellGap));
                visual.sizeDelta = new Vector2(
                    item.Size.x * cellSize + (item.Size.x - 1) * cellGap,
                    item.Size.y * cellSize + (item.Size.y - 1) * cellGap);

                if (itemImages != null && visualIndex < itemImages.Length && itemImages[visualIndex] != null)
                    itemImages[visualIndex].color = ItemColor(item.Definition);

                if (itemIcons != null && visualIndex < itemIcons.Length && itemIcons[visualIndex] != null)
                {
                    itemIcons[visualIndex].sprite = item.Definition.Icon;
                    itemIcons[visualIndex].enabled = item.Definition.Icon != null;
                    itemIcons[visualIndex].color = Color.white;
                }

                if (itemOutlines != null && visualIndex < itemOutlines.Length && itemOutlines[visualIndex] != null)
                {
                    itemOutlines[visualIndex].effectColor = item == selectedItem
                        ? new Color(0.8f, 1f, 0.56f, 1f)
                        : new Color(0.02f, 0.04f, 0.03f, 1f);
                    itemOutlines[visualIndex].effectDistance = item == selectedItem
                        ? new Vector2(3f, -3f)
                        : new Vector2(1f, -1f);
                }

                if (itemLabels != null && visualIndex < itemLabels.Length && itemLabels[visualIndex] != null)
                    itemLabels[visualIndex].text = item.Definition.ItemName.ToUpperInvariant();

                if (itemCounts != null && visualIndex < itemCounts.Length && itemCounts[visualIndex] != null)
                {
                    itemCounts[visualIndex].text = item.Amount > 1 ? "x" + item.Amount : string.Empty;
                    itemCounts[visualIndex].gameObject.SetActive(item.Amount > 1);
                }

                visualIndex++;
            }
        }

        private void RefreshHotbarVisuals()
        {
            if (hotbar == null || hotbarCellImages == null)
                return;

            for (int i = 0; i < ToolHotbar.SlotCount; i++)
            {
                if (i >= hotbarCellImages.Length || hotbarCellImages[i] == null)
                    continue;

                string toolId = hotbar.GetTool(i);
                int amount = hotbar.GetAmount(i);

                if (!string.IsNullOrEmpty(toolId) && amount > 0)
                {
                    ItemDefinition def = FindItemDefinitionSafe(toolId);
                    if (def != null)
                    {
                        // 1. Color / Icono del slot
                        if (def.Icon != null)
                        {
                            hotbarCellImages[i].sprite = def.Icon;
                            hotbarCellImages[i].color = Color.white;
                        }
                        else
                        {
                            hotbarCellImages[i].sprite = null;
                            hotbarCellImages[i].color = def.FallbackColor;
                        }

                        // 2. Texto con nombre y cantidad (ej. LATA DE ALUMINIO x2)
                        if (hotbarItemLabels != null && i < hotbarItemLabels.Length && hotbarItemLabels[i] != null)
                        {
                            string textLabel = def.ItemName.ToUpperInvariant();
                            if (amount > 1)
                            {
                                textLabel += " x" + amount;
                            }

                            hotbarItemLabels[i].text = textLabel;
                            hotbarItemLabels[i].gameObject.SetActive(true);
                        }
                    }
                }
                else
                {
                    // Si la casilla está vacía
                    hotbarCellImages[i].sprite = null;
                    hotbarCellImages[i].color = new Color(0.16f, 0.19f, 0.18f, 1f);

                    if (hotbarItemLabels != null && i < hotbarItemLabels.Length && hotbarItemLabels[i] != null)
                    {
                        hotbarItemLabels[i].text = string.Empty;
                    }
                }
            }
        }

        private void RefreshRecipeList()
        {
            if (recipeButtons == null || activeRecyclingMachine == null)
                return;

            int count = activeRecyclingMachine.RecipeCount;
            for (int i = 0; i < recipeButtons.Length; i++)
            {
                bool visible = i < count && activeRecyclingMachine.GetRecipe(i) != null;
                if (recipeButtons[i] != null)
                {
                    recipeButtons[i].gameObject.SetActive(visible);
                    if (visible)
                    {
                        Image image = recipeButtons[i].GetComponent<Image>();
                        if (image != null)
                            image.color = i == selectedRecipeIndex
                                ? new Color(0.25f, 0.45f, 0.28f, 1f)
                                : new Color(0.16f, 0.19f, 0.18f, 1f);
                    }
                }

                if (recipeButtonLabels != null && i < recipeButtonLabels.Length && recipeButtonLabels[i] != null)
                    recipeButtonLabels[i].text = visible
                        ? activeRecyclingMachine.GetRecipe(i).recipeName
                        : string.Empty;
            }
        }

        private void SelectRecipe(int index)
        {
            if (activeRecyclingMachine == null || index < 0 || index >= activeRecyclingMachine.RecipeCount)
                return;

            selectedRecipeIndex = index;
            craftAmount = 1;
            RefreshInventory();
        }

        private void RefreshRecipeDetails()
        {
            if (activeRecyclingMachine == null || detailsText == null)
                return;

            RecyclingMachine.CraftingRecipe recipe = activeRecyclingMachine.GetRecipe(selectedRecipeIndex);
            if (recipe == null)
            {
                if (infoTitle != null)
                    infoTitle.text = "RECETA";
                detailsText.text = "Selecciona una receta.";
                if (actionButton != null)
                    actionButton.gameObject.SetActive(false);
                UpdateQuantityText();
                return;
            }

            if (infoTitle != null)
                infoTitle.text = recipe.recipeName.ToUpperInvariant();

            string text = string.IsNullOrWhiteSpace(recipe.description) ? string.Empty : recipe.description + "\n\n";
            text += "<b>INGREDIENTES</b>\n";
            if (recipe.ingredients != null)
            {
                foreach (RecyclingMachine.RecipePart part in recipe.ingredients)
                {
                    if (part == null || part.item == null)
                        continue;
                    int required = part.amount * craftAmount;
                    int available = activeRecyclingMachine.GetTotalAmountById(inventory, part.item.ItemID);
                    text += "• " + required + " × " + part.item.ItemName + "  (" + available + ")";
                    text += available >= required ? " <color=#69c475>✓</color>\n" : " <color=#cc5555>✕</color>\n";
                }
            }

            text += "\n<b>RESULTADOS</b>\n";
            if (recipe.outputs != null)
            {
                foreach (RecyclingMachine.RecipePart part in recipe.outputs)
                {
                    if (part != null && part.item != null)
                        text += "• " + part.amount * craftAmount + " × " + part.item.ItemName + "\n";
                }
            }

            if (activeRecyclingMachine.IsProcessing)
                text += "\n" + activeRecyclingMachine.StatusMessage;

            detailsText.text = text;
            UpdateQuantityText();
            RefreshCraftButton(recipe);
        }

        private void RefreshCraftButton(RecyclingMachine.CraftingRecipe recipe)
        {
            if (actionButton == null)
                return;

            actionButton.gameObject.SetActive(true);
            actionButton.interactable = !activeRecyclingMachine.IsProcessing &&
                activeRecyclingMachine.CanCraft(inventory, recipe, craftAmount);
            Text buttonText = actionButton.GetComponentInChildren<Text>();
            if (buttonText != null)
                buttonText.text = activeRecyclingMachine.IsProcessing
                    ? Mathf.RoundToInt(activeRecyclingMachine.Progress * 100f) + "%"
                    : "FABRICAR";
        }

        private void IncreaseCraftAmount()
        {
            if (activeRecyclingMachine != null)
            {
                if (activeRecyclingMachine.IsProcessing)
                    return;

                craftAmount = Mathf.Clamp(craftAmount + 1, 1, 99);
                RefreshRecipeDetails();
                return;
            }

            if (activeSellTable != null && selectedItem != null)
            {
                craftAmount = Mathf.Clamp(craftAmount + 1, 1, selectedItem.Amount);
                RefreshSellUI();
            }
        }

        private void DecreaseCraftAmount()
        {
            if (activeRecyclingMachine != null)
            {
                if (activeRecyclingMachine.IsProcessing)
                    return;

                craftAmount = Mathf.Clamp(craftAmount - 1, 1, 99);
                RefreshRecipeDetails();
                return;
            }

            if (activeSellTable != null && selectedItem != null)
            {
                craftAmount = Mathf.Clamp(craftAmount - 1, 1, selectedItem.Amount);
                RefreshSellUI();
            }
        }

        private void UpdateQuantityText()
        {
            if (quantityText != null)
                quantityText.text = craftAmount.ToString();
        }

        private void ActionSelected()
        {
            if (activeSellTable != null)
                SellSelected();
            else if (activeRecyclingMachine != null)
                CraftSelected();
        }

        private void CraftSelected()
        {
            if (activeRecyclingMachine == null || selectedRecipeIndex < 0)
                return;
            activeRecyclingMachine.TryStartCraft(inventory, selectedRecipeIndex, craftAmount);
            RefreshRecipeDetails();
        }

        private void RefreshSellUI()
        {
            if (selectedItem == null)
                craftAmount = 1;
            else
                craftAmount = Mathf.Clamp(craftAmount, 1, selectedItem.Amount);

            if (minusButton != null)
                minusButton.interactable = selectedItem != null && craftAmount > 1;
            if (plusButton != null)
                plusButton.interactable = selectedItem != null && craftAmount < (selectedItem == null ? 1 : selectedItem.Amount);

            UpdateQuantityText();

            if (actionButton != null)
            {
                actionButton.gameObject.SetActive(selectedItem != null);
                actionButton.interactable = selectedItem != null;
                Text buttonText = actionButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                    buttonText.text = selectedItem == null
                        ? "VENDER LOTE"
                        : "VENDER " + craftAmount;
            }
            RefreshDetails();
        }

        private void SellSelected()
        {
            if (activeSellTable == null || selectedItem == null || wallet == null)
                return;

            int amountToSell = Mathf.Clamp(craftAmount, 1, selectedItem.Amount);
            int remainingAfterSale = selectedItem.Amount - amountToSell;
            if (activeSellTable.TrySellAmount(inventory, wallet, selectedItem, amountToSell, out _))
            {
                if (remainingAfterSale <= 0)
                    selectedItem = null;
                else
                    craftAmount = Mathf.Clamp(craftAmount, 1, remainingAfterSale);
                RefreshInventory();
            }
        }

        private void ClickHotbarCell(int hotbarIndex)
        {
            if (hotbar == null || inventory == null)
                return;

            string currentHotbarTool = hotbar.GetTool(hotbarIndex);
            int currentHotbarAmount = hotbar.GetAmount(hotbarIndex);

            if (selectedItem != null)
            {
                if (selectedItem.Size.x == 1 && selectedItem.Size.y == 1)
                {
                    ItemDefinition defToPut = selectedItem.Definition;
                    int amountToPut = selectedItem.Amount; // Tomamos toda la cantidad que tenía el item seleccionado

                    if (inventory.TryRemoveItem(selectedItem))
                    {
                        // Si ya había algo en el hotbar, lo devolvemos al inventario con su cantidad respectiva
                        if (!string.IsNullOrEmpty(currentHotbarTool))
                        {
                            ItemDefinition oldDef = FindItemDefinitionSafe(currentHotbarTool);
                            if (oldDef != null)
                            {
                                inventory.TryAddItem(oldDef, currentHotbarAmount);
                            }
                        }

                        // Colocamos el nuevo objeto con su cantidad total en el hotbar
                        hotbar.SetTool(hotbarIndex, defToPut.ItemID, amountToPut);
                        selectedItem = null;
                        RefreshInventory();
                    }
                }
                else
                {
                    Debug.Log("Este objeto es muy grande. Solo se pueden colocar objetos de 1x1 en el hotbar.");
                }
            }
            else if (!string.IsNullOrEmpty(currentHotbarTool))
            {
                ItemDefinition defToReturn = FindItemDefinitionSafe(currentHotbarTool);
                if (defToReturn != null)
                {
                    hotbar.ClearSlot(hotbarIndex);

                    // Devolvemos todas las unidades que estaban en el hotbar de regreso al inventario
                    if (!inventory.TryAddItem(defToReturn, currentHotbarAmount))
                    {
                        // Si no hay espacio, lo restauramos en el hotbar
                        hotbar.SetTool(hotbarIndex, defToReturn.ItemID, currentHotbarAmount);
                    }

                    RefreshInventory();
                }
            }
        }

        private void ClickCell(int cellIndex)
        {
            if (inventory == null || cellIndex < 0 || cellIndex >= inventory.Capacity)
                return;

            Vector2Int cell = new Vector2Int(cellIndex % inventory.Width, cellIndex / inventory.Width);
            ItemInstance clicked = inventory.GetItemAtCell(cell);
            if (selectedItem == null)
            {
                selectedItem = clicked;
                craftAmount = 1;
            }
            else if (clicked == selectedItem)
            {
                selectedItem = null;
                craftAmount = 1;
            }
            else if (clicked != null)
            {
                selectedItem = clicked;
                craftAmount = 1;
            }
            else
                inventory.TryMoveItem(selectedItem, cell, selectedItem.Rotated);
            RefreshInventory();
        }

        private void RotateSelected()
        {
            if (selectedItem != null && inventory != null)
            {
                inventory.TryRotateItem(selectedItem);
                RefreshInventory();
            }
        }

        private void RefreshDetails()
        {
            if (detailsText == null)
                return;
            if (selectedItem == null || selectedItem.Definition == null)
            {
                detailsText.text = "Selecciona un objeto.";
                return;
            }

            ItemDefinition item = selectedItem.Definition;
            string description = string.IsNullOrWhiteSpace(item.Description) ? "Sin descripción." : item.Description;
            detailsText.text = "<b>" + item.ItemName + "</b>\n\n" +
                item.Category.ToString().ToUpperInvariant() + "\nID  " + item.ItemID + "\n\n" +
                description + "\n\nTAMAÑO  " + selectedItem.Size.x + " × " + selectedItem.Size.y +
                (item.Stackable ? "\nCANTIDAD  " + selectedItem.Amount + "/" + item.MaxStack : string.Empty);
        }

        private bool ContainsItem(ItemInstance item)
        {
            if (item == null || inventory == null)
                return false;
            foreach (ItemInstance candidate in inventory.Items)
            {
                if (candidate == item)
                    return true;
            }
            return false;
        }

        public void RefreshStationUI()
        {
            if (!IsOpen)
                return;
            if (activeRecyclingMachine != null)
            {
                RefreshRecipeDetails();
            }
            else if (activeSellTable != null)
            {
                RefreshSellUI();
            }
        }

        private static Color ItemColor(ItemDefinition item)
        {
            switch (item.Category)
            {
                case ItemCategory.Wood: return new Color(0.44f, 0.25f, 0.12f, 1f);
                case ItemCategory.Metal: return new Color(0.28f, 0.38f, 0.43f, 1f);
                case ItemCategory.Paper: return new Color(0.55f, 0.49f, 0.31f, 1f);
                case ItemCategory.Plastic: return new Color(0.18f, 0.43f, 0.49f, 1f);
                case ItemCategory.Porcelain: return new Color(0.70f, 0.70f, 0.72f, 1f);
                case ItemCategory.Glass: return new Color(0.16f, 0.53f, 0.48f, 1f);
                case ItemCategory.Organic: return new Color(0.30f, 0.48f, 0.20f, 1f);
                case ItemCategory.Tool: return new Color(0.54f, 0.26f, 0.16f, 1f);
                case ItemCategory.KeyItem: return new Color(0.55f, 0.47f, 0.12f, 1f);
                case ItemCategory.Consumable: return new Color(0.42f, 0.24f, 0.48f, 1f);
                case ItemCategory.Equipment: return new Color(0.24f, 0.32f, 0.48f, 1f);
                case ItemCategory.Quest: return new Color(0.48f, 0.43f, 0.20f, 1f);
                default: return item.FallbackColor;
            }
        }

        private ItemDefinition FindItemDefinitionSafe(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;

            foreach (var item in inventory.Items)
            {
                if (item != null && item.Definition != null && item.Definition.ItemID == itemId)
                    return item.Definition;
            }

            ItemDefinition[] allDefinitions = Resources.FindObjectsOfTypeAll<ItemDefinition>();
            foreach (var def in allDefinitions)
            {
                if (def != null && def.ItemID == itemId)
                    return def;
            }

            return null;
        }
    }
}