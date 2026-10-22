using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem
{
    /// <summary>
    /// Actualiza la interfaz del Hotbar en la escena basándose en ToolHotbar.
    /// </summary>
    public class ToolHotbarUI : MonoBehaviour
    {
        [Header("Scene UI")]
        [SerializeField] private GameObject hotbarRoot;
        [SerializeField] private Image[] slotBackgrounds;
        [SerializeField] private Image[] slotBorders;

        [Header("Colors Base")]
        [SerializeField] private Color slotColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        [SerializeField] private Color normalBorderColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color selectedBorderColor = new Color(1f, 0.75f, 0.15f, 1f);

        private ToolHotbar hotbar;

        private void Awake()
        {
            hotbar = GetComponent<ToolHotbar>();
        }

        private void OnEnable()
        {
            if (hotbar != null)
                hotbar.OnHotbarChanged += RefreshVisuals;
        }

        private void Start()
        {
            RefreshVisuals();
        }

        private void OnDisable()
        {
            if (hotbar != null)
                hotbar.OnHotbarChanged -= RefreshVisuals;
        }

        public void RefreshVisuals()
        {
            if (hotbar == null || slotBackgrounds == null || slotBorders == null)
                return;

            int count = Mathf.Min(ToolHotbar.SlotCount, Mathf.Min(slotBackgrounds.Length, slotBorders.Length));
            for (int i = 0; i < count; i++)
            {
                // Borde de selección del Hotbar (Slot actual 1-8)
                bool selected = i == hotbar.SelectedSlot;
                if (slotBorders[i] != null)
                    slotBorders[i].color = selected ? selectedBorderColor : normalBorderColor;

                if (slotBackgrounds[i] != null)
                {
                    string toolId = hotbar.GetTool(i);
                    if (!string.IsNullOrEmpty(toolId))
                    {
                        ItemDefinition def = FindItemDefinitionSafe(toolId);
                        if (def != null)
                        {
                            slotBackgrounds[i].sprite = def.Icon;
                            slotBackgrounds[i].color = def.Icon != null ? Color.white : def.FallbackColor;
                        }
                        else
                        {
                            slotBackgrounds[i].sprite = null;
                            slotBackgrounds[i].color = slotColor;
                        }
                    }
                    else
                    {
                        // Slot vacío: mantiene el color oscuro/transparente original de la UI
                        slotBackgrounds[i].sprite = null;
                        slotBackgrounds[i].color = slotColor;
                    }
                }
            }
        }

        private ItemDefinition FindItemDefinitionSafe(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;

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