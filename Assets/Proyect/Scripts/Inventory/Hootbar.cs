using System;
using UnityEngine;

namespace InventorySystem
{
    public class ToolHotbar : MonoBehaviour
    {
        public const int SlotCount = 8;

        [SerializeField]
        private string[] toolSlots = new string[SlotCount];

        [SerializeField, Range(0, SlotCount - 1)]
        private int selectedSlot = 0;

        public int SelectedSlot => selectedSlot;

        public event Action OnHotbarChanged;

        public string GetTool(int slot)
        {
            if (slot < 0 || slot >= SlotCount)
                return null;

            return toolSlots[slot];
        }

        public bool HasTool(int slot)
        {
            return !string.IsNullOrEmpty(GetTool(slot));
        }

        public void SetTool(int slot, string toolId)
        {
            if (slot < 0 || slot >= SlotCount)
                return;

            toolSlots[slot] = toolId;
            OnHotbarChanged?.Invoke();
        }

        public void ClearSlot(int slot)
        {
            if (slot < 0 || slot >= SlotCount)
                return;

            toolSlots[slot] = null;
            OnHotbarChanged?.Invoke();
        }

        public void SelectSlot(int slot)
        {
            if (slot < 0 || slot >= SlotCount)
                return;

            if (selectedSlot == slot)
                return;

            selectedSlot = slot;
            OnHotbarChanged?.Invoke();
        }

        private void Update()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SelectSlot(i);
                    break;
                }
            }
        }
    }
}