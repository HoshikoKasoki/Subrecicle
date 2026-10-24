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
        [SerializeField] private Outline[] selectionOutlines;

        private ToolHotbar hotbar;

        private void Awake()
        {
            hotbar = GetComponent<ToolHotbar>();
            if (hotbarRoot == null || selectionOutlines == null ||
                selectionOutlines.Length < ToolHotbar.SlotCount)
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
            if (hotbar == null || selectionOutlines == null)
                return;

            int count = Mathf.Min(ToolHotbar.SlotCount, selectionOutlines.Length);
            for (int i = 0; i < count; i++)
            {
                if (selectionOutlines[i] != null)
                    selectionOutlines[i].enabled = i == hotbar.SelectedSlot;
            }
        }
    }
}
