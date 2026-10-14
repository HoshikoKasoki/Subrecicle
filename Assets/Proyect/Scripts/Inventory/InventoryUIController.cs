using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace InventorySystem
{
    /// <summary>
    /// Interfaz autocontenida del inventario.
    ///
    /// MODOS:
    /// 1. Inventario normal
    /// 2. Mesa de venta
    /// 3. Recicladora / fabricación
    ///
    /// El panel de recetas SOLO aparece en modo recicladora.
    ///
    /// En inventario normal y venta:
    /// - Inventario centrado
    /// - Panel de detalles centrado junto al inventario
    ///
    /// En recicladora:
    /// - Inventario a la izquierda
    /// - Lista de recetas en el centro
    /// - Detalles a la derecha
    /// </summary>
    public class InventoryUIController : MonoBehaviour
    {
        // =========================================================
        // DATA
        // =========================================================

        [Header("Data")]
        [SerializeField]
        private Inventory inventory;

        [Header("Legacy scene reference (optional)")]
        [SerializeField]
        private GameObject inventoryPanel;

        // =========================================================
        // DIMENSIONES INVENTARIO
        // =========================================================

        // El Inventory debe estar configurado como 4x4.
        private const float CellSize = 92f;
        private const float Gap = 5f;

        // =========================================================
        // UI GENERAL
        // =========================================================

        private GameObject runtimeCanvas;
        private GameObject shadePanel;
        private GameObject casePanel;

        private RectTransform grid;

        private Text titleText;
        private Text modeText;
        private Text detailsText;
        private Text infoTitle;
        private Text interactionText;

        // =========================================================
        // PANEL RECETAS
        // =========================================================

        private GameObject recipePanelObject;
        private RectTransform recipeContent;
        private ScrollRect recipeScroll;

        // =========================================================
        // BOTONES
        // =========================================================

        private Button actionButton;
        private Button minusButton;
        private Button plusButton;

        // =========================================================
        // ESTADO
        // =========================================================

        private ItemInstance selectedItem;

        private PlayerInventoryController playerInventory;
        private FirstPersonController movement;
        private Wallet wallet;

        private SellTable activeSellTable;
        private RecyclingMachine activeRecyclingMachine;

        private int selectedRecipeIndex = 0;
        private int craftAmount = 1;

        public bool IsOpen
        {
            get;
            private set;
        }

        // =========================================================
        // UNITY
        // =========================================================

        private void Awake()
        {
            if (inventory == null)
                inventory = GetComponent<Inventory>();

            playerInventory =
                GetComponent<PlayerInventoryController>();

            movement =
                GetComponent<FirstPersonController>();

            wallet =
                GetComponent<Wallet>();
        }

        private void Start()
        {
            if (wallet == null)
                wallet = GetComponent<Wallet>();

            if (inventoryPanel != null)
                inventoryPanel.SetActive(false);

            BuildView();

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

            // =====================================================
            // TAB / ESC
            // =====================================================

            if (
                Keyboard.current.tabKey.wasPressedThisFrame ||
                (
                    Keyboard.current.escapeKey.wasPressedThisFrame &&
                    IsOpen
                )
            )
            {
                ToggleInventory();
            }

            // =====================================================
            // ROTAR
            // =====================================================

            if (
                IsOpen &&
                Keyboard.current.rKey.wasPressedThisFrame
            )
            {
                RotateSelected();
            }

            // =====================================================
            // MENSAJE DE INTERACCIÓN
            // =====================================================

            if (
                !IsOpen &&
                interactionText != null
            )
            {
                interactionText.text =
                    playerInventory == null
                        ? string.Empty
                        : playerInventory.InteractionMessage;
            }

            // =====================================================
            // RECICLADORA
            // =====================================================

            if (
                IsOpen &&
                activeRecyclingMachine != null
            )
            {
                RefreshRecipeDetails();
            }
        }

        // =========================================================
        // INVENTARIO
        // =========================================================

        public void ToggleInventory()
        {
            if (IsOpen)
                CloseInventory();
            else
                OpenInventory();
        }

        public void OpenInventory()
        {
            if (
                inventory == null ||
                casePanel == null
            )
            {
                return;
            }

            IsOpen = true;

            if (shadePanel != null)
                shadePanel.SetActive(true);

            casePanel.SetActive(true);

            // Aplicar distribución correspondiente.
            UpdateLayoutForCurrentMode();

            if (interactionText != null)
                interactionText.gameObject.SetActive(false);

            if (movement != null)
                movement.SetInputLocked(true);

            Cursor.lockState =
                CursorLockMode.None;

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

            // Siempre ocultar recetas al cerrar.
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

            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible = false;
        }

        // =========================================================
        // MODO VENTA
        // =========================================================

        public void OpenSellMode(
            SellTable sellTable)
        {
            if (sellTable == null)
                return;

            if (wallet == null)
                wallet = GetComponent<Wallet>();

            activeSellTable =
                sellTable;

            activeRecyclingMachine =
                null;

            selectedItem = null;

            // Venta = distribución normal.
            if (recipePanelObject != null)
                recipePanelObject.SetActive(false);

            OpenInventory();
        }

        // =========================================================
        // MODO RECICLAJE
        // =========================================================

        public void OpenRecycleMode(
            RecyclingMachine machine)
        {
            if (machine == null)
                return;

            activeSellTable =
                null;

            activeRecyclingMachine =
                machine;

            selectedItem =
                null;

            selectedRecipeIndex =
                machine.RecipeCount > 0
                    ? 0
                    : -1;

            craftAmount = 1;

            OpenInventory();
        }

        // =========================================================
        // DISTRIBUCIÓN DE LA INTERFAZ
        // =========================================================

        private void UpdateLayoutForCurrentMode()
        {
            if (grid == null)
                return;

            // =====================================================
            // INVENTARIO NORMAL / VENTA
            // =====================================================

            if (activeRecyclingMachine == null)
            {
                // -------------------------------------------------
                // Inventario centrado.
                //
                // Panel: 1120 px
                // Grid: aprox. 383 px
                // Detalles: 350 px
                //
                // El conjunto queda centrado.
                // -------------------------------------------------

                grid.anchoredPosition =
                    new Vector2(
                        145f,
                        -125f
                    );

                // -------------------------------------------------
                // Detalles a la derecha del inventario.
                // -------------------------------------------------

                if (detailsPanelRect != null)
                {
                    SetRect(
                        detailsPanelRect,
                        new Vector2(
                            -145f,
                            -125f
                        ),
                        new Vector2(
                            350f,
                            450f
                        ),
                        new Vector2(
                            1f,
                            1f
                        )
                    );
                }

                // No hay lista de recetas.
                if (recipePanelObject != null)
                    recipePanelObject.SetActive(false);

                return;
            }

            // =====================================================
            // RECICLADORA
            // =====================================================

            // -----------------------------------------------------
            // Inventario a la izquierda.
            // -----------------------------------------------------

            grid.anchoredPosition =
                new Vector2(
                    30f,
                    -125f
                );

            // -----------------------------------------------------
            // Panel de recetas.
            // -----------------------------------------------------

            if (recipePanelObject != null)
            {
                recipePanelObject.SetActive(true);
            }

            // -----------------------------------------------------
            // Detalles a la derecha.
            // -----------------------------------------------------

            if (detailsPanelRect != null)
            {
                SetRect(
                    detailsPanelRect,
                    new Vector2(
                        -30f,
                        -125f
                    ),
                    new Vector2(
                        350f,
                        450f
                    ),
                    new Vector2(
                        1f,
                        1f
                    )
                );
            }
        }

        // =========================================================
        // PANEL DE DETALLES
        // =========================================================

        private RectTransform detailsPanelRect;

        // =========================================================
        // BUILD UI
        // =========================================================

        private void BuildView()
        {
            if (runtimeCanvas != null)
                return;

            EnsureEventSystem();

            // =====================================================
            // CANVAS
            // =====================================================

            runtimeCanvas =
                new GameObject(
                    "Resident Inventory UI",
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster)
                );

            Canvas canvas =
                runtimeCanvas.GetComponent<Canvas>();

            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            canvas.sortingOrder =
                100;

            CanvasScaler scaler =
                runtimeCanvas.GetComponent<CanvasScaler>();

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            scaler.referenceResolution =
                new Vector2(
                    1920f,
                    1080f
                );

            scaler.matchWidthOrHeight =
                0.5f;

            // =====================================================
            // FONDO
            // =====================================================

            Image shade =
                CreateImage(
                    runtimeCanvas.transform,
                    "Dimmer",
                    new Color(
                        0.015f,
                        0.02f,
                        0.02f,
                        0.90f
                    )
                );

            shadePanel =
                shade.gameObject;

            Stretch(
                shade.rectTransform
            );

            // =====================================================
            // PANEL PRINCIPAL
            // =====================================================

            casePanel =
                CreateImage(
                    runtimeCanvas.transform,
                    "Inventory Case",
                    new Color(
                        0.07f,
                        0.09f,
                        0.085f,
                        0.985f
                    )
                ).gameObject;

            RectTransform panel =
                casePanel.GetComponent<RectTransform>();

            panel.anchorMin =
                panel.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f
                );

            panel.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );

            panel.sizeDelta =
                new Vector2(
                    1120f,
                    650f
                );

            // =====================================================
            // ACCENT
            // =====================================================

            Image accent =
                CreateImage(
                    casePanel.transform,
                    "Accent",
                    new Color(
                        0.38f,
                        0.78f,
                        0.42f,
                        1f
                    )
                );

            SetRect(
                accent.rectTransform,
                new Vector2(
                    26f,
                    -26f
                ),
                new Vector2(
                    1068f,
                    3f
                ),
                new Vector2(
                    0f,
                    1f
                )
            );

            // =====================================================
            // TÍTULO
            // =====================================================

            titleText =
                CreateText(
                    casePanel.transform,
                    "INVENTARIO",
                    30,
                    TextAnchor.UpperLeft,
                    new Color(
                        0.92f,
                        0.95f,
                        0.91f
                    )
                );

            SetRect(
                titleText.rectTransform,
                new Vector2(
                    28f,
                    -42f
                ),
                new Vector2(
                    1050f,
                    40f
                ),
                new Vector2(
                    0f,
                    1f
                )
            );

            // =====================================================
            // MODO
            // =====================================================

            modeText =
                CreateText(
                    casePanel.transform,
                    "MALETÍN DE RECUPERACIÓN",
                    13,
                    TextAnchor.UpperLeft,
                    new Color(
                        0.48f,
                        0.62f,
                        0.52f
                    )
                );

            SetRect(
                modeText.rectTransform,
                new Vector2(
                    30f,
                    -82f
                ),
                new Vector2(
                    1050f,
                    24f
                ),
                new Vector2(
                    0f,
                    1f
                )
            );

            // =====================================================
            // INVENTARIO
            // =====================================================

            grid =
                new GameObject(
                    "Inventory Grid",
                    typeof(RectTransform)
                ).GetComponent<RectTransform>();

            grid.SetParent(
                casePanel.transform,
                false
            );

            grid.anchorMin =
                grid.anchorMax =
                new Vector2(
                    0f,
                    1f
                );

            grid.pivot =
                new Vector2(
                    0f,
                    1f
                );

            // Posición inicial.
            // UpdateLayoutForCurrentMode() la cambiará
            // según el modo.
            grid.anchoredPosition =
                new Vector2(
                    145f,
                    -125f
                );

            // =====================================================
            // PANEL DE RECETAS
            // =====================================================

            Image recipePanel =
                CreateImage(
                    casePanel.transform,
                    "Recipe Panel",
                    new Color(
                        0.11f,
                        0.14f,
                        0.13f,
                        1f
                    )
                );

            recipePanelObject =
                recipePanel.gameObject;

            // 20 px más a la derecha.
            SetRect(
                recipePanel.rectTransform,
                new Vector2(
                    455f,
                    -125f
                ),
                new Vector2(
                    250f,
                    450f
                ),
                new Vector2(
                    0f,
                    1f
                )
            );

            Text recipeTitle =
                CreateText(
                    recipePanel.transform,
                    "RECETAS",
                    14,
                    TextAnchor.UpperLeft,
                    new Color(
                        0.4f,
                        0.78f,
                        0.45f
                    )
                );

            SetRect(
                recipeTitle.rectTransform,
                new Vector2(
                    16f,
                    -20f
                ),
                new Vector2(
                    215f,
                    28f
                ),
                new Vector2(
                    0f,
                    1f
                )
            );

            // =====================================================
            // VIEWPORT
            // =====================================================

            GameObject viewportObject =
                new GameObject(
                    "Recipe Viewport",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(RectMask2D)
                );

            viewportObject.transform.SetParent(
                recipePanel.transform,
                false
            );

            RectTransform viewport =
                viewportObject.GetComponent<RectTransform>();

            SetRect(
                viewport,
                new Vector2(
                    10f,
                    -50f
                ),
                new Vector2(
                    230f,
                    385f
                ),
                new Vector2(
                    0f,
                    1f
                )
            );

            viewport.GetComponent<Image>().color =
                new Color(
                    0.06f,
                    0.08f,
                    0.075f,
                    1f
                );

            // =====================================================
            // CONTENT
            // =====================================================

            GameObject content =
                new GameObject(
                    "Recipe Content",
                    typeof(RectTransform),
                    typeof(VerticalLayoutGroup),
                    typeof(ContentSizeFitter)
                );

            content.transform.SetParent(
                viewport.transform,
                false
            );

            recipeContent =
                content.GetComponent<RectTransform>();

            recipeContent.anchorMin =
                new Vector2(
                    0f,
                    1f
                );

            recipeContent.anchorMax =
                new Vector2(
                    1f,
                    1f
                );

            recipeContent.pivot =
                new Vector2(
                    0.5f,
                    1f
                );

            recipeContent.anchoredPosition =
                Vector2.zero;

            recipeContent.sizeDelta =
                new Vector2(
                    0f,
                    0f
                );

            VerticalLayoutGroup layout =
                content.GetComponent<VerticalLayoutGroup>();

            layout.spacing =
                5f;

            layout.padding =
                new RectOffset(
                    5,
                    5,
                    5,
                    5
                );

            layout.childForceExpandWidth =
                true;

            layout.childForceExpandHeight =
                false;

            ContentSizeFitter fitter =
                content.GetComponent<ContentSizeFitter>();

            fitter.verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            // =====================================================
            // SCROLL
            // =====================================================

            recipeScroll =
                recipePanel.gameObject.AddComponent<ScrollRect>();

            recipeScroll.viewport =
                viewport;

            recipeScroll.content =
                recipeContent;

            recipeScroll.horizontal =
                false;

            recipeScroll.vertical =
                true;

            recipeScroll.movementType =
                ScrollRect.MovementType.Clamped;

            // Ocultar por defecto.
            recipePanelObject.SetActive(false);

            // =====================================================
            // PANEL DETALLES
            // =====================================================

            Image info =
                CreateImage(
                    casePanel.transform,
                    "Recipe Details",
                    new Color(
                        0.11f,
                        0.14f,
                        0.13f,
                        1f
                    )
                );

            detailsPanelRect =
                info.rectTransform;

            // Posición inicial para inventario normal.
            SetRect(
                detailsPanelRect,
                new Vector2(
                    -145f,
                    -125f
                ),
                new Vector2(
                    350f,
                    450f
                ),
                new Vector2(
                    1f,
                    1f
                )
            );

            infoTitle =
                CreateText(
                    info.transform,
                    "OBJETO SELECCIONADO",
                    15,
                    TextAnchor.UpperLeft,
                    new Color(
                        0.4f,
                        0.78f,
                        0.45f
                    )
                );

            SetRect(
                infoTitle.rectTransform,
                new Vector2(
                    20f,
                    -22f
                ),
                new Vector2(
                    310f,
                    30f
                ),
                new Vector2(
                    0f,
                    1f
                )
            );

            detailsText =
                CreateText(
                    info.transform,
                    "Selecciona un objeto.",
                    15,
                    TextAnchor.UpperLeft,
                    Color.white
                );

            detailsText.verticalOverflow =
                VerticalWrapMode.Overflow;

            detailsText.horizontalOverflow =
                HorizontalWrapMode.Wrap;

            SetRect(
                detailsText.rectTransform,
                new Vector2(
                    20f,
                    -60f
                ),
                new Vector2(
                    310f,
                    280f
                ),
                new Vector2(
                    0f,
                    1f
                )
            );

            // =====================================================
            // CANTIDAD -
            // =====================================================

            minusButton =
                CreateButton(
                    info.transform,
                    "-",
                    DecreaseCraftAmount
                );

            SetRect(
                minusButton.GetComponent<RectTransform>(),
                new Vector2(
                    20f,
                    83f
                ),
                new Vector2(
                    45f,
                    40f
                ),
                new Vector2(
                    0f,
                    0f
                )
            );

            // =====================================================
            // CANTIDAD
            // =====================================================

            Text quantityText =
                CreateText(
                    info.transform,
                    "1",
                    18,
                    TextAnchor.MiddleCenter,
                    Color.white
                );

            quantityText.name =
                "Craft Quantity";

            SetRect(
                quantityText.rectTransform,
                new Vector2(
                    72f,
                    83f
                ),
                new Vector2(
                    75f,
                    40f
                ),
                new Vector2(
                    0f,
                    0f
                )
            );

            // =====================================================
            // CANTIDAD +
            // =====================================================

            plusButton =
                CreateButton(
                    info.transform,
                    "+",
                    IncreaseCraftAmount
                );

            SetRect(
                plusButton.GetComponent<RectTransform>(),
                new Vector2(
                    155f,
                    83f
                ),
                new Vector2(
                    45f,
                    40f
                ),
                new Vector2(
                    0f,
                    0f
                )
            );

            // =====================================================
            // BOTÓN ACCIÓN
            // =====================================================

            actionButton =
                CreateButton(
                    info.transform,
                    "ACCIÓN",
                    ActionSelected
                );

            SetRect(
                actionButton.GetComponent<RectTransform>(),
                new Vector2(
                    20f,
                    28f
                ),
                new Vector2(
                    310f,
                    42f
                ),
                new Vector2(
                    0f,
                    0f
                )
            );

            // =====================================================
            // TEXTO INTERACCIÓN
            // =====================================================

            interactionText =
                CreateText(
                    runtimeCanvas.transform,
                    string.Empty,
                    19,
                    TextAnchor.MiddleCenter,
                    new Color(
                        0.88f,
                        1f,
                        0.89f
                    )
                );

            interactionText.fontStyle =
                FontStyle.Bold;

            interactionText.rectTransform.anchorMin =
                interactionText.rectTransform.anchorMax =
                new Vector2(
                    0.5f,
                    0f
                );

            interactionText.rectTransform.pivot =
                new Vector2(
                    0.5f,
                    0f
                );

            interactionText.rectTransform.anchoredPosition =
                new Vector2(
                    0f,
                    90f
                );

            interactionText.rectTransform.sizeDelta =
                new Vector2(
                    700f,
                    34f
                );
        }

        // =========================================================
        // REFRESH INVENTARIO
        // =========================================================

        private void RefreshInventory()
        {
            if (
                !IsOpen ||
                inventory == null ||
                grid == null
            )
            {
                return;
            }

            // Reaplicar la distribución por seguridad.
            UpdateLayoutForCurrentMode();

            // =====================================================
            // VALIDAR SELECCIÓN
            // =====================================================

            bool selectedExists =
                false;

            foreach (
                ItemInstance item
                in inventory.Items)
            {
                if (item == selectedItem)
                {
                    selectedExists = true;
                    break;
                }
            }

            if (!selectedExists)
                selectedItem = null;

            // =====================================================
            // LIMPIAR GRID
            // =====================================================

            ClearChildren(
                grid
            );

            grid.sizeDelta =
                new Vector2(
                    inventory.Width *
                        (CellSize + Gap) -
                        Gap,

                    inventory.Height *
                        (CellSize + Gap) -
                        Gap
                );

            // =====================================================
            // CASILLAS
            // =====================================================

            for (
                int y = 0;
                y < inventory.Height;
                y++)
            {
                for (
                    int x = 0;
                    x < inventory.Width;
                    x++)
                {
                    Vector2Int cell =
                        new Vector2Int(
                            x,
                            y
                        );

                    Button slot =
                        CreateButton(
                            grid,
                            string.Empty,
                            delegate
                            {
                                ClickCell(cell);
                            }
                        );

                    slot.GetComponent<Image>().color =
                        inventory.IsCellOccupied(cell)
                            ? new Color(
                                0.10f,
                                0.13f,
                                0.12f,
                                1f
                            )
                            : new Color(
                                0.16f,
                                0.19f,
                                0.18f,
                                1f
                            );

                    SetRect(
                        slot.GetComponent<RectTransform>(),
                        new Vector2(
                            x *
                            (CellSize + Gap),

                            -y *
                            (CellSize + Gap)
                        ),
                        new Vector2(
                            CellSize,
                            CellSize
                        ),
                        new Vector2(
                            0f,
                            1f
                        )
                    );
                }
            }

            // =====================================================
            // ITEMS
            // =====================================================

            foreach (
                ItemInstance item
                in inventory.Items)
            {
                CreateItemVisual(item);
            }

            // =====================================================
            // CABECERA
            // =====================================================

            int balance =
                wallet == null
                    ? 0
                    : wallet.Balance;

            titleText.text =
                "INVENTARIO  " +
                "<color=#69c475>" +
                inventory.UsedCells +
                "/" +
                (
                    inventory.Width *
                    inventory.Height
                ) +
                "</color>" +
                "    " +
                "<color=#f2d66a>" +
                "¢ " +
                balance +
                "</color>";

            // =====================================================
            // MODO
            // =====================================================

            if (
                activeRecyclingMachine != null
            )
            {
                modeText.text =
                    activeRecyclingMachine
                        .StationName
                        .ToUpperInvariant() +
                    "  //  SELECCIONA UNA RECETA";

                if (recipePanelObject != null)
                    recipePanelObject.SetActive(true);

                RefreshRecipeList();
                RefreshRecipeDetails();

                return;
            }

            if (
                activeSellTable != null
            )
            {
                modeText.text =
                    activeSellTable
                        .StationName
                        .ToUpperInvariant() +
                    "  //  SELECCIONA Y VENDE UN LOTE";

                if (recipePanelObject != null)
                    recipePanelObject.SetActive(false);

                RefreshSellUI();

                return;
            }

            // =====================================================
            // INVENTARIO NORMAL
            // =====================================================

            modeText.text =
                "MALETÍN DE RECUPERACIÓN  // TAB PARA CERRAR";

            if (recipePanelObject != null)
                recipePanelObject.SetActive(false);

            if (actionButton != null)
                actionButton.gameObject.SetActive(false);

            RefreshDetails();
        }

        // =========================================================
        // LISTA DE RECETAS
        // =========================================================

        private void RefreshRecipeList()
        {
            if (
                recipeContent == null ||
                activeRecyclingMachine == null
            )
            {
                return;
            }

            ClearChildren(
                recipeContent
            );

            int count =
                activeRecyclingMachine.RecipeCount;

            if (count <= 0)
            {
                Text empty =
                    CreateText(
                        recipeContent,
                        "No hay recetas.",
                        14,
                        TextAnchor.MiddleCenter,
                        Color.white
                    );

                LayoutElement element =
                    empty.gameObject
                        .AddComponent<LayoutElement>();

                element.preferredHeight =
                    40f;

                return;
            }

            if (
                selectedRecipeIndex < 0 ||
                selectedRecipeIndex >= count
            )
            {
                selectedRecipeIndex =
                    0;
            }

            for (
                int i = 0;
                i < count;
                i++)
            {
                RecyclingMachine.CraftingRecipe recipe =
                    activeRecyclingMachine.GetRecipe(i);

                if (recipe == null)
                    continue;

                int recipeIndex =
                    i;

                Button button =
                    CreateButton(
                        recipeContent,
                        recipe.recipeName,
                        delegate
                        {
                            SelectRecipe(
                                recipeIndex
                            );
                        }
                    );

                LayoutElement element =
                    button.gameObject
                        .AddComponent<LayoutElement>();

                element.preferredHeight =
                    48f;

                Image image =
                    button.GetComponent<Image>();

                image.color =
                    recipeIndex ==
                    selectedRecipeIndex
                        ? new Color(
                            0.25f,
                            0.45f,
                            0.28f,
                            1f
                        )
                        : new Color(
                            0.16f,
                            0.19f,
                            0.18f,
                            1f
                        );
            }
        }

        private void SelectRecipe(
            int index)
        {
            if (
                activeRecyclingMachine == null
            )
            {
                return;
            }

            if (
                index < 0 ||
                index >=
                activeRecyclingMachine.RecipeCount
            )
            {
                return;
            }

            selectedRecipeIndex =
                index;

            craftAmount =
                1;

            RefreshInventory();
        }

        // =========================================================
        // DETALLES RECETA
        // =========================================================

        private void RefreshRecipeDetails()
        {
            if (
                activeRecyclingMachine == null ||
                detailsText == null
            )
            {
                return;
            }

            RecyclingMachine.CraftingRecipe recipe =
                activeRecyclingMachine.GetRecipe(
                    selectedRecipeIndex
                );

            if (recipe == null)
            {
                infoTitle.text =
                    "RECETA";

                detailsText.text =
                    "Selecciona una receta.";

                if (actionButton != null)
                    actionButton.gameObject
                        .SetActive(false);

                UpdateQuantityText();

                return;
            }

            infoTitle.text =
                recipe.recipeName
                    .ToUpperInvariant();

            string text =
                string.Empty;

            // =====================================================
            // DESCRIPCIÓN
            // =====================================================

            if (
                !string.IsNullOrWhiteSpace(
                    recipe.description
                )
            )
            {
                text +=
                    recipe.description +
                    "\n\n";
            }

            // =====================================================
            // INGREDIENTES
            // =====================================================

            text +=
                "<b>INGREDIENTES</b>\n";

            for (
                int i = 0;
                i < recipe.ingredients.Count;
                i++)
            {
                RecyclingMachine.RecipePart part =
                    recipe.ingredients[i];

                if (
                    part == null ||
                    part.item == null
                )
                {
                    continue;
                }

                int required =
                    part.amount *
                    craftAmount;

                int available =
                    activeRecyclingMachine
                        .GetTotalAmountById(
                            inventory,
                            part.item.ItemID
                        );

                bool enough =
                    available >= required;

                text +=
                    "• " +
                    required +
                    " × " +
                    part.item.ItemName +
                    "  (" +
                    available +
                    ")";

                text +=
                    enough
                        ? " <color=#69c475>✓</color>"
                        : " <color=#cc5555>✕</color>";

                text +=
                    "\n";
            }

            // =====================================================
            // RESULTADOS
            // =====================================================

            text +=
                "\n<b>RESULTADOS</b>\n";

            for (
                int i = 0;
                i < recipe.outputs.Count;
                i++)
            {
                RecyclingMachine.RecipePart part =
                    recipe.outputs[i];

                if (
                    part == null ||
                    part.item == null
                )
                {
                    continue;
                }

                int outputAmount =
                    part.amount *
                    craftAmount;

                text +=
                    "• " +
                    outputAmount +
                    " × " +
                    part.item.ItemName +
                    "\n";
            }

            // =====================================================
            // PROGRESO
            // =====================================================

            if (
                activeRecyclingMachine.IsProcessing
            )
            {
                text +=
                    "\n" +
                    activeRecyclingMachine
                        .StatusMessage;
            }

            detailsText.text =
                text;

            UpdateQuantityText();

            RefreshCraftButton(
                recipe
            );
        }

        // =========================================================
        // BOTÓN FABRICAR
        // =========================================================

        private void RefreshCraftButton(
            RecyclingMachine.CraftingRecipe recipe)
        {
            if (actionButton == null)
                return;

            actionButton.gameObject.SetActive(
                true
            );

            Text buttonText =
                actionButton
                    .GetComponentInChildren<Text>();

            // -----------------------------------------------------
            // PROCESANDO
            // -----------------------------------------------------

            if (
                activeRecyclingMachine.IsProcessing
            )
            {
                actionButton.interactable =
                    false;

                if (buttonText != null)
                {
                    int percent =
                        Mathf.RoundToInt(
                            activeRecyclingMachine
                                .Progress *
                            100f
                        );

                    buttonText.text =
                        percent +
                        "%";
                }

                return;
            }

            // -----------------------------------------------------
            // DISPONIBILIDAD
            // -----------------------------------------------------

            bool canCraft =
                activeRecyclingMachine.CanCraft(
                    inventory,
                    recipe,
                    craftAmount
                );

            actionButton.interactable =
                canCraft;

            if (buttonText != null)
                buttonText.text =
                    "FABRICAR";
        }

        // =========================================================
        // CANTIDAD
        // =========================================================

        private void IncreaseCraftAmount()
        {
            if (
                activeRecyclingMachine == null ||
                activeRecyclingMachine.IsProcessing
            )
            {
                return;
            }

            craftAmount =
                Mathf.Clamp(
                    craftAmount + 1,
                    1,
                    99
                );

            RefreshRecipeDetails();
        }

        private void DecreaseCraftAmount()
        {
            if (
                activeRecyclingMachine == null ||
                activeRecyclingMachine.IsProcessing
            )
            {
                return;
            }

            craftAmount =
                Mathf.Clamp(
                    craftAmount - 1,
                    1,
                    99
                );

            RefreshRecipeDetails();
        }

        private void UpdateQuantityText()
        {
            if (minusButton == null)
                return;

            Transform parent =
                minusButton.transform.parent;

            Transform textTransform =
                parent.Find(
                    "Craft Quantity"
                );

            if (textTransform == null)
                return;

            Text quantityText =
                textTransform.GetComponent<Text>();

            if (quantityText != null)
                quantityText.text =
                    craftAmount.ToString();
        }

        // =========================================================
        // ACCIÓN
        // =========================================================

        private void ActionSelected()
        {
            if (activeSellTable != null)
            {
                SellSelected();
                return;
            }

            if (
                activeRecyclingMachine != null
            )
            {
                CraftSelected();
            }
        }

        // =========================================================
        // FABRICAR
        // =========================================================

        private void CraftSelected()
        {
            if (
                activeRecyclingMachine == null
            )
            {
                return;
            }

            if (selectedRecipeIndex < 0)
                return;

            activeRecyclingMachine.TryStartCraft(
                inventory,
                selectedRecipeIndex,
                craftAmount
            );

            RefreshRecipeDetails();
        }

        // =========================================================
        // VENTA
        // =========================================================

        private void RefreshSellUI()
        {
            if (actionButton == null)
                return;

            actionButton.gameObject.SetActive(
                selectedItem != null
            );

            actionButton.interactable =
                selectedItem != null;

            Text buttonText =
                actionButton
                    .GetComponentInChildren<Text>();

            if (buttonText != null)
                buttonText.text =
                    "VENDER LOTE";

            RefreshDetails();
        }

        private void SellSelected()
        {
            if (
                activeSellTable == null ||
                selectedItem == null ||
                wallet == null
            )
            {
                return;
            }

            int earned;

            bool sold =
                activeSellTable.TrySell(
                    inventory,
                    wallet,
                    selectedItem,
                    out earned
                );

            if (sold)
            {
                selectedItem =
                    null;

                RefreshInventory();
            }
        }

        // =========================================================
        // CELDA INVENTARIO
        // =========================================================

        private void ClickCell(
            Vector2Int cell)
        {
            if (inventory == null)
                return;

            ItemInstance clicked =
                inventory.GetItemAtCell(cell);

            if (selectedItem == null)
            {
                selectedItem =
                    clicked;
            }
            else if (
                clicked == selectedItem
            )
            {
                selectedItem =
                    null;
            }
            else if (
                clicked != null
            )
            {
                selectedItem =
                    clicked;
            }
            else
            {
                inventory.TryMoveItem(
                    selectedItem,
                    cell,
                    selectedItem.Rotated
                );
            }

            RefreshInventory();
        }

        // =========================================================
        // ROTAR
        // =========================================================

        private void RotateSelected()
        {
            if (
                selectedItem == null ||
                inventory == null
            )
            {
                return;
            }

            inventory.TryRotateItem(
                selectedItem
            );

            RefreshInventory();
        }

        // =========================================================
        // DETALLES OBJETO
        // =========================================================

        private void RefreshDetails()
        {
            if (detailsText == null)
                return;

            if (
                selectedItem == null ||
                selectedItem.Definition == null
            )
            {
                detailsText.text =
                    "Selecciona un objeto.";

                return;
            }

            ItemDefinition item =
                selectedItem.Definition;

            string description =
                string.IsNullOrWhiteSpace(
                    item.Description
                )
                    ? "Sin descripción."
                    : item.Description;

            detailsText.text =
                "<b>" +
                item.ItemName +
                "</b>\n\n" +
                item.Category
                    .ToString()
                    .ToUpperInvariant() +
                "\nID  " +
                item.ItemID +
                "\n\n" +
                description +
                "\n\nTAMAÑO  " +
                selectedItem.Size.x +
                " × " +
                selectedItem.Size.y +
                (
                    item.Stackable
                        ? "\nCANTIDAD  " +
                          selectedItem.Amount +
                          "/" +
                          item.MaxStack
                        : string.Empty
                );
        }

        // =========================================================
        // VISUAL ITEM
        // =========================================================

        private void CreateItemVisual(
            ItemInstance item)
        {
            if (
                item == null ||
                item.Definition == null
            )
            {
                return;
            }

            Image visual =
                CreateImage(
                    grid,
                    item.Definition.ItemName,
                    ItemColor(
                        item.Definition
                    )
                );

            visual.raycastTarget =
                false;

            RectTransform rect =
                visual.rectTransform;

            rect.SetAsLastSibling();

            SetRect(
                rect,
                new Vector2(
                    item.Position.x *
                    (CellSize + Gap),

                    -item.Position.y *
                    (CellSize + Gap)
                ),
                new Vector2(
                    item.Size.x *
                    CellSize +
                    (item.Size.x - 1) *
                    Gap,

                    item.Size.y *
                    CellSize +
                    (item.Size.y - 1) *
                    Gap
                ),
                new Vector2(
                    0f,
                    1f
                )
            );

            // =====================================================
            // ICONO
            // =====================================================

            if (item.Definition.Icon != null)
            {
                Image icon =
                    CreateImage(
                        visual.transform,
                        "Icon",
                        Color.white
                    );

                icon.sprite =
                    item.Definition.Icon;

                icon.preserveAspect =
                    true;

                icon.raycastTarget =
                    false;

                Stretch(
                    icon.rectTransform,
                    10f
                );
            }

            // =====================================================
            // OUTLINE
            // =====================================================

            Outline outline =
                visual.gameObject
                    .AddComponent<Outline>();

            outline.effectColor =
                item == selectedItem
                    ? new Color(
                        0.8f,
                        1f,
                        0.56f,
                        1f
                    )
                    : new Color(
                        0.02f,
                        0.04f,
                        0.03f,
                        1f
                    );

            outline.effectDistance =
                item == selectedItem
                    ? new Vector2(
                        3f,
                        -3f
                    )
                    : new Vector2(
                        1f,
                        -1f
                    );

            // =====================================================
            // NOMBRE
            // =====================================================

            Text label =
                CreateText(
                    visual.transform,
                    item.Definition
                        .ItemName
                        .ToUpperInvariant(),
                    item.Size.x >= 2
                        ? 12
                        : 10,
                    TextAnchor.LowerCenter,
                    Color.white
                );

            label.raycastTarget =
                false;

            SetRect(
                label.rectTransform,
                new Vector2(
                    4f,
                    4f
                ),
                new Vector2(
                    Mathf.Max(
                        30f,
                        rect.rect.width - 8f
                    ),
                    26f
                ),
                new Vector2(
                    0.5f,
                    0f
                )
            );

            // =====================================================
            // CANTIDAD
            // =====================================================

            if (item.Amount > 1)
            {
                Text count =
                    CreateText(
                        visual.transform,
                        "x" +
                        item.Amount,
                        14,
                        TextAnchor.LowerRight,
                        Color.white
                    );

                count.raycastTarget =
                    false;

                SetRect(
                    count.rectTransform,
                    new Vector2(
                        -5f,
                        5f
                    ),
                    new Vector2(
                        60f,
                        22f
                    ),
                    new Vector2(
                        1f,
                        0f
                    )
                );
            }
        }

        // =========================================================
        // UI HELPERS
        // =========================================================

        private static Image CreateImage(
            Transform parent,
            string objectName,
            Color color)
        {
            GameObject go =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image)
                );

            go.transform.SetParent(
                parent,
                false
            );

            Image image =
                go.GetComponent<Image>();

            image.color =
                color;

            return image;
        }

        private static Text CreateText(
            Transform parent,
            string content,
            int size,
            TextAnchor alignment,
            Color color)
        {
            GameObject go =
                new GameObject(
                    "Text",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text)
                );

            go.transform.SetParent(
                parent,
                false
            );

            Text text =
                go.GetComponent<Text>();

            text.font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf"
                );

            text.text =
                content;

            text.fontSize =
                size;

            text.alignment =
                alignment;

            text.color =
                color;

            text.supportRichText =
                true;

            return text;
        }

        private static Button CreateButton(
            Transform parent,
            string label,
            UnityEngine.Events.UnityAction action)
        {
            Image image =
                CreateImage(
                    parent,
                    "Button",
                    new Color(
                        0.16f,
                        0.19f,
                        0.18f,
                        1f
                    )
                );

            Button button =
                image.gameObject
                    .AddComponent<Button>();

            ColorBlock colors =
                button.colors;

            colors.normalColor =
                Color.white;

            colors.highlightedColor =
                new Color(
                    0.72f,
                    0.9f,
                    0.7f,
                    1f
                );

            colors.pressedColor =
                new Color(
                    0.43f,
                    0.66f,
                    0.42f,
                    1f
                );

            button.colors =
                colors;

            button.onClick.AddListener(
                action
            );

            if (!string.IsNullOrEmpty(label))
            {
                Text text =
                    CreateText(
                        button.transform,
                        label,
                        15,
                        TextAnchor.MiddleCenter,
                        Color.white
                    );

                Stretch(
                    text.rectTransform,
                    4f
                );

                text.raycastTarget =
                    false;
            }

            return button;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 position,
            Vector2 size,
            Vector2 anchor)
        {
            rect.anchorMin =
                rect.anchorMax =
                anchor;

            rect.pivot =
                anchor;

            rect.anchoredPosition =
                position;

            rect.sizeDelta =
                size;
        }

        private static void Stretch(
            RectTransform rect,
            float padding = 0f)
        {
            rect.anchorMin =
                Vector2.zero;

            rect.anchorMax =
                Vector2.one;

            rect.offsetMin =
                new Vector2(
                    padding,
                    padding
                );

            rect.offsetMax =
                new Vector2(
                    -padding,
                    -padding
                );
        }

        private static void ClearChildren(
            Transform parent)
        {
            for (
                int i =
                    parent.childCount - 1;
                i >= 0;
                i--
            )
            {
                Destroy(
                    parent.GetChild(i).gameObject
                );
            }
        }

        private static Color ItemColor(
            ItemDefinition item)
        {
            switch (item.Category)
            {
                case ItemCategory.Wood:
                    return new Color(
                        0.44f,
                        0.25f,
                        0.12f,
                        1f
                    );

                case ItemCategory.Metal:
                    return new Color(
                        0.28f,
                        0.38f,
                        0.43f,
                        1f
                    );

                case ItemCategory.Paper:
                    return new Color(
                        0.55f,
                        0.49f,
                        0.31f,
                        1f
                    );

                case ItemCategory.Plastic:
                    return new Color(
                        0.18f,
                        0.43f,
                        0.49f,
                        1f
                    );

                case ItemCategory.Porcelain:
                    return new Color(
                        0.70f,
                        0.70f,
                        0.72f,
                        1f
                    );

                case ItemCategory.Glass:
                    return new Color(
                        0.16f,
                        0.53f,
                        0.48f,
                        1f
                    );

                case ItemCategory.Organic:
                    return new Color(
                        0.30f,
                        0.48f,
                        0.20f,
                        1f
                    );

                case ItemCategory.Tool:
                    return new Color(
                        0.54f,
                        0.26f,
                        0.16f,
                        1f
                    );

                case ItemCategory.KeyItem:
                    return new Color(
                        0.55f,
                        0.47f,
                        0.12f,
                        1f
                    );

                case ItemCategory.Consumable:
                    return new Color(
                        0.42f,
                        0.24f,
                        0.48f,
                        1f
                    );

                case ItemCategory.Equipment:
                    return new Color(
                        0.24f,
                        0.32f,
                        0.48f,
                        1f
                    );

                case ItemCategory.Quest:
                    return new Color(
                        0.48f,
                        0.43f,
                        0.20f,
                        1f
                    );

                default:
                    return item.FallbackColor;
            }
        }

        // =========================================================
        // EVENT SYSTEM
        // =========================================================

        private static void EnsureEventSystem()
        {
            if (
                Object.FindFirstObjectByType<EventSystem>() !=
                null
            )
            {
                return;
            }

            GameObject eventSystem =
                new GameObject(
                    "EventSystem",
                    typeof(EventSystem),
                    typeof(InputSystemUIInputModule)
                );

            eventSystem
                .GetComponent<EventSystem>()
                .sendNavigationEvents =
                true;
        }

        // =========================================================
        // REFRESH ESTACIÓN
        // =========================================================

        public void RefreshStationUI()
        {
            if (!IsOpen)
                return;

            if (
                activeRecyclingMachine != null
            )
            {
                RefreshRecipeDetails();
            }
            else if (
                activeSellTable != null
            )
            {
                RefreshSellUI();
            }
        }
    }
}