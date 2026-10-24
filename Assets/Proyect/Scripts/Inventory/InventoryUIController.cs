using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace InventorySystem
{
    /// <summary>
    /// Actualiza las vistas de inventario creadas físicamente en la escena.
    /// No crea, clona ni reordena elementos de interfaz en tiempo de ejecución.
    /// </summary>
    public class InventoryUIController : MonoBehaviour
    {
        [Serializable]
        private sealed class SceneView
        {
            public GameObject canvasRoot;

            [Header("Textos")]
            public Text titleText;
            public Text modeText;
            public Text detailsText;
            public Text infoTitle;
            public Text quantityText;
            public Text actionButtonLabel;

            [Header("Botones")]
            public Button closeButton;
            public Button actionButton;
            public Button minusButton;
            public Button plusButton;
            public Button[] inventoryCells = Array.Empty<Button>();
            public Button[] recipeButtons = Array.Empty<Button>();
            public Text[] recipeButtonLabels = Array.Empty<Text>();
            public Outline[] recipeOutlines = Array.Empty<Outline>();

            [Header("Objetos del inventario")]
            public RectTransform[] itemVisuals = Array.Empty<RectTransform>();
            public Image[] itemImages = Array.Empty<Image>();
            public Image[] itemIcons = Array.Empty<Image>();
            public Outline[] itemOutlines = Array.Empty<Outline>();
            public Text[] itemLabels = Array.Empty<Text>();
            public Text[] itemCounts = Array.Empty<Text>();
        }

        [Header("Datos")]
        [SerializeField] private Inventory inventory;

        [Header("Canvas físicos de la escena")]
        [SerializeField] private SceneView inventoryView = new SceneView();
        [SerializeField] private SceneView saleView = new SceneView();
        [SerializeField] private SceneView fabricatorView1 = new SceneView();
        [SerializeField] private SceneView fabricatorView2 = new SceneView();

        [Header("HUD / Hotbar")]
        [SerializeField] private Text interactionText;

        private ItemInstance selectedItem;
        private PlayerInventoryController playerInventory;
        private FirstPersonController movement;
        private Wallet wallet;
        private ToolHotbar hotbar;
        private ToolHotbarUI hotbarUI;
        private SellTable activeSellTable;
        private RecyclingMachine activeRecyclingMachine;
        private SceneView activeView;
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
            hotbar = GetComponent<ToolHotbar>();
            hotbarUI = GetComponent<ToolHotbarUI>();

            BindSceneView(inventoryView);
            BindSceneView(saleView);
            BindSceneView(fabricatorView1);
            BindSceneView(fabricatorView2);
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
            if (!IsOpen && interactionText != null)
            {
                interactionText.text = playerInventory == null
                    ? string.Empty
                    : playerInventory.InteractionMessage;
            }

           if (!IsOpen && Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
            {
                DropSelectedHotbarItem();
            }

            if (Keyboard.current == null)
                return;

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                ToggleInventory();
                return;
            }

            if (IsOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseInventory();
                return;
            }

            if (IsOpen && Keyboard.current.rKey.wasPressedThisFrame)
                RotateSelected();

            if (IsOpen && activeRecyclingMachine != null)
                RefreshRecipeDetails();
        }

        private void DropSelectedHotbarItem()
        {
            if (hotbar == null || !hotbar.HasItemInHand)
                return;

            int slotIndex = hotbar.SelectedSlot;
            string itemId = hotbar.GetTool(slotIndex);
            int amount = hotbar.GetAmount(slotIndex);
            if (string.IsNullOrEmpty(itemId) || amount <= 0)
                return;

            ItemDefinition definition = FindItemDefinition(itemId);
            if (definition == null || definition.WorldPrefab == null)
                return;

            Vector3 spawnPosition = transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
            GameObject droppedObject = Instantiate(
                definition.WorldPrefab,
                spawnPosition,
                Quaternion.identity);

            PickupItem pickup = droppedObject.GetComponentInChildren<PickupItem>(true);
            if (pickup != null)
                pickup.SetAmount(1);

            foreach (Collider itemCollider in droppedObject.GetComponentsInChildren<Collider>(true))
                itemCollider.enabled = true;

            Rigidbody rigidbody = droppedObject.GetComponentInChildren<Rigidbody>(true);
            if (rigidbody != null)
            {
                rigidbody.isKinematic = false;
                rigidbody.AddForce(transform.forward * 2f + Vector3.up * 2f, ForceMode.Impulse);
            }

            hotbar.RemoveAmount(slotIndex, 1);
            if (hotbar.GetAmount(slotIndex) > 0)
                hotbar.UpdateEquippedHand();
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
            activeSellTable = null;
            activeRecyclingMachine = null;
            selectedItem = null;
            selectedRecipeIndex = 0;
            craftAmount = 1;
            OpenView(inventoryView);
        }

        public void OpenSellMode(SellTable sellTable)
        {
            if (sellTable == null)
                return;

            activeSellTable = sellTable;
            activeRecyclingMachine = null;
            selectedItem = null;
            selectedRecipeIndex = 0;
            craftAmount = 1;
            OpenView(saleView);
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

            SceneView view = machine.InterfaceCanvas == RecyclingMachine.FabricatorCanvas.Fabricadora2
                ? fabricatorView2
                : fabricatorView1;
            OpenView(view);
        }

        public void CloseInventory()
        {
            IsOpen = false;
            activeSellTable = null;
            activeRecyclingMachine = null;
            activeView = null;
            selectedItem = null;
            selectedRecipeIndex = 0;
            craftAmount = 1;

            SetViewActive(inventoryView, false);
            SetViewActive(saleView, false);
            SetViewActive(fabricatorView1, false);
            SetViewActive(fabricatorView2, false);

            if (interactionText != null)
                interactionText.gameObject.SetActive(true);
            if (hotbarUI != null)
                hotbarUI.SetInventoryState(false, false);
            if (movement != null)
                movement.SetInputLocked(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OpenView(SceneView view)
        {
            if (inventory == null || view == null || view.canvasRoot == null)
                return;

            SetViewActive(inventoryView, false);
            SetViewActive(saleView, false);
            SetViewActive(fabricatorView1, false);
            SetViewActive(fabricatorView2, false);

            activeView = view;
            SetViewActive(activeView, true);
            IsOpen = true;

            if (interactionText != null)
                interactionText.gameObject.SetActive(false);
            if (hotbarUI != null)
                hotbarUI.SetInventoryState(true, view == inventoryView);
            if (movement != null)
                movement.SetInputLocked(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            RefreshInventory();
        }

        private static void SetViewActive(SceneView view, bool active)
        {
            if (view != null && view.canvasRoot != null)
                view.canvasRoot.SetActive(active);
        }

        private void BindSceneView(SceneView view)
        {
            if (view == null)
                return;

            if (view.closeButton != null)
                view.closeButton.onClick.AddListener(CloseInventory);
            if (view.minusButton != null)
                view.minusButton.onClick.AddListener(DecreaseCraftAmount);
            if (view.plusButton != null)
                view.plusButton.onClick.AddListener(IncreaseCraftAmount);
            if (view.actionButton != null)
                view.actionButton.onClick.AddListener(ActionSelected);

            if (view.inventoryCells != null)
            {
                for (int i = 0; i < view.inventoryCells.Length; i++)
                {
                    int cellIndex = i;
                    if (view.inventoryCells[i] != null)
                        view.inventoryCells[i].onClick.AddListener(() => ClickCell(cellIndex));
                }
            }

            if (view.recipeButtons != null)
            {
                for (int i = 0; i < view.recipeButtons.Length; i++)
                {
                    int recipeIndex = i;
                    if (view.recipeButtons[i] != null)
                        view.recipeButtons[i].onClick.AddListener(() => SelectRecipe(recipeIndex));
                }
            }
        }

        private void ValidateSceneUi()
        {
            ValidateView(inventoryView, "Inventario");
            ValidateView(saleView, "Venta");
            ValidateView(fabricatorView1, "Fabricadora 1");
            ValidateView(fabricatorView2, "Fabricadora 2");

            if (interactionText == null)
                Debug.LogWarning("Falta Interaction Message dentro del Canvas de Hotbar.", this);
        }

        private void ValidateView(SceneView view, string viewName)
        {
            if (view == null || view.canvasRoot == null || view.inventoryCells == null ||
                inventory == null || view.inventoryCells.Length < inventory.Capacity)
            {
                Debug.LogWarning("La vista física de " + viewName + " tiene referencias incompletas.", this);
            }
        }

        private void RefreshInventory()
        {
            if (!IsOpen || inventory == null || activeView == null)
                return;

            if (!ContainsItem(selectedItem))
                selectedItem = null;

            RefreshItemVisuals();

            if (activeView.titleText != null)
            {
                int balance = wallet == null ? 0 : wallet.Balance;
                activeView.titleText.text = "INVENTARIO  <color=#69c475>" + inventory.UsedCells + "/" +
                    inventory.Capacity + "</color>    <color=#f2d66a>¢ " + balance + "</color>";
            }

            if (activeRecyclingMachine != null)
            {
                if (activeView.modeText != null)
                    activeView.modeText.text = activeRecyclingMachine.StationName.ToUpperInvariant() +
                        "  //  SELECCIONA UNA RECETA";
                RefreshRecipeList();
                RefreshRecipeDetails();
                return;
            }

            if (activeSellTable != null)
            {
                if (activeView.modeText != null)
                    activeView.modeText.text = activeSellTable.StationName.ToUpperInvariant() +
                        "  //  SELECCIONA Y VENDE UN LOTE";
                RefreshSellUI();
                return;
            }

            if (activeView.modeText != null)
                activeView.modeText.text = "MALETÍN DE RECUPERACIÓN  //  E PARA CERRAR";
            if (activeView.actionButton != null)
                activeView.actionButton.gameObject.SetActive(false);
            RefreshDetails();
        }

        private void RefreshItemVisuals()
        {
            if (activeView.itemVisuals == null)
                return;

            for (int i = 0; i < activeView.itemVisuals.Length; i++)
            {
                if (activeView.itemVisuals[i] != null)
                    activeView.itemVisuals[i].gameObject.SetActive(false);
            }

            int visualIndex = 0;
            foreach (ItemInstance item in inventory.Items)
            {
                if (visualIndex >= activeView.itemVisuals.Length)
                    break;
                if (item == null || item.Definition == null || activeView.itemVisuals[visualIndex] == null)
                    continue;

                RectTransform visual = activeView.itemVisuals[visualIndex];
                PlaceVisualOverPhysicalCells(activeView, item, visual);
                visual.gameObject.SetActive(true);

                if (visualIndex < activeView.itemImages.Length && activeView.itemImages[visualIndex] != null)
                    activeView.itemImages[visualIndex].color = ItemColor(item.Definition);

                if (visualIndex < activeView.itemIcons.Length && activeView.itemIcons[visualIndex] != null)
                {
                    activeView.itemIcons[visualIndex].sprite = item.Definition.Icon;
                    activeView.itemIcons[visualIndex].enabled = item.Definition.Icon != null;
                    activeView.itemIcons[visualIndex].color = Color.white;
                }

                if (visualIndex < activeView.itemOutlines.Length && activeView.itemOutlines[visualIndex] != null)
                    activeView.itemOutlines[visualIndex].enabled = item == selectedItem;

                if (visualIndex < activeView.itemLabels.Length && activeView.itemLabels[visualIndex] != null)
                    activeView.itemLabels[visualIndex].text = item.Definition.ItemName.ToUpperInvariant();

                if (visualIndex < activeView.itemCounts.Length && activeView.itemCounts[visualIndex] != null)
                {
                    activeView.itemCounts[visualIndex].text = item.Amount > 1 ? "x" + item.Amount : string.Empty;
                    activeView.itemCounts[visualIndex].gameObject.SetActive(item.Amount > 1);
                }

                visualIndex++;
            }
        }

        private void PlaceVisualOverPhysicalCells(SceneView view, ItemInstance item, RectTransform visual)
        {
            int startIndex = item.Position.y * inventory.Width + item.Position.x;
            if (view.inventoryCells == null || startIndex < 0 || startIndex >= view.inventoryCells.Length ||
                view.inventoryCells[startIndex] == null)
                return;

            RectTransform first = view.inventoryCells[startIndex].transform as RectTransform;
            if (first == null)
                return;

            visual.anchorMin = first.anchorMin;
            visual.anchorMax = first.anchorMax;
            visual.pivot = first.pivot;

            int endX = item.Position.x + item.Size.x - 1;
            int endY = item.Position.y + item.Size.y - 1;
            int endIndex = endY * inventory.Width + endX;
            RectTransform last = endX >= 0 && endX < inventory.Width &&
                endY >= 0 && endY < inventory.Height &&
                endIndex >= 0 && endIndex < view.inventoryCells.Length &&
                view.inventoryCells[endIndex] != null
                    ? view.inventoryCells[endIndex].transform as RectTransform
                    : first;

            if (last == null)
                last = first;

            // El RectTransform se ancla por su pivote. Al aumentar sólo el tamaño
            // desde la primera celda, un objeto 2x2 crecía hacia fuera del grid.
            // Calculamos los límites reales de la primera y última celda para que
            // el visual quede centrado exactamente sobre todas las que ocupa.
            Vector2 firstMin = first.anchoredPosition - Vector2.Scale(first.rect.size, first.pivot);
            Vector2 firstMax = firstMin + first.rect.size;
            Vector2 lastMin = last.anchoredPosition - Vector2.Scale(last.rect.size, last.pivot);
            Vector2 lastMax = lastMin + last.rect.size;
            Vector2 visualMin = Vector2.Min(firstMin, lastMin);
            Vector2 visualMax = Vector2.Max(firstMax, lastMax);
            Vector2 visualSize = visualMax - visualMin;

            visual.sizeDelta = visualSize;
            visual.anchoredPosition = visualMin + Vector2.Scale(visualSize, visual.pivot);
        }

        private void RefreshRecipeList()
        {
            if (activeView.recipeButtons == null || activeRecyclingMachine == null)
                return;

            int count = activeRecyclingMachine.RecipeCount;
            for (int i = 0; i < activeView.recipeButtons.Length; i++)
            {
                bool visible = i < count && activeRecyclingMachine.GetRecipe(i) != null;
                if (activeView.recipeButtons[i] != null)
                    activeView.recipeButtons[i].gameObject.SetActive(visible);

                if (i < activeView.recipeButtonLabels.Length && activeView.recipeButtonLabels[i] != null)
                    activeView.recipeButtonLabels[i].text = visible
                        ? activeRecyclingMachine.GetRecipe(i).recipeName
                        : string.Empty;

                if (i < activeView.recipeOutlines.Length && activeView.recipeOutlines[i] != null)
                    activeView.recipeOutlines[i].enabled = visible && i == selectedRecipeIndex;
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
            if (activeRecyclingMachine == null || activeView == null || activeView.detailsText == null)
                return;

            RecyclingMachine.CraftingRecipe recipe = activeRecyclingMachine.GetRecipe(selectedRecipeIndex);
            if (recipe == null)
            {
                if (activeView.infoTitle != null)
                    activeView.infoTitle.text = "RECETA";
                activeView.detailsText.text = "Selecciona una receta.";
                if (activeView.actionButton != null)
                    activeView.actionButton.gameObject.SetActive(false);
                UpdateQuantityText();
                return;
            }

            if (activeView.infoTitle != null)
                activeView.infoTitle.text = recipe.recipeName.ToUpperInvariant();

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

            activeView.detailsText.text = text;
            UpdateQuantityText();
            RefreshCraftButton(recipe);
        }

        private void RefreshCraftButton(RecyclingMachine.CraftingRecipe recipe)
        {
            if (activeView.actionButton == null)
                return;

            activeView.actionButton.gameObject.SetActive(true);
            activeView.actionButton.interactable = !activeRecyclingMachine.IsProcessing &&
                activeRecyclingMachine.CanCraft(inventory, recipe, craftAmount);
            if (activeView.actionButtonLabel != null)
                activeView.actionButtonLabel.text = activeRecyclingMachine.IsProcessing
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
            if (activeView != null && activeView.quantityText != null)
                activeView.quantityText.text = craftAmount.ToString();
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

            if (activeView.minusButton != null)
                activeView.minusButton.interactable = selectedItem != null && craftAmount > 1;
            if (activeView.plusButton != null)
                activeView.plusButton.interactable = selectedItem != null && craftAmount < selectedItem.Amount;

            UpdateQuantityText();

            if (activeView.actionButton != null)
            {
                activeView.actionButton.gameObject.SetActive(selectedItem != null);
                activeView.actionButton.interactable = selectedItem != null;
            }
            if (activeView.actionButtonLabel != null)
                activeView.actionButtonLabel.text = selectedItem == null
                    ? "VENDER LOTE"
                    : "VENDER " + craftAmount;

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

        public void ClickHotbarSlot(int hotbarIndex)
        {
            if (!IsOpen || activeView != inventoryView || hotbar == null || inventory == null ||
                hotbarIndex < 0 || hotbarIndex >= ToolHotbar.SlotCount)
            {
                return;
            }

            string currentToolId = hotbar.GetTool(hotbarIndex);
            int currentAmount = hotbar.GetAmount(hotbarIndex);

            if (selectedItem != null)
            {
                if (selectedItem.Size != Vector2Int.one || selectedItem.Definition == null)
                {
                    Debug.Log("Sólo se pueden colocar objetos de 1x1 en el Hotbar.", this);
                    return;
                }

                ItemDefinition selectedDefinition = selectedItem.Definition;
                int selectedAmount = selectedItem.Amount;
                ItemDefinition previousDefinition = string.IsNullOrEmpty(currentToolId)
                    ? null
                    : FindItemDefinition(currentToolId);

                if (!string.IsNullOrEmpty(currentToolId) && previousDefinition == null)
                {
                    Debug.LogWarning("No se encontró la definición del objeto que estaba en el Hotbar.", this);
                    return;
                }

                if (!inventory.TryRemoveItem(selectedItem))
                    return;

                if (previousDefinition != null && !inventory.TryAddItem(previousDefinition, currentAmount))
                {
                    inventory.TryAddItem(selectedDefinition, selectedAmount);
                    Debug.LogWarning("No hay espacio para intercambiar el objeto del Hotbar.", this);
                    return;
                }

                hotbar.SetTool(hotbarIndex, selectedDefinition.ItemID, selectedAmount);
                selectedItem = null;
                RefreshInventory();
                return;
            }

            if (string.IsNullOrEmpty(currentToolId) || currentAmount <= 0)
                return;

            ItemDefinition definitionToReturn = FindItemDefinition(currentToolId);
            if (definitionToReturn == null)
                return;

            hotbar.ClearSlot(hotbarIndex);
            if (!inventory.TryAddItem(definitionToReturn, currentAmount))
            {
                hotbar.SetTool(hotbarIndex, currentToolId, currentAmount);
                Debug.LogWarning("No hay espacio para devolver el objeto del Hotbar al inventario.", this);
            }

            RefreshInventory();
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
            {
                inventory.TryMoveItem(selectedItem, cell, selectedItem.Rotated);
            }

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
            if (activeView == null || activeView.detailsText == null)
                return;
            if (selectedItem == null || selectedItem.Definition == null)
            {
                activeView.detailsText.text = "Selecciona un objeto.";
                return;
            }

            ItemDefinition item = selectedItem.Definition;
            string description = string.IsNullOrWhiteSpace(item.Description) ? "Sin descripción." : item.Description;
            activeView.detailsText.text = "<b>" + item.ItemName + "</b>\n\n" +
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

        public void RefreshStationUI()
        {
            if (!IsOpen)
                return;
            if (activeRecyclingMachine != null)
                RefreshRecipeDetails();
            else if (activeSellTable != null)
                RefreshSellUI();
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
    }
}
