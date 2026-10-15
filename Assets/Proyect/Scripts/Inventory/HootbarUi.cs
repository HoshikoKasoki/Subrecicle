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
        [SerializeField] private Image[] slotBackgrounds;
        [SerializeField] private Image[] slotBorders;

        [Header("Colors")]
        [SerializeField] private Color slotColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        [SerializeField] private Color normalBorderColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color selectedBorderColor = new Color(1f, 0.75f, 0.15f, 1f);

        private ToolHotbar hotbar;

        private void Awake()
        {
            hotbar = GetComponent<ToolHotbar>();
            if (hotbarRoot == null || slotBackgrounds == null || slotBorders == null)
                Debug.LogWarning("ToolHotbarUI necesita referencias a la barra creada en la escena.", this);
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

        private void RefreshVisuals()
        {
            if (hotbar == null || slotBackgrounds == null || slotBorders == null)
                return;

            int count = Mathf.Min(ToolHotbar.SlotCount, Mathf.Min(slotBackgrounds.Length, slotBorders.Length));
            for (int i = 0; i < count; i++)
            {
                bool selected = i == hotbar.SelectedSlot;
                if (slotBorders[i] != null)
                    slotBorders[i].color = selected ? selectedBorderColor : normalBorderColor;
                if (slotBackgrounds[i] != null)
                    slotBackgrounds[i].color = slotColor;
            }
        }
    }
}
