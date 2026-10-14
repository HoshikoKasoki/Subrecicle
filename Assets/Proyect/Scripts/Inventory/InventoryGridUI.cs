using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem
{
    public class InventoryGridUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Inventory inventory;

        [SerializeField] private RectTransform slotsContainer;
        [SerializeField] private RectTransform itemsContainer;

        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private GameObject itemUIPrefab;

        [Header("Grid")]
        [SerializeField] private float cellSize = 70f;
        [SerializeField] private float spacing = 4f;

        private GridLayoutGroup gridLayout;

        private void Start()
        {
            if (inventory == null)
            {
                Debug.LogError(
                    "InventoryGridUI: No se asignó Inventory."
                );

                return;
            }

            CreateSlots();
            RefreshItems();
        }

        private void OnEnable()
        {
            if (inventory != null)
                inventory.OnInventoryChanged += RefreshItems;
        }

        private void OnDisable()
        {
            if (inventory != null)
                inventory.OnInventoryChanged -= RefreshItems;
        }

        // =========================================================
        // CREAR GRID
        // =========================================================

        private void CreateSlots()
        {
            if (slotsContainer == null)
            {
                Debug.LogError(
                    "InventoryGridUI: Slots Container está vacío."
                );

                return;
            }

            gridLayout =
                slotsContainer.GetComponent<GridLayoutGroup>();

            if (gridLayout == null)
            {
                gridLayout =
                    slotsContainer.gameObject
                    .AddComponent<GridLayoutGroup>();
            }

            gridLayout.cellSize =
                new Vector2(cellSize, cellSize);

            gridLayout.spacing =
                new Vector2(spacing, spacing);

            gridLayout.constraint =
                GridLayoutGroup.Constraint.FixedColumnCount;

            gridLayout.constraintCount =
                inventory.Width;

            gridLayout.childAlignment =
                TextAnchor.UpperLeft;

            // Limpiar slots anteriores.
            for (int i = slotsContainer.childCount - 1;
                 i >= 0;
                 i--)
            {
                Destroy(
                    slotsContainer.GetChild(i).gameObject
                );
            }

            int totalSlots =
                inventory.Width *
                inventory.Height;

            for (int i = 0; i < totalSlots; i++)
            {
                Instantiate(
                    slotPrefab,
                    slotsContainer
                );
            }
        }

        // =========================================================
        // ACTUALIZAR OBJETOS
        // =========================================================

        public void RefreshItems()
        {
            if (inventory == null)
                return;

            if (itemsContainer == null)
                return;

            if (itemUIPrefab == null)
            {
                Debug.LogError(
                    "InventoryGridUI: Item UI Prefab está vacío."
                );

                return;
            }

            // Eliminar objetos visuales anteriores.
            for (int i = itemsContainer.childCount - 1;
                 i >= 0;
                 i--)
            {
                Destroy(
                    itemsContainer.GetChild(i).gameObject
                );
            }

            // Crear los objetos actuales.
            foreach (ItemInstance item in inventory.Items)
            {
                CreateItemVisual(item);
            }
        }

        private void CreateItemVisual(ItemInstance item)
        {
            if (item == null)
                return;

            if (item.Definition == null)
                return;

            GameObject itemObject =
                Instantiate(
                    itemUIPrefab,
                    itemsContainer
                );

            RectTransform rect =
                itemObject.GetComponent<RectTransform>();

            if (rect == null)
            {
                Debug.LogError(
                    "InventoryItem no tiene RectTransform."
                );

                return;
            }

            // -----------------------------------------
            // ANCHORS
            // -----------------------------------------

            rect.anchorMin =
                new Vector2(0f, 1f);

            rect.anchorMax =
                new Vector2(0f, 1f);

            rect.pivot =
                new Vector2(0f, 1f);

            // -----------------------------------------
            // POSICIÓN
            // -----------------------------------------

            rect.anchoredPosition =
                GetItemPosition(
                    item.Position
                );

            // -----------------------------------------
            // TAMAÑO
            // -----------------------------------------

            rect.sizeDelta =
                GetItemSize(
                    item.Size
                );

            // -----------------------------------------
            // SCRIPT
            // -----------------------------------------

            InventoryItemUI itemUI =
                itemObject.GetComponent<InventoryItemUI>();

            if (itemUI != null)
            {
                itemUI.Setup(item);
            }
            else
            {
                Debug.LogError(
                    "El InventoryItem Prefab no tiene " +
                    "InventoryItemUI."
                );
            }
        }

        // =========================================================
        // POSICIÓN
        // =========================================================

        public Vector2 GetItemPosition(
            Vector2Int position)
        {
            float x =
                position.x *
                (cellSize + spacing);

            float y =
                -position.y *
                (cellSize + spacing);

            return new Vector2(x, y);
        }

        // =========================================================
        // TAMAÑO
        // =========================================================

        public Vector2 GetItemSize(
            Vector2Int size)
        {
            float width =
                size.x * cellSize +
                (size.x - 1) * spacing;

            float height =
                size.y * cellSize +
                (size.y - 1) * spacing;

            return new Vector2(
                width,
                height
            );
        }
    }
}